using System;
using System.Collections.Generic;
using KnightOrder.Battle.Loading;
using KnightOrder.Battle.Timing;
using KnightOrder.Duel;
using KnightOrder.Duel.Loading;
using KnightOrder.Stable;
using KnightOrder.Stable.Loading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace KnightOrder.UI.Stable
{
    /// <summary>
    /// The stable screen: the protagonist on the horse, one button per training (stat name, value and gain),
    /// the training actions left, and the numbers the profile gives the next battle (mounted charge power,
    /// health, charge window) and the next duel (lance power, health, window). After a training the trained
    /// stat and every changed number are shown as "before -> after" in green. TO BATTLE and TO DUEL open those
    /// scenes with the trained profile (<see cref="KnightProfileStore.Current"/>). Each visit to the scene is a
    /// new <see cref="StableSession"/> with every action available.
    /// Touch and mouse are one input: an InputAction bound to &lt;Pointer&gt;/press; buttons are hit-tested in world
    /// space. All rules live in <see cref="StableSession"/> and <see cref="KnightConversion"/>; this class only
    /// draws and forwards button presses. Placeholder visuals and English text until the art and localization
    /// exist.
    /// Implements: Story 005.
    ///
    /// Usage: add to a GameObject in a scene with an orthographic main camera and assign stable_rules.json,
    /// stable_start_profile.json, battle_charge_timing.json and duel_rules.json.
    /// </summary>
    public sealed class StableView : MonoBehaviour
    {
        private const int TEXT_ORDER = 3000;
        private const int BUTTON_ORDER = 100;
        private const float BUTTON_WIDTH = 6.2f;
        private const float BUTTON_HEIGHT = 0.9f;
        private const float BUTTON_STEP = 1.05f;
        private const float PORTRAIT_HEIGHT = 3.2f;

        private static readonly Color FieldColor = new Color(0.66f, 0.62f, 0.53f);
        private static readonly Color ButtonColor = new Color(0.9f, 0.87f, 0.79f);
        private static readonly Color DisabledColor = new Color(0.74f, 0.71f, 0.64f);
        private static readonly Color GoColor = new Color(0.2f, 0.42f, 0.95f);
        private static readonly Color PanelColor = new Color(0.08f, 0.07f, 0.06f, 0.72f);
        private static readonly Color HeroColor = new Color(0.2f, 0.42f, 0.95f);
        private static readonly Color TextColor = new Color(0.12f, 0.11f, 0.1f);
        private static readonly Color MutedTextColor = new Color(0.12f, 0.11f, 0.1f, 0.45f);
        private static readonly Color PanelTextColor = new Color(0.95f, 0.93f, 0.88f);
        private static readonly Color GrowthColor = new Color(0.1f, 0.62f, 0.15f);
        private static readonly Color PanelGrowthColor = new Color(0.55f, 0.92f, 0.45f);

        [SerializeField] private TextAsset _stableRulesJson;
        [SerializeField] private TextAsset _startProfileJson;
        [SerializeField] private TextAsset _chargeTimingJson;
        [SerializeField] private TextAsset _duelRulesJson;
        [SerializeField] private string _battleSceneName = "Battle";
        [SerializeField] private string _duelSceneName = "Duel";

        private readonly List<Button> _buttons = new List<Button>();
        private StableRules _rules;
        private ChargeTimingRules _timing;
        private DuelRules _duelRules;
        private StableSession _session;
        private InputAction _tapAction;
        private Sprite _squareSprite;
        private Font _font;
        private TextMesh _actionsText;
        private TextMesh _messageText;
        private NumberLine[] _numberLines;

        /// <summary>The current stable visit.</summary>
        public StableSession Session => _session;

        /// <summary>Raised after every successful training, after the screen shows it.</summary>
        public event EventHandler<TrainingResult> OnTrained;

        private void Awake()
        {
            _rules = StableDataLoader.ParseRules(_stableRulesJson.text);
            _timing = ChargeTimingDataLoader.ParseRules(_chargeTimingJson.text);
            _duelRules = DuelDataLoader.ParseRules(_duelRulesJson.text);
            KnightProfile profile = KnightProfileStore.Current ?? StableDataLoader.ParseProfile(_startProfileJson.text);
            _session = new StableSession(_rules, profile);
            KnightProfileStore.Current = _session.Profile.Clone();

            _squareSprite = MakeSquareSprite();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _tapAction = new InputAction("Tap", InputActionType.Button, "<Pointer>/press");
            _tapAction.Enable();

            FrameCamera();
            Build();
            Refresh();
        }

        private void OnDestroy()
        {
            _tapAction?.Dispose();
        }

        private void Update()
        {
            if (!_tapAction.WasPressedThisFrame() || Pointer.current == null || Camera.main == null)
            {
                return;
            }

            Vector3 world = Camera.main.ScreenToWorldPoint(Pointer.current.position.ReadValue());
            foreach (Button button in _buttons)
            {
                Vector3 center = button.Panel.transform.position;
                Vector3 size = button.Panel.transform.localScale;
                if (Mathf.Abs(world.x - center.x) <= size.x * 0.5f && Mathf.Abs(world.y - center.y) <= size.y * 0.5f)
                {
                    button.OnPress();
                    return;
                }
            }
        }

        /// <summary>Trains one stat, as its button does.</summary>
        /// <param name="stat">The stat to train.</param>
        /// <returns>What changed, or null when no action is left.</returns>
        public TrainingResult Train(KnightStat stat)
        {
            TrainingResult result = _session.Train(stat);
            if (result == null)
            {
                _messageText.text = "No training actions left";
                _messageText.color = TextColor;
                return null;
            }

            KnightProfileStore.Current = _session.Profile.Clone();
            Refresh();
            OnTrained?.Invoke(this, result);
            return result;
        }

        /// <summary>Opens the battle with the trained profile, as the TO BATTLE button does.</summary>
        public void GoToBattle()
        {
            KnightProfileStore.Current = _session.Profile.Clone();
            SceneManager.LoadScene(_battleSceneName);
        }

        /// <summary>Opens the duel with the trained profile, as the TO DUEL button does.</summary>
        public void GoToDuel()
        {
            KnightProfileStore.Current = _session.Profile.Clone();
            SceneManager.LoadScene(_duelSceneName);
        }

        private void FrameCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(5f, 8.8f / Mathf.Max(0.1f, cam.aspect));
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = FieldColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private void Build()
        {
            CreateText("Title", transform, new Vector3(0f, 4.25f, 0f), 0.75f, TextColor, TextAnchor.MiddleCenter).text = "STABLE";
            _actionsText = CreateText("Actions", transform, new Vector3(0f, 3.55f, 0f), 0.4f, TextColor, TextAnchor.MiddleCenter);

            // The protagonist on the horse, left.
            float pixelsPerUnit = 22f / PORTRAIT_HEIGHT;
            var portrait = new GameObject("Portrait").transform;
            portrait.SetParent(transform, false);
            portrait.localPosition = new Vector3(-6.1f, -1.4f, 0f);
            CreateSprite("Horse", portrait, MountedSprites.MakeHorse(pixelsPerUnit), Color.white, BUTTON_ORDER);
            CreateSprite("Rider", portrait, MountedSprites.MakeRider(pixelsPerUnit), HeroColor, BUTTON_ORDER + 1);
            CreateText("PortraitLabel", transform, new Vector3(-6.1f, -2.0f, 0f), 0.34f, TextColor, TextAnchor.MiddleCenter).text =
                "YOUR KNIGHT AND HORSE";

            // One button per training, middle.
            for (int i = 0; i < _rules.Trainings.Count; i++)
            {
                KnightStat stat = _rules.Trainings[i].Stat;
                var center = new Vector3(-0.7f, 2.5f - i * BUTTON_STEP, 0f);
                Button button = CreateButton("Train" + stat, center, new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT), ButtonColor);
                button.Label.anchor = TextAnchor.MiddleLeft;
                button.Label.alignment = TextAlignment.Left;
                button.Label.transform.position = center + new Vector3(-BUTTON_WIDTH * 0.5f + 0.25f, 0f, -0.1f);
                button.Label.text = StatName(stat);
                button.Value = CreateText("Value", button.Panel.transform.parent,
                    center + new Vector3(BUTTON_WIDTH * 0.5f - 0.25f, 0f, -0.1f), 0.36f, TextColor, TextAnchor.MiddleRight);
                button.Stat = stat;
                button.OnPress = () => Train(stat);
            }

            _messageText = CreateText("Message", transform, new Vector3(-0.7f, 2.5f - _rules.Trainings.Count * BUTTON_STEP, 0f),
                0.42f, TextColor, TextAnchor.MiddleCenter);

            // Next battle and duel numbers, right.
            SpriteRenderer panel = CreateSprite("NumbersPanel", transform, _squareSprite, PanelColor, BUTTON_ORDER);
            panel.transform.localPosition = new Vector3(5.75f, 0.45f, 0f);
            panel.transform.localScale = new Vector3(5.6f, 5.4f, 1f);
            CreateText("BattleHeader", transform, new Vector3(5.75f, 2.75f, 0f), 0.36f, PanelTextColor, TextAnchor.MiddleCenter).text =
                "NEXT BATTLE (MOUNTED HERO)";
            CreateText("DuelHeader", transform, new Vector3(5.75f, 0.25f, 0f), 0.36f, PanelTextColor, TextAnchor.MiddleCenter).text =
                "NEXT DUEL";
            _numberLines = new[]
            {
                CreateNumberLine("CHARGE POWER", 2.1f), CreateNumberLine("HEALTH", 1.55f), CreateNumberLine("CHARGE WINDOW", 1.0f),
                CreateNumberLine("LANCE POWER", -0.4f), CreateNumberLine("HEALTH", -0.95f), CreateNumberLine("WINDOW", -1.5f)
            };

            Button battle = CreateButton("ToBattle", new Vector3(-2.4f, -4.0f, 0f), new Vector2(3.8f, 0.85f), GoColor);
            battle.Label.text = "TO BATTLE";
            battle.Label.color = PanelTextColor;
            battle.OnPress = GoToBattle;
            Button duel = CreateButton("ToDuel", new Vector3(2.4f, -4.0f, 0f), new Vector2(3.8f, 0.85f), GoColor);
            duel.Label.text = "TO DUEL";
            duel.Label.color = PanelTextColor;
            duel.OnPress = GoToDuel;
        }

        private void Refresh()
        {
            _actionsText.text = "TRAINING ACTIONS " + _session.ActionsLeft + " / " + _session.ActionsPerVisit;
            TrainingResult last = _session.LastResult;
            bool canTrain = _session.ActionsLeft > 0;
            foreach (Button button in _buttons)
            {
                if (button.Value == null)
                {
                    continue;
                }

                int gain = _rules.TrainingFor(button.Stat).Gain;
                int value = _session.Profile.Get(button.Stat);
                bool grew = last != null && last.Stat == button.Stat;
                button.Value.text = grew
                    ? last.StatBefore + " -> " + last.StatAfter + (canTrain ? "   (+" + gain + ")" : string.Empty)
                    : value + (canTrain ? "   (+" + gain + ")" : string.Empty);
                button.Value.color = grew ? GrowthColor : canTrain ? TextColor : MutedTextColor;
                button.Label.color = canTrain ? TextColor : MutedTextColor;
                button.Panel.color = canTrain ? ButtonColor : DisabledColor;
            }

            KnightNumbers now = KnightConversion.Numbers(_session.Profile, _rules, _timing, _duelRules);
            KnightNumbers before = last != null ? KnightConversion.Numbers(last.Before, _rules, _timing, _duelRules) : now;
            SetNumber(_numberLines[0], before.BattleChargePower, now.BattleChargePower);
            SetNumber(_numberLines[1], before.BattleHealth, now.BattleHealth);
            SetSeconds(_numberLines[2], before.BattleWindowSeconds, now.BattleWindowSeconds);
            SetNumber(_numberLines[3], before.DuelLancePower, now.DuelLancePower);
            SetNumber(_numberLines[4], before.DuelHealth, now.DuelHealth);
            SetSeconds(_numberLines[5], before.DuelWindowSeconds, now.DuelWindowSeconds);

            if (last != null)
            {
                _messageText.text = StatName(last.Stat) + "  " + last.StatBefore + " -> " + last.StatAfter;
                _messageText.color = GrowthColor;
            }
            else
            {
                _messageText.text = "Tap a training. Each one uses an action.";
                _messageText.color = TextColor;
            }
        }

        private static void SetNumber(NumberLine line, int before, int after)
        {
            bool grew = before != after;
            line.Value.text = grew ? before + " -> " + after : after.ToString();
            line.Value.color = grew ? PanelGrowthColor : PanelTextColor;
        }

        private static void SetSeconds(NumberLine line, float before, float after)
        {
            bool grew = Mathf.Abs(before - after) > 1e-5f;
            line.Value.text = grew ? before.ToString("0.000") + "s -> " + after.ToString("0.000") + "s" : after.ToString("0.000") + "s";
            line.Value.color = grew ? PanelGrowthColor : PanelTextColor;
        }

        private static string StatName(KnightStat stat)
        {
            switch (stat)
            {
                case KnightStat.RiderAttack: return "RIDER ATTACK";
                case KnightStat.RiderHp: return "RIDER HEALTH";
                case KnightStat.HorseSpeed: return "HORSE SPEED";
                case KnightStat.HorseHp: return "HORSE HEALTH";
                case KnightStat.HorseAgility: return "HORSE AGILITY";
                default: return stat.ToString();
            }
        }

        private NumberLine CreateNumberLine(string label, float y)
        {
            CreateText(label, transform, new Vector3(3.25f, y, 0f), 0.32f, PanelTextColor, TextAnchor.MiddleLeft).text = label;
            return new NumberLine { Value = CreateText(label + "Value", transform, new Vector3(8.25f, y, 0f), 0.32f, PanelTextColor, TextAnchor.MiddleRight) };
        }

        private Button CreateButton(string name, Vector3 center, Vector2 size, Color color)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            SpriteRenderer panel = CreateSprite("Panel", root, _squareSprite, color, BUTTON_ORDER);
            panel.transform.position = center;
            panel.transform.localScale = new Vector3(size.x, size.y, 1f);
            TextMesh label = CreateText("Label", root, center + new Vector3(0f, 0f, -0.1f), 0.38f, TextColor, TextAnchor.MiddleCenter);
            var button = new Button { Panel = panel, Label = label };
            _buttons.Add(button);
            return button;
        }

        private static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private TextMesh CreateText(string name, Transform parent, Vector3 position, float height, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var text = go.AddComponent<TextMesh>();
            text.font = _font;
            text.fontSize = 64;
            text.characterSize = height / 6.4f;
            text.anchor = anchor;
            text.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left
                : anchor == TextAnchor.MiddleRight ? TextAlignment.Right : TextAlignment.Center;
            text.color = color;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _font.material;
            renderer.sortingOrder = TEXT_ORDER;
            return text;
        }

        private static Sprite MakeSquareSprite()
        {
            var texture = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private sealed class Button
        {
            public SpriteRenderer Panel;
            public TextMesh Label;
            public TextMesh Value;
            public KnightStat Stat;
            public Action OnPress;
        }

        private sealed class NumberLine
        {
            public TextMesh Value;
        }
    }
}
