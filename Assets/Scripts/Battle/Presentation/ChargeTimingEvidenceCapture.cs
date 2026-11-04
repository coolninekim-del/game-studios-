using System.Collections;
using System.IO;
using KnightOrder.Battle.Timing;
using UnityEngine;

namespace KnightOrder.Battle.Presentation
{
    /// <summary>
    /// Run-and-observe capture for Stories 002 and 003 (see .claude/docs/run-and-observe.md). Does nothing
    /// unless the player is launched with <c>--evidence-dir &lt;absolute dir&gt;</c>. It then plays the battle
    /// with scripted taps and saves screenshots into that directory before quitting. Scripted taps go through
    /// the same <see cref="ChargeTimingController.Tap"/> as pointer taps.
    /// <c>--evidence-story 002</c> (default): per charge impact group (one tick's player impacts), group 1
    /// tapped inside the window, group 2 not tapped, group 3 tapped before the window; saves window-open,
    /// success, miss and early shots.
    /// <c>--evidence-story 003</c>: the same three steps per arrow volley group, saving window-open, block,
    /// hit and early shots, plus one shot the first time a charge cue and an arrow cue are visible together.
    /// When the battle ends before all shots are taken, it is restarted and the steps continue. Battle time is
    /// paused while a screenshot is written.
    /// Implements: Story 002, Story 003 (test evidence).
    ///
    /// Usage:
    /// <code>
    /// KnightOrder.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 --evidence-dir C:/abs/evidence --evidence-story 003
    /// </code>
    /// </summary>
    public sealed class ChargeTimingEvidenceCapture : MonoBehaviour
    {
        private const int SETTLE_FRAMES = 6;
        private const float EARLY_MARGIN_SECONDS = 0.15f;

        private string _directory;
        private bool _arrowStory;
        private ChargeTimingBattleView _view;
        private bool _tapNextFrame;
        private int _groupsJudged;
        private int _lastJudgedTick = -1;
        private bool _bothCuesCaptured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string directory = Arg("--evidence-dir");
            if (directory == null)
            {
                return;
            }

            // Keep running when the window loses focus; otherwise an unattended run pauses forever.
            Application.runInBackground = true;
            var go = new GameObject(nameof(ChargeTimingEvidenceCapture));
            DontDestroyOnLoad(go);
            var capture = go.AddComponent<ChargeTimingEvidenceCapture>();
            capture._directory = directory;
            capture._arrowStory = Arg("--evidence-story") == "003";
        }

        private static string Arg(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_directory);
            _view = FindAnyObjectByType<ChargeTimingBattleView>();
            if (_view == null)
            {
                Debug.LogError("Evidence capture: no ChargeTimingBattleView in the scene.");
                Application.Quit(1);
                yield break;
            }

            _view.ExtraTapSource = _ =>
            {
                bool tap = _tapNextFrame;
                _tapNextFrame = false;
                return tap;
            };
            if (_arrowStory)
            {
                _view.OnArrowJudged += (s, e) => CountGroup(e.Outcome);
            }
            else
            {
                _view.OnImpactJudged += (s, e) => CountGroup(e.Outcome);
            }

            string prefix = _arrowStory ? "story-003-" : "story-002-";

            // Group 1: show the open window, then tap inside it.
            yield return WaitFor(() => Active() != null && Active().IsWindowOpen(Now()));
            yield return Capture(prefix + "window-open.png");
            _tapNextFrame = true;
            yield return WaitFor(() => _groupsJudged >= 1);
            yield return SettleAndCapture(prefix + (_arrowStory ? "block.png" : "success.png"));

            // Group 2: no tap.
            yield return WaitFor(() => _groupsJudged >= 2);
            yield return SettleAndCapture(prefix + (_arrowStory ? "hit.png" : "miss.png"));

            // Group 3: tap after the cue appears but before the window opens.
            yield return WaitFor(() => Active() != null && !Active().IsTapped
                && Now() < Active().WindowStartTime - EARLY_MARGIN_SECONDS);
            _tapNextFrame = true;
            yield return WaitFor(() => _groupsJudged >= 3);
            yield return SettleAndCapture(prefix + "early.png");

            if (_arrowStory && !_bothCuesCaptured)
            {
                Debug.LogWarning("Evidence capture: a charge cue and an arrow cue were never visible together.");
            }

            Application.Quit(0);
        }

        private void LateUpdate()
        {
            if (!_arrowStory || _bothCuesCaptured || _view == null || _view.Controller == null)
            {
                return;
            }

            bool charge = false;
            bool arrow = false;
            foreach (PendingImpact impact in _view.Controller.Pending)
            {
                if (impact.IsCueVisible(Now()))
                {
                    charge |= impact.Kind == TimingCueKind.Charge;
                    arrow |= impact.Kind == TimingCueKind.Arrow;
                }
            }

            if (charge && arrow)
            {
                _bothCuesCaptured = true;
                StartCoroutine(Capture("story-003-both-cues.png"));
            }
        }

        private void CountGroup(TimingOutcome outcome)
        {
            int tick = _view.Controller.Simulation.Tick;
            if (tick != _lastJudgedTick)
            {
                _lastJudgedTick = tick;
                _groupsJudged++;
                Debug.Log("Evidence capture: group " + _groupsJudged + " judged " + outcome + " at tick " + tick);
            }
        }

        /// <summary>
        /// The soonest visible cue of the story's kind, and only while it is also the soonest cue overall,
        /// so a scripted tap (which goes to the soonest cue) lands on it.
        /// </summary>
        private PendingImpact Active()
        {
            PendingImpact active = _view.Controller.GetActiveCue();
            TimingCueKind kind = _arrowStory ? TimingCueKind.Arrow : TimingCueKind.Charge;
            return active != null && active.Kind == kind ? active : null;
        }

        private float Now() => _view.Controller.Now;

        private IEnumerator WaitFor(System.Func<bool> condition)
        {
            while (!condition())
            {
                if (_view.Controller.Simulation.IsFinished)
                {
                    // A battle can have fewer groups than shots (arrows only fly at charging units), so the
                    // run continues in a restarted battle, as a player tap on the end banner does.
                    Debug.Log("Evidence capture: battle ended, restarting it.");
                    _lastJudgedTick = -1;
                    _tapNextFrame = true;
                    yield return null;
                    yield return null;
                    continue;
                }

                yield return null;
            }
        }

        private IEnumerator SettleAndCapture(string fileName)
        {
            for (int i = 0; i < SETTLE_FRAMES; i++)
            {
                yield return null;
            }

            yield return Capture(fileName);
        }

        private IEnumerator Capture(string fileName)
        {
            // Battle time stands still while the file is written, so a slow frame cannot skip a window.
            Time.timeScale = 0f;
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(_directory, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Evidence capture: saved " + path);
            yield return null;
            yield return null;
            Time.timeScale = 1f;
        }
    }
}
