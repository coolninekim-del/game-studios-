using System.Collections;
using System.IO;
using UnityEngine;

namespace KnightOrder.Duel.Presentation
{
    /// <summary>
    /// Run-and-observe capture for Story 004 (see .claude/docs/run-and-observe.md). Does nothing unless the
    /// player is launched with <c>--evidence-dir &lt;absolute dir&gt; --evidence-story 004</c>; the Duel scene is
    /// then opened by <see cref="DuelSceneLauncher"/>. Scripted taps go through the same
    /// <see cref="DuelSimulation.Tap"/> as pointer taps. Duel 1: pass 1 tapped inside the window at the open
    /// side, pass 2 inside the window at the shield, pass 3 before the window at the open side, every later pass
    /// inside the window at the open side until the duel is won. Duel 2 (restarted): no input until it is lost.
    /// Saves window-open, perfect, shield, early, victory and defeat shots. Duel time is paused while a
    /// screenshot is written.
    /// Implements: Story 004 (test evidence).
    ///
    /// Usage:
    /// <code>
    /// KnightOrder.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 --evidence-dir C:/abs/evidence --evidence-story 004
    /// </code>
    /// </summary>
    public sealed class DuelEvidenceCapture : MonoBehaviour
    {
        private const int SETTLE_FRAMES = 8;
        private const float EARLY_MARGIN_SECONDS = 0.15f;
        private const string PREFIX = "story-004-";

        private string _directory;
        private DuelView _view;
        private LanceAim? _tapNextFrame;
        private int _passesResolved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string directory = DuelSceneLauncher.Arg("--evidence-dir");
            if (directory == null || DuelSceneLauncher.Arg("--evidence-story") != "004")
            {
                return;
            }

            // Keep running when the window loses focus; otherwise an unattended run pauses forever.
            Application.runInBackground = true;
            var go = new GameObject(nameof(DuelEvidenceCapture));
            DontDestroyOnLoad(go);
            go.AddComponent<DuelEvidenceCapture>()._directory = directory;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_directory);
            for (int frame = 0; _view == null && frame < 300; frame++)
            {
                yield return null;
                _view = FindAnyObjectByType<DuelView>();
            }

            if (_view == null)
            {
                Debug.LogError("Evidence capture: no DuelView in the scene.");
                Application.Quit(1);
                yield break;
            }

            _view.ExtraTapSource = _ =>
            {
                LanceAim? tap = _tapNextFrame;
                _tapNextFrame = null;
                return tap;
            };
            _view.OnPassResolved += (s, e) =>
            {
                _passesResolved++;
                Debug.Log("Evidence capture: pass " + (e.PassIndex + 1) + " " + e.Outcome + (e.HitShield ? " (shield)" : "")
                    + ", boss -" + e.DamageToBoss + ", hero -" + e.DamageToHero
                    + ", hp hero " + _view.Duel.HeroHp + " boss " + _view.Duel.BossHp);
            };

            // Pass 1: show the open window, then tap inside it at the open side.
            yield return WaitFor(() => Duel.IsWindowOpen);
            yield return Capture(PREFIX + "window-open.png");
            _tapNextFrame = OpenSide();
            yield return WaitFor(() => _passesResolved >= 1);
            yield return SettleAndCapture(PREFIX + "perfect.png");

            // Pass 2: inside the window at the shield.
            yield return WaitFor(() => !Duel.IsTurning && Duel.IsWindowOpen);
            _tapNextFrame = Duel.BossGuard;
            yield return WaitFor(() => _passesResolved >= 2);
            yield return SettleAndCapture(PREFIX + "shield.png");

            // Pass 3: after the shield is shown, before the window, at the open side.
            yield return WaitFor(() => !Duel.IsTurning && Duel.IsGuardRevealed
                && Duel.Now < Duel.WindowStartTime - EARLY_MARGIN_SECONDS);
            _tapNextFrame = OpenSide();
            yield return WaitFor(() => _passesResolved >= 3);
            yield return SettleAndCapture(PREFIX + "early.png");

            // The rest: inside the window at the open side until the duel is won.
            while (!Duel.IsFinished)
            {
                int resolved = _passesResolved;
                yield return WaitFor(() => Duel.IsFinished || (!Duel.IsTurning && Duel.IsWindowOpen));
                if (Duel.IsFinished)
                {
                    break;
                }

                _tapNextFrame = OpenSide();
                yield return WaitFor(() => _passesResolved > resolved);
            }

            Debug.Log("Evidence capture: duel 1 " + (Duel.GetResult().Victory ? "won" : "lost")
                + ", growth +" + Duel.GetResult().GrowthReward);
            yield return SettleAndCapture(PREFIX + "victory.png");

            // Duel 2: restart with a tap on the banner, then no input.
            _tapNextFrame = LanceAim.High;
            yield return null;
            yield return null;
            yield return WaitFor(() => Duel.IsFinished);
            Debug.Log("Evidence capture: duel 2 (no input) " + (Duel.GetResult().Victory ? "won" : "lost")
                + " after " + Duel.GetResult().PassesFought + " passes");
            yield return SettleAndCapture(PREFIX + "defeat.png");

            Application.Quit(0);
        }

        private DuelSimulation Duel => _view.Duel;

        private LanceAim OpenSide() => Duel.BossGuard == LanceAim.High ? LanceAim.Low : LanceAim.High;

        private static IEnumerator WaitFor(System.Func<bool> condition)
        {
            while (!condition())
            {
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
            // Duel time stands still while the file is written, so a slow frame cannot skip a window.
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
