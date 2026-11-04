using System.Collections;
using System.IO;
using KnightOrder.Battle.Timing;
using UnityEngine;

namespace KnightOrder.Battle.Presentation
{
    /// <summary>
    /// Run-and-observe capture for Story 002 (see .claude/docs/run-and-observe.md). Does nothing unless the
    /// player is launched with <c>--evidence-dir &lt;absolute dir&gt;</c>. It then plays the battle with a
    /// scripted tap per impact group (one tick's player impacts): group 1 tapped inside the window, group 2
    /// not tapped, group 3 tapped before the window; and saves window-open, success, miss and early
    /// screenshots into that directory before quitting. Scripted taps go through the same
    /// <see cref="ChargeTimingController.Tap"/> as pointer taps.
    /// Implements: Story 002 (test evidence).
    ///
    /// Usage:
    /// <code>
    /// KnightOrder.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 --evidence-dir C:/abs/evidence
    /// </code>
    /// </summary>
    public sealed class ChargeTimingEvidenceCapture : MonoBehaviour
    {
        private const int SETTLE_FRAMES = 6;
        private const float EARLY_MARGIN_SECONDS = 0.15f;

        private string _directory;
        private ChargeTimingBattleView _view;
        private bool _tapNextFrame;
        private int _groupsJudged;
        private int _lastJudgedTick = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string directory = Arg("--evidence-dir");
            if (directory == null)
            {
                return;
            }

            var go = new GameObject(nameof(ChargeTimingEvidenceCapture));
            DontDestroyOnLoad(go);
            go.AddComponent<ChargeTimingEvidenceCapture>()._directory = directory;
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
            _view.OnImpactJudged += (s, e) =>
            {
                int tick = _view.Controller.Simulation.Tick;
                if (tick != _lastJudgedTick)
                {
                    _lastJudgedTick = tick;
                    _groupsJudged++;
                    Debug.Log("Evidence capture: group " + _groupsJudged + " judged " + e.Outcome + " at tick " + tick);
                }
            };

            // Group 1: show the open window, then tap inside it.
            yield return WaitFor(() => Active() != null && Active().IsWindowOpen(Now()));
            yield return Capture("story-002-window-open.png");
            _tapNextFrame = true;
            yield return WaitFor(() => _groupsJudged >= 1);
            yield return SettleAndCapture("story-002-success.png");

            // Group 2: no tap.
            yield return WaitFor(() => _groupsJudged >= 2);
            yield return SettleAndCapture("story-002-miss.png");

            // Group 3: tap after the cue appears but before the window opens.
            yield return WaitFor(() => Active() != null && !Active().IsTapped
                && Now() < Active().WindowStartTime - EARLY_MARGIN_SECONDS);
            _tapNextFrame = true;
            yield return WaitFor(() => _groupsJudged >= 3);
            yield return SettleAndCapture("story-002-early.png");

            Application.Quit(0);
        }

        private PendingImpact Active() => _view.Controller.GetActiveCue();

        private float Now() => _view.Controller.Now;

        private IEnumerator WaitFor(System.Func<bool> condition)
        {
            while (!condition())
            {
                if (_view.Controller.Simulation.IsFinished)
                {
                    Debug.LogError("Evidence capture: battle ended before all shots were taken.");
                    Application.Quit(1);
                    yield break;
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
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(_directory, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Evidence capture: saved " + path);
            yield return null;
            yield return null;
        }
    }
}
