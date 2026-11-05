using System;
using KnightOrder.Battle.Timing;
using KnightOrder.Duel.Loading;
using KnightOrder.Stable;
using KnightOrder.Stable.Loading;
using KnightOrder.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KnightOrder.Duel.Presentation
{
    /// <summary>
    /// Shows the boss duel as a joust on an isometric list: the protagonist (blue) and the boss (red) ride
    /// at each other along two lanes split by a barrier, pass side by side at the impact, and turn at the ends.
    /// When the boss's shield side is revealed a gold shield appears on the boss (HIGH or LOW) and a timing bar
    /// runs to the impact; its gold zone is the judgement window. A tap in the upper half of the screen aims
    /// the lance high, in the lower half low; the lance tilts to show the aim. At the impact a popup and a sound
    /// show the strike (PERFECT / EARLY / SHIELD / NO STRIKE) and the boss's lance (PARRIED / HIT).
    /// Touch and mouse are one input: an InputAction bound to &lt;Pointer&gt;/press.
    /// All rules live in <see cref="DuelSimulation"/>; this class only draws and forwards taps.
    /// Placeholder visuals (generated pixel riders and tiles) until the pixel-art assets exist.
    /// The protagonist's stats come from the trained profile (<see cref="KnightProfileStore.Current"/>, set by
    /// the stable) or, without a stable visit, from the start profile; the setup's hero entry is replaced.
    /// Implements: Story 004, Story 005 (trained stats reach the duel); design/game-brief.md (isometric view,
    /// mounted protagonist).
    ///
    /// Usage: add to a GameObject in a scene with an orthographic main camera and assign duel_rules.json,
    /// a duel setup TextAsset and stable_start_profile.json. A tap after the duel ends restarts it.
    /// </summary>
    public sealed class DuelView : MonoBehaviour
    {
        private const int POPUP_POOL_SIZE = 6;
        private const float POPUP_SECONDS = 1.1f;
        private const float FLASH_SECONDS = 0.3f;
        private const float RIDER_PIXELS_PER_UNIT = 20f;
        private const float HP_BAR_GAP = 1.3f;

        private const int BACKDROP_ORDER = -200;
        private const int TILE_ORDER = -100;
        private const int BARRIER_ORDER = -50;
        private const int FIGURE_ORDER = 1000;
        private const int HUD_ORDER = 2900;
        private const int TEXT_ORDER = 3000;

        private static readonly Color HeroColor = new Color(0.2f, 0.42f, 0.95f);
        private static readonly Color BossColor = new Color(0.85f, 0.2f, 0.15f);
        private static readonly Color FieldColor = new Color(0.66f, 0.62f, 0.53f);
        private static readonly Color UpperHalfColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color TileColor = new Color(0.9f, 0.87f, 0.79f);
        private static readonly Color BarrierColor = new Color(0.42f, 0.3f, 0.18f);
        private static readonly Color TextColor = new Color(0.12f, 0.11f, 0.1f);
        private static readonly Color HintColor = new Color(0.12f, 0.11f, 0.1f, 0.55f);
        private static readonly Color WindowColor = new Color(0.95f, 0.65f, 0f);
        private static readonly Color ShieldColor = new Color(0.95f, 0.72f, 0.05f);
        private static readonly Color LanceColor = new Color(0.32f, 0.24f, 0.14f);
        private static readonly Color SuccessColor = new Color(0.1f, 0.62f, 0.15f);
        private static readonly Color EarlyColor = new Color(0.88f, 0.42f, 0f);
        private static readonly Color MissColor = new Color(0.85f, 0.1f, 0.1f);
        private static readonly Color ParryColor = new Color(0f, 0.55f, 0.78f);
        private static readonly Color PanelColor = new Color(0.08f, 0.07f, 0.06f, 0.72f);
        private static readonly Color VictoryColor = new Color(0.55f, 0.92f, 0.45f);
        private static readonly Color DefeatColor = new Color(1f, 0.45f, 0.4f);

        [SerializeField] private TextAsset _duelRulesJson;
        [SerializeField] private TextAsset _duelSetupJson;
        [SerializeField] private TextAsset _startProfileJson;
        [SerializeField] private float _tileWidth = 1f;
        [SerializeField] private int _laneHalfLength = 4;
        [SerializeField] private float _laneOffset = 1f;
        [SerializeField] private float _bossScale = 1.12f;

        private DuelRules _rules;
        private DuelSetup _setup;
        private DuelSimulation _duel;
        private InputAction _tapAction;
        private Sprite _squareSprite;
        private Sprite _tileSprite;
        private Sprite _horseSprite;
        private Sprite _riderSprite;
        private Font _font;
        private AudioSource _audio;
        private AudioClip _successClip;
        private AudioClip _failClip;
        private AudioClip _shieldClip;
        private AudioClip _hitClip;

        private Rider _hero;
        private Rider _boss;
        private SpriteRenderer _shield;
        private TextMesh _guardLabel;
        private TextMesh _aimLabel;
        private SpriteRenderer _upperHalf;
        private SpriteRenderer _heroHpBar;
        private SpriteRenderer _bossHpBar;
        private TextMesh _heroHpText;
        private TextMesh _bossHpText;
        private TextMesh _passText;
        private TextMesh _highHint;
        private TextMesh _lowHint;
        private SpriteRenderer _barTrack;
        private SpriteRenderer _barWindow;
        private SpriteRenderer _barMarker;
        private TextMesh _barLabel;
        private TextMesh _banner;
        private SpriteRenderer _bannerPanel;
        private Popup[] _popups;
        private int _nextPopup;
        private float _barFlashUntil;
        private Color _barFlashColor;
        private int _shownPass = -1;
        private float _hpBarWidth;

        /// <summary>
        /// Optional extra tap source checked every frame besides the pointer, for scripted runs (evidence
        /// capture). Returns the aim to tap with this frame, or null for no tap.
        /// </summary>
        public Func<DuelSimulation, LanceAim?> ExtraTapSource { get; set; }

        /// <summary>The running duel; replaced when the duel restarts.</summary>
        public DuelSimulation Duel => _duel;

        /// <summary>Raised for every resolved pass, after the view has shown its feedback.</summary>
        public event EventHandler<PassResolvedEventArgs> OnPassResolved;

        private void Awake()
        {
            _rules = DuelDataLoader.ParseRules(_duelRulesJson.text);
            _setup = DuelDataLoader.ParseSetup(_duelSetupJson.text);
            KnightProfile profile = KnightProfileStore.Current ?? StableDataLoader.ParseProfile(_startProfileJson.text);
            _setup.Hero = KnightConversion.ToDuelist(profile);

            _squareSprite = MakeSquareSprite();
            _tileSprite = MakeTileSprite();
            _horseSprite = MountedSprites.MakeHorse(RIDER_PIXELS_PER_UNIT);
            _riderSprite = MountedSprites.MakeRider(RIDER_PIXELS_PER_UNIT);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _successClip = MakeTone("perfect", new[] { 660f, 990f, 1320f }, 0.2f);
            _failClip = MakeTone("fail", new[] { 180f }, 0.2f);
            _shieldClip = MakeTone("shield", new[] { 520f, 520f }, 0.14f);
            _hitClip = MakeTone("hit", new[] { 240f, 120f }, 0.25f);

            _tapAction = new InputAction("Tap", InputActionType.Button, "<Pointer>/press");
            _tapAction.Enable();

            BuildField();
            BuildHud();
            FrameCamera();
            StartDuel();
        }

        private void OnDestroy()
        {
            _tapAction?.Dispose();
        }

        private void Update()
        {
            LanceAim? aim = null;
            if (_tapAction.WasPressedThisFrame())
            {
                Pointer pointer = Pointer.current;
                float y = pointer != null ? pointer.position.ReadValue().y : 0f;
                aim = y >= Screen.height * 0.5f ? LanceAim.High : LanceAim.Low;
            }

            LanceAim? extra = ExtraTapSource?.Invoke(_duel);
            if (extra.HasValue)
            {
                aim = extra;
            }

            if (_duel.IsFinished)
            {
                if (aim.HasValue)
                {
                    StartDuel();
                }
            }
            else
            {
                if (aim.HasValue)
                {
                    _duel.Tap(aim.Value);
                }

                _duel.Advance(Time.deltaTime);
            }

            if (_duel.PassIndex != _shownPass)
            {
                _shownPass = _duel.PassIndex;
                RefreshHud();
            }

            DrawRiders();
            DrawTimingBar();
            DrawPopups();
        }

        private void StartDuel()
        {
            if (_duel != null)
            {
                _duel.OnPassResolved -= HandlePassResolved;
            }

            _duel = new DuelSimulation(_rules, _setup);
            _duel.OnPassResolved += HandlePassResolved;
            _shownPass = -1;
            _banner.text = string.Empty;
            _bannerPanel.enabled = false;
            RefreshHud();
        }

        private void HandlePassResolved(object sender, PassResolvedEventArgs e)
        {
            string strike;
            Color strikeColor;
            if (e.Outcome == TimingOutcome.Miss)
            {
                strike = "NO STRIKE";
                strikeColor = MissColor;
                _audio.PlayOneShot(_failClip);
            }
            else if (e.HitShield)
            {
                strike = "SHIELD -" + e.DamageToBoss;
                strikeColor = EarlyColor;
                _audio.PlayOneShot(_shieldClip);
            }
            else if (e.Outcome == TimingOutcome.Success)
            {
                strike = "PERFECT -" + e.DamageToBoss;
                strikeColor = SuccessColor;
                _audio.PlayOneShot(_successClip);
            }
            else
            {
                strike = "EARLY -" + e.DamageToBoss;
                strikeColor = EarlyColor;
                _audio.PlayOneShot(_failClip);
            }

            if (e.DamageToBoss > 0)
            {
                Flash(_boss);
            }

            ShowPopup(strike, strikeColor, _boss.Root.position + new Vector3(0f, 1.55f, 0f));
            if (e.DamageToHero > 0)
            {
                _audio.PlayOneShot(_hitClip);
                Flash(_hero);
                ShowPopup("HIT -" + e.DamageToHero, MissColor, _hero.Root.position + new Vector3(0f, -0.35f, 0f));
            }
            else
            {
                ShowPopup("PARRIED", ParryColor, _hero.Root.position + new Vector3(0f, -0.35f, 0f));
            }

            _barFlashColor = strikeColor;
            _barFlashUntil = Time.time + FLASH_SECONDS;
            RefreshHud();
            if (_duel.IsFinished)
            {
                DuelResult result = _duel.GetResult();
                _bannerPanel.enabled = true;
                _banner.text = result.Victory
                    ? "VICTORY\n+" + result.GrowthReward + " growth\nTap to restart"
                    : (result.ReachedPassCap ? "DEFEAT (time)" : "DEFEAT") + "\nTap to restart";
                _banner.color = result.Victory ? VictoryColor : DefeatColor;
            }

            OnPassResolved?.Invoke(this, e);
        }

        private void RefreshHud()
        {
            _heroHpText.text = "HERO " + _duel.HeroHp + " / " + _duel.HeroMaxHp;
            _bossHpText.text = "BOSS " + _duel.BossHp + " / " + _duel.BossMaxHp;
            SetHpBar(_heroHpBar, _duel.HeroHp / (float)_duel.HeroMaxHp, -1f);
            SetHpBar(_bossHpBar, _duel.BossHp / (float)_duel.BossMaxHp, 1f);
            _passText.text = "PASS " + (_duel.PassIndex + 1) + " / " + _rules.MaxPasses;
        }

        private void SetHpBar(SpriteRenderer bar, float fraction01, float side)
        {
            // The bar shrinks toward the screen edge on its side.
            float width = _hpBarWidth * Mathf.Clamp01(fraction01);
            Vector3 position = bar.transform.localPosition;
            float outer = side * (_hpBarWidth + HP_BAR_GAP);
            bar.transform.localScale = new Vector3(width, 0.22f, 1f);
            bar.transform.localPosition = new Vector3(outer - side * width * 0.5f, position.y, position.z);
        }

        private void DrawRiders()
        {
            // Pass k: the protagonist rides +x on even passes, -x on odd ones, from one end of the lane to the
            // middle (impact), then on to the other end while turning. The boss mirrors them.
            float direction = _duel.PassIndex % 2 == 0 ? 1f : -1f;
            float x;
            if (_duel.IsTurning)
            {
                float t = Mathf.Clamp01((_duel.Now - _duel.ImpactTime) / Mathf.Max(0.01f, _rules.TurnaroundSeconds));
                x = direction * _laneHalfLength * (1f - (1f - t) * (1f - t));
            }
            else
            {
                float t = Mathf.Clamp01((_duel.Now - _duel.PassStartTime) / _rules.ApproachSeconds);
                // The horses gather speed toward the impact.
                x = direction * _laneHalfLength * (t * t - 1f);
            }

            // Riders turn their horses around for the next pass during the second half of the turnaround.
            bool turned = _duel.IsTurning && _duel.Now - _duel.ImpactTime > _rules.TurnaroundSeconds * 0.5f;
            float heroFacing = turned ? -direction : direction;
            PlaceRider(_hero, x, _laneOffset, heroFacing, _duel.HeroAim);
            PlaceRider(_boss, -x, -_laneOffset, -heroFacing, null);

            bool showGuard = _duel.IsGuardRevealed && !_duel.IsTurning && !_duel.IsFinished;
            _shield.enabled = showGuard;
            _guardLabel.text = _duel.IsFinished || _duel.IsTurning ? "" : !showGuard ? "GUARD ?"
                : _duel.BossGuard == LanceAim.High ? "GUARD HIGH" : "GUARD LOW";
            _guardLabel.transform.position = _boss.Root.position + new Vector3(0f, 1.25f * _bossScale, -0.2f);
            if (showGuard)
            {
                float facing = _boss.Horse.flipX ? -1f : 1f;
                float height = _duel.BossGuard == LanceAim.High ? 0.82f : 0.5f;
                _shield.transform.position = _boss.Root.position + new Vector3(facing * 0.12f, height, -0.01f) * _bossScale;
                _shield.sortingOrder = _boss.Body.sortingOrder + 2;
            }

            _aimLabel.text = _duel.HeroAim.HasValue && !_duel.IsTurning ? (_duel.HeroAim.Value == LanceAim.High ? "AIM HIGH" : "AIM LOW") : "";
            _aimLabel.transform.position = _hero.Root.position + new Vector3(0f, 1.3f, -0.2f);
        }

        private void PlaceRider(Rider rider, float gridX, float gridY, float facing, LanceAim? aim)
        {
            Vector3 position = GridToWorld(gridX, gridY);
            rider.Root.position = position;
            bool left = facing < 0f;
            rider.Horse.flipX = left;
            rider.Body.flipX = left;
            int order = FIGURE_ORDER - Mathf.RoundToInt(position.y * 100f);
            rider.Horse.sortingOrder = order;
            rider.Body.sortingOrder = order + 1;
            rider.Lance.sortingOrder = order + 3;
            rider.Body.color = Time.time < rider.FlashUntil ? Color.Lerp(rider.BaseColor, Color.white, 0.6f) : rider.BaseColor;

            // The lance points along the riding direction on the ground, tilted up or down by the aim.
            float baseAngle = Mathf.Atan2(0.25f, 0.5f) * Mathf.Rad2Deg;
            float tilt = aim == LanceAim.High ? 14f : aim == LanceAim.Low ? -14f : 0f;
            float angle = left ? 180f + baseAngle - tilt : baseAngle + tilt;
            Vector3 hand = new Vector3(facing * 0.05f, 0.68f, 0f) * rider.Scale;
            Vector3 along = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
            rider.Lance.transform.position = position + hand + along * (0.5f * rider.Scale) + new Vector3(0f, 0f, -0.01f);
            rider.Lance.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void DrawTimingBar()
        {
            bool active = !_duel.IsFinished && _duel.IsGuardRevealed && !_duel.IsTurning;
            bool visible = active || Time.time < _barFlashUntil;
            _barTrack.enabled = visible;
            _barWindow.enabled = visible;
            _barMarker.enabled = active;
            _barLabel.text = !active ? "" : _duel.HasTapped ? "" : "STRIKE! TAP in the gold zone - aim away from the shield";
            if (!visible)
            {
                return;
            }

            float width = _barTrack.transform.localScale.x;
            float left = _barTrack.transform.localPosition.x - width * 0.5f;
            float windowWidth = width * _duel.HeroWindowSeconds / _rules.GuardRevealSeconds;
            _barWindow.transform.localScale = new Vector3(windowWidth, _barTrack.transform.localScale.y, 1f);
            _barWindow.transform.localPosition = new Vector3(left + width - windowWidth * 0.5f, _barTrack.transform.localPosition.y, -0.01f);
            _barWindow.color = Time.time < _barFlashUntil ? _barFlashColor : _duel.IsWindowOpen ? Color.white : WindowColor;
            if (active)
            {
                float progress = Mathf.Clamp01((_duel.Now - _duel.GuardRevealTime) / _rules.GuardRevealSeconds);
                _barMarker.transform.localPosition = new Vector3(left + width * progress, _barTrack.transform.localPosition.y, -0.02f);
                _barMarker.color = _duel.HasTapped ? Color.gray : Color.white;
            }
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

                popup.Text.transform.position = popup.Origin + new Vector3(0f, age * 0.5f, -0.3f);
                Color color = popup.Text.color;
                color.a = 1f - age * age;
                popup.Text.color = color;
            }
        }

        /// <summary>A hit flash toward white in the rider's own colour; the popup colour carries the meaning, so
        /// both sides stay recognisable when they are hit together.</summary>
        private static void Flash(Rider rider)
        {
            rider.FlashUntil = Time.time + FLASH_SECONDS;
        }

        private void ShowPopup(string text, Color color, Vector3 origin)
        {
            Popup popup = _popups[_nextPopup];
            _nextPopup = (_nextPopup + 1) % _popups.Length;
            popup.Text.text = text;
            popup.Text.color = color;
            popup.Origin = origin;
            popup.StartTime = Time.time;
            popup.Text.gameObject.SetActive(true);
        }

        /// <summary>Isometric projection, the same as the battle view: grid x to the upper right, grid y to the lower right.</summary>
        private Vector3 GridToWorld(float x, float y)
        {
            return new Vector3((x + y) * _tileWidth * 0.5f, (x - y) * _tileWidth * 0.25f, 0f);
        }

        private void BuildField()
        {
            var field = new GameObject("List").transform;
            field.SetParent(transform, false);
            int length = _laneHalfLength + 1;
            for (int x = -length; x <= length; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    SpriteRenderer tile = CreateSprite("Tile", field, _tileSprite, TileColor, TILE_ORDER);
                    tile.transform.position = GridToWorld(x, y);
                    tile.transform.localScale = new Vector3(_tileWidth, _tileWidth, 1f);
                }
            }

            // The barrier (tilt) between the two lanes: a low fence along grid y = 0.
            float angle = Mathf.Atan2(0.25f, 0.5f) * Mathf.Rad2Deg;
            Vector3 from = GridToWorld(-_laneHalfLength - 0.5f, 0f);
            Vector3 to = GridToWorld(_laneHalfLength + 0.5f, 0f);
            SpriteRenderer rail = CreateSprite("Barrier", field, _squareSprite, BarrierColor, BARRIER_ORDER);
            rail.transform.position = (from + to) * 0.5f + new Vector3(0f, 0.18f, 0f);
            rail.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            rail.transform.localScale = new Vector3(Vector3.Distance(from, to), 0.06f, 1f);
            for (int x = -_laneHalfLength; x <= _laneHalfLength; x++)
            {
                SpriteRenderer post = CreateSprite("Post", field, _squareSprite, BarrierColor, BARRIER_ORDER);
                post.transform.position = GridToWorld(x, 0f) + new Vector3(0f, 0.09f, 0f);
                post.transform.localScale = new Vector3(0.05f, 0.2f, 1f);
            }

            _hero = CreateRider("Hero", HeroColor, 1f);
            _boss = CreateRider("Boss", BossColor, _bossScale);
            _shield = CreateSprite("BossShield", transform, _squareSprite, ShieldColor, FIGURE_ORDER);
            _shield.transform.localScale = new Vector3(0.2f, 0.26f, 1f) * _bossScale;
            _guardLabel = CreateText("GuardLabel", transform, 0.3f, TextColor);
            _aimLabel = CreateText("AimLabel", transform, 0.26f, HeroColor);

            _popups = new Popup[POPUP_POOL_SIZE];
            for (int i = 0; i < _popups.Length; i++)
            {
                TextMesh text = CreateText("Feedback" + i, transform, 0.42f, Color.white);
                text.gameObject.SetActive(false);
                _popups[i] = new Popup { Text = text };
            }
        }

        private void BuildHud()
        {
            var hud = new GameObject("Hud").transform;
            hud.SetParent(transform, false);
            _upperHalf = CreateSprite("UpperHalf", hud, _squareSprite, UpperHalfColor, BACKDROP_ORDER);
            _heroHpBar = CreateSprite("HeroHp", hud, _squareSprite, HeroColor, HUD_ORDER);
            _bossHpBar = CreateSprite("BossHp", hud, _squareSprite, BossColor, HUD_ORDER);
            _heroHpText = CreateText("HeroHpText", hud, 0.3f, TextColor);
            _bossHpText = CreateText("BossHpText", hud, 0.3f, TextColor);
            _passText = CreateText("Pass", hud, 0.34f, TextColor);
            _highHint = CreateText("HighHint", hud, 0.22f, HintColor);
            _lowHint = CreateText("LowHint", hud, 0.22f, HintColor);
            _highHint.text = "TAP UPPER HALF = LANCE HIGH";
            _lowHint.text = "TAP LOWER HALF = LANCE LOW";
            _barTrack = CreateSprite("TimingBarTrack", hud, _squareSprite, new Color(0f, 0f, 0f, 0.6f), HUD_ORDER);
            _barWindow = CreateSprite("TimingBarWindow", hud, _squareSprite, WindowColor, HUD_ORDER + 1);
            _barMarker = CreateSprite("TimingBarMarker", hud, _squareSprite, Color.white, HUD_ORDER + 2);
            _barLabel = CreateText("TimingBarLabel", hud, 0.3f, TextColor);
            _bannerPanel = CreateSprite("BannerPanel", hud, _squareSprite, PanelColor, TEXT_ORDER - 1);
            _bannerPanel.enabled = false;
            _banner = CreateText("Banner", hud, 0.5f, TextColor);
        }

        private void FrameCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            // The list runs diagonally, so its screen half-width is half its grid length.
            float halfLaneWidth = (_laneHalfLength + 2f) * _tileWidth * 0.5f;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(3.2f, (halfLaneWidth + 0.6f) / Mathf.Max(0.1f, cam.aspect));
            cam.transform.position = new Vector3(0f, 0.5f, -10f);
            cam.backgroundColor = FieldColor;
            cam.clearFlags = CameraClearFlags.SolidColor;

            Transform hud = _banner.transform.parent;
            hud.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            float size = cam.orthographicSize;
            float halfWidth = size * cam.aspect;
            _upperHalf.transform.localScale = new Vector3(halfWidth * 2f, size, 1f);
            _upperHalf.transform.localPosition = new Vector3(0f, size * 0.5f, 1f);
            _hpBarWidth = Mathf.Min(halfWidth * 0.55f, 3.5f);
            _heroHpBar.transform.localPosition = new Vector3(0f, size - 0.45f, -0.5f);
            _bossHpBar.transform.localPosition = new Vector3(0f, size - 0.45f, -0.5f);
            _heroHpText.transform.localPosition = new Vector3(-(_hpBarWidth * 0.5f + HP_BAR_GAP), size - 0.82f, -0.5f);
            _bossHpText.transform.localPosition = new Vector3(_hpBarWidth * 0.5f + HP_BAR_GAP, size - 0.82f, -0.5f);
            _passText.transform.localPosition = new Vector3(0f, size - 0.45f, -0.5f);
            // The list runs from the lower left to the upper right, so the other two corners are free.
            _highHint.transform.localPosition = new Vector3(-halfWidth * 0.64f, size * 0.38f, -0.5f);
            _lowHint.transform.localPosition = new Vector3(halfWidth * 0.64f, -size * 0.38f, -0.5f);
            float barWidth = Mathf.Min(halfWidth * 1.4f, 9f);
            _barTrack.transform.localScale = new Vector3(barWidth, 0.3f, 1f);
            _barTrack.transform.localPosition = new Vector3(0f, -size + 0.85f, 0f);
            _barMarker.transform.localScale = new Vector3(0.08f, 0.55f, 1f);
            _barLabel.transform.localPosition = new Vector3(0f, -size + 0.4f, -0.5f);
            _banner.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            _bannerPanel.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            _bannerPanel.transform.localScale = new Vector3(Mathf.Min(halfWidth * 1.2f, 7f), 2.1f, 1f);
        }

        private Rider CreateRider(string name, Color color, float scale)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            SpriteRenderer horse = CreateSprite("Horse", root, _horseSprite, Color.white, FIGURE_ORDER);
            SpriteRenderer rider = CreateSprite("Rider", root, _riderSprite, color, FIGURE_ORDER + 1);
            SpriteRenderer lance = CreateSprite("Lance", root, _squareSprite, LanceColor, FIGURE_ORDER + 3);
            horse.transform.localScale = Vector3.one * scale;
            rider.transform.localScale = Vector3.one * scale;
            lance.transform.localScale = new Vector3(1.0f * scale, 0.05f, 1f);
            return new Rider { Root = root, Horse = horse, Body = rider, Lance = lance, BaseColor = color, Scale = scale };
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

        /// <summary>A 2:1 diamond tile with a blue outline, the same look as the battle view's tiles.</summary>
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

        private sealed class Rider
        {
            public Transform Root;
            public SpriteRenderer Horse;
            public SpriteRenderer Body;
            public SpriteRenderer Lance;
            public Color BaseColor;
            public float FlashUntil;
            public float Scale;
        }

        private sealed class Popup
        {
            public TextMesh Text;
            public Vector3 Origin;
            public float StartTime;
        }
    }
}
