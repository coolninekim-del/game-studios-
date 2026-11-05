using System;
using System.Collections;
using System.IO;
using KnightOrder.Battle;
using KnightOrder.Battle.Presentation;
using KnightOrder.Battle.Timing;
using KnightOrder.Duel.Presentation;
using KnightOrder.Stable;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KnightOrder.UI.Stable
{
    /// <summary>
    /// Run-and-observe capture for Story 005 (see .claude/docs/run-and-observe.md). Does nothing unless the
    /// player is launched with <c>--evidence-dir &lt;absolute dir&gt; --evidence-story 005</c>; the Stable scene is
    /// then opened by <see cref="StableSceneLauncher"/>. Scripted presses call the same
    /// <see cref="StableView.Train"/> and <see cref="StableView.GoToBattle"/> as the buttons. Steps: the stable
    /// untouched; after training horse speed; after also training horse agility and horse health (no action
    /// left, a fourth training is refused); TO BATTLE, until the mounted protagonist's charge window is open
    /// while the foot units' windows of the same impact are not (the blue part of the timing bar);
    /// a shot of that cue before the hero window opens comes first; then the Duel scene, until the window is
    /// open. Logs the battle charge windows of the protagonist and a foot unit and the duel numbers. Time is
    /// paused while a screenshot is written.
    /// Implements: Story 005 (test evidence).
    ///
    /// Usage:
    /// <code>
    /// KnightOrder.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 --evidence-dir C:/abs/evidence --evidence-story 005
    /// </code>
    /// </summary>
    public sealed class StableEvidenceCapture : MonoBehaviour
    {
        private const int SETTLE_FRAMES = 8;
        private const int FIND_FRAMES = 300;
        private const float BATTLE_SECONDS = 120f;
        private const string PREFIX = "story-005-";

        private string _directory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string directory = StableSceneLauncher.Arg("--evidence-dir");
            if (directory == null || StableSceneLauncher.Arg("--evidence-story") != "005")
            {
                return;
            }

            // Keep running when the window loses focus; otherwise an unattended run pauses forever.
            Application.runInBackground = true;
            var go = new GameObject(nameof(StableEvidenceCapture));
            DontDestroyOnLoad(go);
            go.AddComponent<StableEvidenceCapture>()._directory = directory;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_directory);
            StableView stable = null;
            yield return Find(() => stable = FindAnyObjectByType<StableView>());
            if (stable == null)
            {
                Fail("no StableView in the scene");
                yield break;
            }

            Log(stable.Session.Profile, "start");
            yield return SettleAndCapture(PREFIX + "stable.png");

            TrainingResult speed = stable.Train(KnightStat.HorseSpeed);
            Debug.Log("Evidence capture: trained " + speed.Stat + " " + speed.StatBefore + " -> " + speed.StatAfter);
            yield return SettleAndCapture(PREFIX + "trained-speed.png");

            stable.Train(KnightStat.HorseAgility);
            stable.Train(KnightStat.HorseHp);
            TrainingResult refused = stable.Train(KnightStat.RiderAttack);
            Debug.Log("Evidence capture: fourth training " + (refused == null ? "refused" : "ACCEPTED") + ", actions left "
                + stable.Session.ActionsLeft);
            Log(stable.Session.Profile, "trained");
            yield return SettleAndCapture(PREFIX + "actions-spent.png");

            stable.GoToBattle();
            ChargeTimingBattleView battle = null;
            yield return Find(() => battle = FindAnyObjectByType<ChargeTimingBattleView>());
            if (battle == null)
            {
                Fail("no ChargeTimingBattleView after TO BATTLE");
                yield break;
            }

            // First the cue with the blue hero-only part still closed, then inside that part.
            bool heroCueShown = false;
            while (!heroCueShown && battle.Controller.Now < BATTLE_SECONDS && !battle.Controller.Simulation.IsFinished)
            {
                yield return null;
                ChargeTimingController controller = battle.Controller;
                PendingImpact active = controller.GetActiveCue();
                foreach (PendingImpact impact in controller.Pending)
                {
                    heroCueShown |= active != null && impact.Kind == TimingCueKind.Charge && impact.ImpactTick == active.ImpactTick
                        && impact.WindowSeconds > active.WindowSeconds && controller.Simulation.Units[impact.AttackerIndex].IsMounted
                        && controller.Now < impact.WindowStartTime - 0.1f;
                }
            }

            Debug.Log("Evidence capture: hero cue before its window: " + heroCueShown);
            yield return Capture(PREFIX + "battle-hero-cue.png");

            float heroWindow = -1f;
            float footWindow = -1f;
            bool heroOnlyOpen = false;
            while (!heroOnlyOpen && battle.Controller.Now < BATTLE_SECONDS && !battle.Controller.Simulation.IsFinished)
            {
                yield return null;
                ChargeTimingController controller = battle.Controller;
                foreach (PendingImpact impact in controller.Pending)
                {
                    if (impact.Kind != TimingCueKind.Charge)
                    {
                        continue;
                    }

                    if (controller.Simulation.Units[impact.AttackerIndex].IsMounted)
                    {
                        heroWindow = impact.WindowSeconds;
                        PendingImpact active = controller.GetActiveCue();
                        heroOnlyOpen = impact.IsWindowOpen(controller.Now) && active != null && active.ImpactTick == impact.ImpactTick
                            && !active.IsWindowOpen(controller.Now);
                    }
                    else
                    {
                        footWindow = impact.WindowSeconds;
                    }
                }
            }

            BattleUnit hero = FindHero(battle.Controller);
            Debug.Log("Evidence capture: battle hero charge power " + hero?.ChargePower + ", health " + hero?.Troops
                + ", charge window " + heroWindow.ToString("0.000") + "s, foot unit window " + footWindow.ToString("0.000")
                + "s, captured inside the hero-only part: " + heroOnlyOpen);
            yield return Capture(PREFIX + "battle-hero.png");

            SceneManager.LoadScene("Duel");
            DuelView duel = null;
            yield return Find(() => duel = FindAnyObjectByType<DuelView>());
            if (duel == null)
            {
                Fail("no DuelView after loading the Duel scene");
                yield break;
            }

            while (!duel.Duel.IsWindowOpen)
            {
                yield return null;
            }

            Debug.Log("Evidence capture: duel hero lance power " + duel.Duel.HeroPower + ", health " + duel.Duel.HeroMaxHp
                + ", window " + duel.Duel.HeroWindowSeconds.ToString("0.000") + "s");
            yield return Capture(PREFIX + "duel.png");
            Application.Quit(0);
        }

        private static BattleUnit FindHero(ChargeTimingController controller)
        {
            foreach (BattleUnit unit in controller.Simulation.Units)
            {
                if (unit.IsMounted)
                {
                    return unit;
                }
            }

            return null;
        }

        private static void Log(KnightProfile profile, string when)
        {
            Debug.Log("Evidence capture: profile " + when + ": rider attack " + profile.RiderAttack + ", rider hp " + profile.RiderHp
                + ", horse speed " + profile.HorseSpeed + ", horse hp " + profile.HorseHp + ", horse agility " + profile.HorseAgility);
        }

        private static void Fail(string message)
        {
            Debug.LogError("Evidence capture: " + message);
            Application.Quit(1);
        }

        private static IEnumerator Find(Func<object> find)
        {
            for (int frame = 0; frame < FIND_FRAMES && find() == null; frame++)
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
            // Time stands still while the file is written, so a slow frame cannot skip a window.
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
