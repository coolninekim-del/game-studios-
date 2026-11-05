using System;
using KnightOrder.Battle.Loading;
using KnightOrder.Battle.Timing;
using KnightOrder.Stable;
using KnightOrder.Stable.Loading;
using KnightOrder.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KnightOrder.Battle.Presentation
{
    /// <summary>
    /// Shows the auto-battle on an isometric tile grid with the timing input. Each grid cell is one diamond
    /// tile and holds one unit; a unit is drawn as overlapping foot soldiers on its tile, one figure per
    /// _troopsPerFigure troops (at most _maxFigures), with its exact troop count below. The protagonist
    /// (Story 005) is one mounted unit drawn as a single horse and rider, labelled HERO with its health; its
    /// stats come from the trained profile (<see cref="KnightProfileStore.Current"/>) or the start profile, and
    /// its charge window is widened by horse agility (<see cref="HorseWindowModifier"/>); when the protagonist
    /// charges in the same impact group as foot units, the part of its window beyond theirs is drawn on the
    /// timing bar in blue ahead of the gold zone. On top: a shrinking dark cue ring per charging player unit (Story 002), an arrow flying from the enemy plus a shrinking cyan ring on the targeted player unit
    /// for each arrow volley (Story 003), a timing bar for the soonest cue whose gold zone is its judgement
    /// window, and feedback with a sound: SUCCESS / EARLY / MISS for charges, BLOCK / EARLY / ARROW HIT for
    /// arrows. Touch and mouse are one input: an InputAction bound to &lt;Pointer&gt;/press.
    /// All rules live in <see cref="ChargeTimingController"/>; this class only draws and forwards taps.
    /// Placeholder visuals (generated pixel figures, tiles and rings) until the pixel-art assets exist.
    /// Implements: Story 002, Story 003, Story 005 (mounted protagonist); design/game-brief.md (isometric view,
    /// only the protagonist and heroes ride, one per tile).
    ///
    /// Usage: add to a GameObject in a scene with an orthographic main camera and assign the JSON TextAssets
    /// (battle_rules, a battle setup, battle_charge_timing, stable_rules, stable_start_profile). A tap after the
    /// battle ends restarts it.
    /// </summary>
    public sealed class ChargeTimingBattleView : MonoBehaviour
    {
        private const int POPUP_POOL_SIZE = 8;
        private const float POPUP_SECONDS = 0.7f;
        private const float FLASH_SECONDS = 0.25f;
        private const int ARROW_POOL_SIZE = 8;

        private const int TILE_ORDER = -100;
        private const int RING_ORDER = 5;
        private const int FIGURE_ORDER = 1000;
        private const int ARROW_ORDER = 2500;
        private const int HUD_ORDER = 2900;
        private const int TEXT_ORDER = 3000;

        private static readonly Color PlayerColor = new Color(0.2f, 0.42f, 0.95f);
        private static readonly Color EnemyColor = new Color(0.85f, 0.2f, 0.15f);
        private static readonly Color FieldColor = new Color(0.66f, 0.62f, 0.53f);
        private static readonly Color TileColor = new Color(0.9f, 0.87f, 0.79f);
        private static readonly Color TextColor = new Color(0.12f, 0.11f, 0.1f);
        private static readonly Color CueColor = new Color(0.12f, 0.11f, 0.1f, 0.6f);
        private static readonly Color JudgementRingColor = new Color(0.12f, 0.11f, 0.1f, 0.3f);
        private static readonly Color WindowColor = new Color(0.95f, 0.65f, 0f);
        private static readonly Color HeroWindowColor = new Color(0.35f, 0.6f, 1f);
        private static readonly Color SuccessColor = new Color(0.1f, 0.62f, 0.15f);
        private static readonly Color EarlyColor = new Color(0.88f, 0.42f, 0f);
        private static readonly Color MissColor = new Color(0.85f, 0.1f, 0.1f);
        private static readonly Color ArrowCueColor = new Color(0f, 0.6f, 0.8f, 0.85f);
        private static readonly Color ArrowColor = new Color(0.3f, 0.22f, 0.12f);
        private static readonly Color BlockColor = new Color(0f, 0.55f, 0.78f);

        // Feet positions of the figures inside a tile (world units, tile width 1); front ones last.
        private static readonly Vector2[] FormationOffsets =
        {
            new Vector2(0f, 0.06f), new Vector2(-0.2f, 0.01f), new Vector2(0.2f, 0.01f),
            new Vector2(-0.1f, -0.06f), new Vector2(0.1f, -0.06f), new Vector2(0f, -0.11f)
        };

        [SerializeField] private TextAsset _battleRulesJson;
        [SerializeField] private TextAsset _battleSetupJson;
        [SerializeField] private TextAsset _chargeTimingJson;
        [SerializeField] private TextAsset _stableRulesJson;
        [SerializeField] private TextAsset _startProfileJson;
        [SerializeField] private float _tileWidth = 1f;
        [SerializeField] private float _unitSize = 0.55f;
        [SerializeField] private float _cueStartRadius = 1.4f;
        [SerializeField] private int _troopsPerFigure = 20;
        [SerializeField] private int _maxFigures = 6;

        private BattleRules _battleRules;
        private BattleSetup _battleSetup;
        private ChargeTimingRules _timingRules;
        private HorseWindowModifier _windowModifier;
        private ChargeTimingController _controller;
        private InputAction _tapAction;
        private Sprite _squareSprite;
        private Sprite _ringSprite;
        private Sprite _tileSprite;
        private Sprite _figureSprite;
        private Sprite _horseSprite;
        private Sprite _riderSprite;
        private Transform _tileRoot;
        private Font _font;
        private AudioSource _audio;
        private AudioClip _successClip;
        private AudioClip _failClip;
        private AudioClip _blockClip;
        private AudioClip _arrowHitClip;

        private UnitVisual[] _units;
        private SpriteRenderer _barTrack;
        private SpriteRenderer _barWindow;
        private SpriteRenderer _barHeroWindow;
        private SpriteRenderer _barMarker;
        private TextMesh _barLabel;
        private TextMesh _hud;
        private TextMesh _banner;
        private Popup[] _popups;
        private SpriteRenderer[] _arrows;
        private int _nextPopup;
        private float _barFlashUntil;
        private Color _barFlashColor;

        /// <summary>
        /// Optional extra tap source checked every frame besides the pointer, for scripted runs
        /// (evidence capture). Returns true to tap this frame.
        /// </summary>
        public Func<ChargeTimingController, bool> ExtraTapSource { get; set; }

        /// <summary>The running battle's controller; replaced when the battle restarts.</summary>
        public ChargeTimingController Controller => _controller;

        /// <summary>Raised for every judged player impact, after the view has shown its feedback.</summary>
        public event EventHandler<ImpactJudgedEventArgs> OnImpactJudged;

        /// <summary>Raised for every judged arrow volley at a player unit, after the view has shown its feedback.</summary>
        public event EventHandler<ArrowJudgedEventArgs> OnArrowJudged;

        private void Awake()
        {
            _battleRules = BattleDataLoader.ParseRules(_battleRulesJson.text);
            _battleSetup = BattleDataLoader.ParseSetup(_battleSetupJson.text);
            _timingRules = ChargeTimingDataLoader.ParseRules(_chargeTimingJson.text);
            StableRules stableRules = StableDataLoader.ParseRules(_stableRulesJson.text);
            KnightProfile profile = KnightProfileStore.Current ?? StableDataLoader.ParseProfile(_startProfileJson.text);
            KnightConversion.AddToBattle(_battleSetup, profile, stableRules);
            _windowModifier = new HorseWindowModifier(profile, stableRules);

            _squareSprite = MakeSquareSprite();
            _ringSprite = MakeRingSprite(128, 0.08f);
            _tileSprite = MakeTileSprite();
            _figureSprite = MakeFigureSprite();
            float mountedPixelsPerUnit = 22f / (_unitSize * 1.35f);
            _horseSprite = MountedSprites.MakeHorse(mountedPixelsPerUnit);
            _riderSprite = MountedSprites.MakeRider(mountedPixelsPerUnit);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _successClip = MakeTone("success", new[] { 880f, 1320f }, 0.16f);
            _failClip = MakeTone("fail", new[] { 180f }, 0.2f);
            _blockClip = MakeTone("block", new[] { 1760f, 2350f }, 0.12f);
            _arrowHitClip = MakeTone("arrow_hit", new[] { 260f, 140f }, 0.22f);

            _tapAction = new InputAction("Tap", InputActionType.Button, "<Pointer>/press");
            _tapAction.Enable();

            BuildStaticVisuals();
            StartBattle();
        }

        private void OnDestroy()
        {
            _tapAction?.Dispose();
        }

        private void Update()
        {
            bool tapped = _tapAction.WasPressedThisFrame();
            if (ExtraTapSource != null && ExtraTapSource(_controller))
            {
                tapped = true;
            }

            if (_controller.Simulation.IsFinished)
            {
                if (tapped)
                {
                    StartBattle();
                }
            }
            else
            {
                if (tapped)
                {
                    _controller.Tap();
                }

                _controller.Advance(Time.deltaTime);
            }

            DrawUnits();
            DrawCues();
            DrawPopups();
        }

        private void StartBattle()
        {
            if (_controller != null)
            {
                _controller.OnImpactJudged -= HandleImpactJudged;
                _controller.OnArrowJudged -= HandleArrowJudged;
                _controller.OnTicked -= HandleTicked;
            }

            _controller = new ChargeTimingController(new BattleSimulation(_battleRules, _battleSetup), _timingRules, _windowModifier);
            _controller.OnImpactJudged += HandleImpactJudged;
            _controller.OnArrowJudged += HandleArrowJudged;
            _controller.OnTicked += HandleTicked;

            if (_units != null)
            {
                foreach (UnitVisual unit in _units)
                {
                    Destroy(unit.Root);
                }
            }

            var simUnits = _controller.Simulation.Units;
            _units = new UnitVisual[simUnits.Count];
            for (int i = 0; i < simUnits.Count; i++)
            {
                _units[i] = CreateUnitVisual(simUnits[i]);
            }

            BuildTiles();
            FrameCamera();
            _banner.text = string.Empty;
            HandleTicked(this, EventArgs.Empty);
        }

        private void HandleTicked(object sender, EventArgs e)
        {
            var simUnits = _controller.Simulation.Units;
            int playerTroops = 0;
            int enemyTroops = 0;
            for (int i = 0; i < simUnits.Count; i++)
            {
                BattleUnit unit = simUnits[i];
                _units[i].Label.text = unit.IsMounted ? "HERO " + unit.Troops : unit.Troops.ToString();
                _units[i].Root.SetActive(unit.IsAlive);
                int figures = Mathf.Clamp(Mathf.CeilToInt(unit.Troops / (float)Mathf.Max(1, _troopsPerFigure)), 1, _units[i].Figures.Length);
                for (int f = 0; f < _units[i].Figures.Length; f++)
                {
                    _units[i].Figures[f].gameObject.SetActive(f < figures);
                }
                if (unit.Side == BattleSide.Player)
                {
                    playerTroops += unit.Troops;
                }
                else
                {
                    enemyTroops += unit.Troops;
                }
            }

            _hud.text = "PLAYER " + playerTroops + "   vs   ENEMY " + enemyTroops;
            if (_controller.Simulation.IsFinished)
            {
                BattleResult result = _controller.Simulation.GetResult();
                _banner.text = (result.Winner == BattleWinner.Player ? "VICTORY"
                    : result.Winner == BattleWinner.Enemy ? "DEFEAT" : "DRAW") + "\nTap to restart";
            }
        }

        private void HandleImpactJudged(object sender, ImpactJudgedEventArgs e)
        {
            Color color;
            string text;
            switch (e.Outcome)
            {
                case TimingOutcome.Success:
                    color = SuccessColor;
                    text = "SUCCESS x" + e.PowerMultiplier.ToString("0.0#");
                    _audio.PlayOneShot(_successClip);
                    break;
                case TimingOutcome.Early:
                    color = EarlyColor;
                    text = "EARLY";
                    _audio.PlayOneShot(_failClip);
                    break;
                default:
                    color = MissColor;
                    text = "MISS";
                    _audio.PlayOneShot(_failClip);
                    break;
            }

            ShowFeedback(_units[e.AttackerIndex], text, color, _unitSize);
            OnImpactJudged?.Invoke(this, e);
        }

        private void HandleArrowJudged(object sender, ArrowJudgedEventArgs e)
        {
            Color color;
            string text;
            switch (e.Outcome)
            {
                case TimingOutcome.Success:
                    color = BlockColor;
                    text = "BLOCK x" + e.DamageMultiplier.ToString("0.0#");
                    _audio.PlayOneShot(_blockClip);
                    break;
                case TimingOutcome.Early:
                    color = EarlyColor;
                    text = "EARLY";
                    _audio.PlayOneShot(_arrowHitClip);
                    break;
                default:
                    color = MissColor;
                    text = "ARROW HIT";
                    _audio.PlayOneShot(_arrowHitClip);
                    break;
            }

            // Below the unit, so it does not cover a charge popup of the same moment.
            ShowFeedback(_units[e.TargetIndex], text, color, -_unitSize * 1.6f);
            OnArrowJudged?.Invoke(this, e);
        }

        private void ShowFeedback(UnitVisual unit, string text, Color color, float yOffset)
        {
            unit.FlashColor = color;
            unit.FlashUntil = Time.time + FLASH_SECONDS;
            _barFlashColor = color;
            _barFlashUntil = Time.time + FLASH_SECONDS;

            Popup popup = _popups[_nextPopup];
            _nextPopup = (_nextPopup + 1) % _popups.Length;
            popup.Text.text = text;
            popup.Text.color = color;
            popup.Origin = unit.Root.transform.position + new Vector3(0f, yOffset, 0f);
            popup.StartTime = Time.time;
            popup.Text.gameObject.SetActive(true);
        }

        private void DrawUnits()
        {
            var simUnits = _controller.Simulation.Units;
            float t = _controller.TickProgress;
            for (int i = 0; i < simUnits.Count; i++)
            {
                UnitVisual visual = _units[i];
                BattleUnit unit = simUnits[i];
                Vector3 from = GridToWorld(unit.X, unit.Y);
                Vector3 to = GridToWorld(_controller.NextX(i), _controller.NextY(i));
                Vector3 position = Vector3.Lerp(from, to, t);
                visual.Root.transform.position = position;
                Color color = Time.time < visual.FlashUntil ? visual.FlashColor : visual.BaseColor;
                for (int f = 0; f < visual.Figures.Length; f++)
                {
                    SpriteRenderer figure = visual.Figures[f];
                    figure.color = color;
                    // Lower on screen = nearer the camera = drawn later.
                    figure.sortingOrder = FIGURE_ORDER - Mathf.RoundToInt((position.y + FormationOffsets[f].y * _tileWidth) * 100f);
                }

                if (visual.Horse != null)
                {
                    visual.Horse.sortingOrder = visual.Figures[0].sortingOrder - 1;
                }

                visual.CueRing.enabled = false;
                visual.TargetRing.enabled = false;
                visual.ArrowRing.enabled = false;
            }
        }

        private void DrawCues()
        {
            float now = _controller.Now;
            int arrowCount = 0;
            foreach (PendingImpact impact in _controller.Pending)
            {
                if (!impact.IsCueVisible(now))
                {
                    continue;
                }

                float remaining01 = (impact.ImpactTime - now) / impact.CueLeadSeconds;
                float targetRadius = _unitSize * 0.9f;
                float radius = Mathf.Lerp(targetRadius, _cueStartRadius, remaining01);
                bool open = impact.IsWindowOpen(now);
                if (impact.Kind == TimingCueKind.Arrow)
                {
                    if (arrowCount < _arrows.Length)
                    {
                        DrawArrow(_arrows[arrowCount++], impact, 1f - remaining01);
                    }

                    UnitVisual target = _units[impact.TargetIndex];
                    target.ArrowRing.enabled = true;
                    target.ArrowRing.transform.localScale = IsoScale(radius);
                    target.ArrowRing.color = impact.IsTapped ? Color.gray : open ? WindowColor : ArrowCueColor;
                    target.TargetRing.enabled = true;
                    target.TargetRing.transform.localScale = IsoScale(targetRadius);
                    target.TargetRing.color = open ? WindowColor : JudgementRingColor;
                    continue;
                }

                UnitVisual visual = _units[impact.AttackerIndex];
                visual.CueRing.enabled = true;
                visual.CueRing.transform.localScale = IsoScale(radius);
                visual.CueRing.color = impact.IsTapped ? Color.gray : open ? WindowColor : CueColor;
                visual.TargetRing.enabled = true;
                visual.TargetRing.transform.localScale = IsoScale(targetRadius);
                visual.TargetRing.color = open ? WindowColor : JudgementRingColor;
            }

            for (int i = arrowCount; i < _arrows.Length; i++)
            {
                _arrows[i].enabled = false;
            }

            PendingImpact active = _controller.GetActiveCue();
            bool barVisible = active != null || Time.time < _barFlashUntil;
            _barTrack.enabled = barVisible;
            _barWindow.enabled = barVisible;
            _barMarker.enabled = active != null;
            float width = _barTrack.transform.localScale.x;
            float left = _barTrack.transform.localPosition.x - width * 0.5f;
            float heroWindow = HeroWindowInGroup(active);
            _barHeroWindow.enabled = barVisible && active != null && heroWindow > active.WindowSeconds;
            _barLabel.text = active == null || active.IsTapped ? ""
                : active.Kind == TimingCueKind.Arrow ? "ARROWS! TAP / CLICK in the gold zone to block"
                : _barHeroWindow.enabled ? "CHARGE! TAP / CLICK in the gold zone (blue: hero only)"
                : "CHARGE! TAP / CLICK in the gold zone";
            if (!barVisible)
            {
                return;
            }

            if (_barHeroWindow.enabled)
            {
                float heroWidth = width * heroWindow / active.CueLeadSeconds;
                _barHeroWindow.transform.localScale = new Vector3(heroWidth, _barTrack.transform.localScale.y, 1f);
                _barHeroWindow.transform.localPosition = new Vector3(left + width - heroWidth * 0.5f,
                    _barTrack.transform.localPosition.y, -0.005f);
                _barHeroWindow.color = now >= active.ImpactTime - heroWindow ? Color.white : HeroWindowColor;
            }

            float windowWidth = active != null ? width * active.WindowSeconds / active.CueLeadSeconds
                : width * _timingRules.WindowSeconds / _timingRules.CueLeadSeconds;
            _barWindow.transform.localScale = new Vector3(windowWidth, _barTrack.transform.localScale.y, 1f);
            _barWindow.transform.localPosition = new Vector3(left + width - windowWidth * 0.5f,
                _barTrack.transform.localPosition.y, -0.01f);
            _barWindow.color = Time.time < _barFlashUntil ? _barFlashColor
                : active != null && active.IsWindowOpen(now) ? Color.white : WindowColor;
            if (active != null)
            {
                float progress = Mathf.Clamp01((now - active.CueStartTime) / active.CueLeadSeconds);
                _barMarker.transform.localPosition = new Vector3(left + width * progress,
                    _barTrack.transform.localPosition.y, -0.02f);
            }
        }

        /// <summary>
        /// The mounted player unit's charge window among the charges that impact at the same time as
        /// <paramref name="active"/>, or 0 when there is none (or the active cue is an arrow).
        /// </summary>
        private float HeroWindowInGroup(PendingImpact active)
        {
            if (active == null || active.Kind != TimingCueKind.Charge)
            {
                return 0f;
            }

            foreach (PendingImpact impact in _controller.Pending)
            {
                if (impact.Kind == TimingCueKind.Charge && impact.ImpactTick == active.ImpactTick && !impact.IsTapped
                    && _controller.Simulation.Units[impact.AttackerIndex].IsMounted)
                {
                    return impact.WindowSeconds;
                }
            }

            return 0f;
        }

        private void DrawArrow(SpriteRenderer arrow, PendingImpact impact, float progress01)
        {
            // Flies from the archer to the target so that it lands exactly when the volley resolves.
            Vector3 height = new Vector3(0f, _unitSize * 0.5f, 0f);
            Vector3 from = _units[impact.AttackerIndex].Root.transform.position + height;
            Vector3 to = _units[impact.TargetIndex].Root.transform.position + height;
            Vector3 direction = to - from;
            arrow.enabled = true;
            arrow.color = impact.IsWindowOpen(_controller.Now) ? WindowColor : ArrowColor;
            arrow.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(progress01)) + new Vector3(0f, 0f, -0.05f);
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void DrawPopups()
        {
            foreach (Popup popup in _popups)
            {
                if (!popup.Text.gameObject.activeSelf)
                {
                    continue;
                }

                float age = (Time.time - popup.StartTime) / POPUP_SECONDS;
                if (age >= 1f)
                {
                    popup.Text.gameObject.SetActive(false);
                    continue;
                }

                popup.Text.transform.position = popup.Origin + new Vector3(0f, age * 0.6f, -0.1f);
                Color color = popup.Text.color;
                color.a = 1f - age * age;
                popup.Text.color = color;
            }
        }

        /// <summary>Isometric projection: grid x runs to the upper right, grid y to the lower right.</summary>
        private Vector3 GridToWorld(float x, float y)
        {
            return new Vector3((x + y) * _tileWidth * 0.5f, (x - y) * _tileWidth * 0.25f, 0f);
        }

        /// <summary>A ring of the given radius lying flat on the isometric ground.</summary>
        private static Vector3 IsoScale(float radius)
        {
            return new Vector3(radius * 2f, radius, 1f);
        }

        private void GetGridBounds(out int minX, out int maxX, out int minY, out int maxY)
        {
            // Start cells plus a margin, enough for the retreats of the charge cycle.
            const int margin = 2;
            minX = int.MaxValue;
            maxX = int.MinValue;
            minY = int.MaxValue;
            maxY = int.MinValue;
            foreach (BattleUnit unit in _controller.Simulation.Units)
            {
                minX = Mathf.Min(minX, unit.X - margin);
                maxX = Mathf.Max(maxX, unit.X + margin);
                minY = Mathf.Min(minY, unit.Y - margin);
                maxY = Mathf.Max(maxY, unit.Y + margin);
            }
        }

        private void BuildTiles()
        {
            if (_tileRoot != null)
            {
                Destroy(_tileRoot.gameObject);
            }

            _tileRoot = new GameObject("Tiles").transform;
            _tileRoot.SetParent(transform, false);
            GetGridBounds(out int minX, out int maxX, out int minY, out int maxY);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    SpriteRenderer tile = CreateSprite("Tile", _tileRoot, _tileSprite, TileColor, TILE_ORDER);
                    tile.transform.position = GridToWorld(x, y);
                    tile.transform.localScale = new Vector3(_tileWidth, _tileWidth, 1f);
                }
            }
        }

        private void FrameCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            // Frame the units' start cells (not the whole tile margin) plus room for the HUD and timing bar.
            GetGridBounds(out int minX, out int maxX, out int minY, out int maxY);
            minX += 1;
            maxX -= 1;
            minY += 1;
            maxY -= 1;
            float left = GridToWorld(minX, minY).x - _tileWidth * 0.5f;
            float right = GridToWorld(maxX, maxY).x + _tileWidth * 0.5f;
            float top = GridToWorld(maxX, minY).y + _unitSize + 0.3f;
            float bottom = GridToWorld(minX, maxY).y - _tileWidth * 0.25f - 0.3f;
            float halfWidth = (right - left) * 0.5f + 0.3f;
            float halfHeight = (top - bottom) * 0.5f + 1.3f;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.1f, cam.aspect));
            cam.transform.position = new Vector3((left + right) * 0.5f, (top + bottom) * 0.5f - 0.3f, -10f);
            cam.backgroundColor = FieldColor;
            cam.clearFlags = CameraClearFlags.SolidColor;

            Transform hudRoot = _hud.transform.parent;
            hudRoot.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            float size = cam.orthographicSize;
            _hud.transform.localPosition = new Vector3(0f, size - 0.5f, -0.5f);
            _banner.transform.localPosition = new Vector3(0f, 0.5f, -0.5f);
            float barWidth = Mathf.Min(size * cam.aspect * 1.4f, 9f);
            _barTrack.transform.localScale = new Vector3(barWidth, 0.3f, 1f);
            _barTrack.transform.localPosition = new Vector3(0f, -size + 0.9f, 0f);
            _barMarker.transform.localScale = new Vector3(0.08f, 0.55f, 1f);
            _barLabel.transform.localPosition = new Vector3(0f, -size + 0.45f, -0.5f);
        }

        private void BuildStaticVisuals()
        {
            var hudRoot = new GameObject("Hud").transform;
            hudRoot.SetParent(transform, false);
            _hud = CreateText("Troops", hudRoot, 0.5f, TextColor);
            _banner = CreateText("Banner", hudRoot, 0.9f, TextColor);
            _barTrack = CreateSprite("TimingBarTrack", hudRoot, _squareSprite, new Color(0f, 0f, 0f, 0.6f), HUD_ORDER);
            _barHeroWindow = CreateSprite("TimingBarHeroWindow", hudRoot, _squareSprite, HeroWindowColor, HUD_ORDER + 1);
            _barWindow = CreateSprite("TimingBarWindow", hudRoot, _squareSprite, WindowColor, HUD_ORDER + 2);
            _barMarker = CreateSprite("TimingBarMarker", hudRoot, _squareSprite, Color.white, HUD_ORDER + 3);
            _barLabel = CreateText("TimingBarLabel", hudRoot, 0.32f, TextColor);

            _arrows = new SpriteRenderer[ARROW_POOL_SIZE];
            for (int i = 0; i < _arrows.Length; i++)
            {
                _arrows[i] = CreateSprite("Arrow" + i, transform, _squareSprite, ArrowColor, ARROW_ORDER);
                _arrows[i].transform.localScale = new Vector3(0.45f, 0.05f, 1f);
                _arrows[i].enabled = false;
            }

            _popups = new Popup[POPUP_POOL_SIZE];
            for (int i = 0; i < _popups.Length; i++)
            {
                TextMesh text = CreateText("Feedback" + i, transform, 0.42f, Color.white);
                text.gameObject.SetActive(false);
                _popups[i] = new Popup { Text = text };
            }
        }

        private UnitVisual CreateUnitVisual(BattleUnit unit)
        {
            var root = new GameObject(unit.Side + "Unit");
            root.transform.SetParent(transform, false);
            Color baseColor = unit.Side == BattleSide.Player ? PlayerColor : EnemyColor;
            SpriteRenderer horse = null;
            SpriteRenderer[] figures;
            if (unit.IsMounted)
            {
                // One horse and rider per tile; the rider is the tinted figure, the horse stays brown.
                horse = CreateSprite("Horse", root.transform, _horseSprite, Color.white, FIGURE_ORDER);
                horse.flipX = unit.Side == BattleSide.Enemy;
                figures = new[] { CreateSprite("Rider", root.transform, _riderSprite, baseColor, FIGURE_ORDER) };
                figures[0].flipX = horse.flipX;
            }
            else
            {
                figures = new SpriteRenderer[Mathf.Clamp(_maxFigures, 1, FormationOffsets.Length)];
                for (int f = 0; f < figures.Length; f++)
                {
                    figures[f] = CreateSprite("Soldier" + f, root.transform, _figureSprite, baseColor, FIGURE_ORDER);
                    figures[f].transform.localPosition = FormationOffsets[f] * _tileWidth;
                    // Player soldiers face +x (upper right), enemy soldiers face the other way.
                    figures[f].flipX = unit.Side == BattleSide.Enemy;
                }
            }

            SpriteRenderer targetRing = CreateSprite("JudgementRing", root.transform, _ringSprite, Color.white, RING_ORDER);
            SpriteRenderer cueRing = CreateSprite("CueRing", root.transform, _ringSprite, CueColor, RING_ORDER + 1);
            SpriteRenderer arrowRing = CreateSprite("ArrowCueRing", root.transform, _ringSprite, ArrowCueColor, RING_ORDER + 2);
            TextMesh label = CreateText("Troops", root.transform, 0.24f, TextColor);
            label.transform.localPosition = new Vector3(0f, -_tileWidth * 0.25f - 0.08f, -0.1f);
            return new UnitVisual
            {
                Root = root, Figures = figures, Horse = horse, CueRing = cueRing, TargetRing = targetRing, ArrowRing = arrowRing,
                Label = label, BaseColor = baseColor
            };
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

        private TextMesh CreateText(string name, Transform parent, float height, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMesh>();
            text.font = _font;
            text.fontSize = 64;
            text.characterSize = height / 6.4f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
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

        /// <summary>A 2:1 diamond tile: light fill with a blue outline (world width 1 at scale 1).</summary>
        private static Sprite MakeTileSprite()
        {
            const int width = 64;
            const int height = 32;
            var texture = new Texture2D(width, height) { filterMode = FilterMode.Point };
            var pixels = new Color32[width * height];
            var fill = new Color32(255, 255, 255, 255);
            var line = new Color32(70, 120, 225, 255);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float d = Mathf.Abs(x + 0.5f - width * 0.5f) / (width * 0.5f) + Mathf.Abs(y + 0.5f - height * 0.5f) / (height * 0.5f);
                    pixels[y * width + x] = d > 1f ? new Color32(0, 0, 0, 0) : d > 0.93f ? line : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
        }

        /// <summary>
        /// A 12x20 pixel foot soldier with a lance, pivot at the feet. The head is white and the body grey,
        /// so the side tint shows strongest on the head.
        /// </summary>
        private Sprite MakeFigureSprite()
        {
            const int width = 12;
            const int height = 20;
            var texture = new Texture2D(width, height) { filterMode = FilterMode.Point };
            var pixels = new Color32[width * height];
            var head = new Color32(255, 255, 255, 255);
            var body = new Color32(60, 60, 60, 255);
            var lance = new Color32(90, 80, 70, 255);
            void Set(int x, int y, Color32 c) => pixels[y * width + x] = c;
            for (int y = 15; y <= 17; y++)
            {
                for (int x = 3; x <= 5; x++)
                {
                    Set(x, y, head);
                }
            }

            for (int y = 7; y <= 14; y++)
            {
                Set(4, y, body);
            }

            for (int x = 2; x <= 7; x++)
            {
                Set(x, 12, body);
            }

            Set(3, 6, body); Set(3, 5, body); Set(2, 4, body); Set(2, 3, body); Set(1, 2, body); Set(1, 1, body); Set(1, 0, body);
            Set(5, 6, body); Set(5, 5, body); Set(6, 4, body); Set(6, 3, body); Set(7, 2, body); Set(7, 1, body); Set(7, 0, body);
            for (int i = 0; i <= 7; i++)
            {
                Set(Mathf.Min(width - 1, 6 + i * 5 / 7), 10 + i, lance);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            float pixelsPerUnit = height / (_unitSize * 0.8f);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), pixelsPerUnit);
        }

        private static Sprite MakeRingSprite(int size, float thickness01)
        {
            var texture = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - half) * (x + 0.5f - half) + (y + 0.5f - half) * (y + 0.5f - half)) / half;
                    float alpha = Mathf.Clamp01(1f - Mathf.Abs(d - (1f - thickness01)) / thickness01);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static AudioClip MakeTone(string name, float[] frequencies, float seconds)
        {
            const int sampleRate = 44100;
            int samples = (int)(sampleRate * seconds);
            var data = new float[samples];
            int segment = samples / frequencies.Length;
            for (int i = 0; i < samples; i++)
            {
                float frequency = frequencies[Mathf.Min(i / segment, frequencies.Length - 1)];
                float envelope = 1f - (float)i / samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private sealed class UnitVisual
        {
            public GameObject Root;
            public SpriteRenderer[] Figures;
            public SpriteRenderer Horse;
            public SpriteRenderer CueRing;
            public SpriteRenderer TargetRing;
            public SpriteRenderer ArrowRing;
            public TextMesh Label;
            public Color BaseColor;
            public Color FlashColor;
            public float FlashUntil;
        }

        private sealed class Popup
        {
            public TextMesh Text;
            public Vector3 Origin;
            public float StartTime;
        }
    }
}
