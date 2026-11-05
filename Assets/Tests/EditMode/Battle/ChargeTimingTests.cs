using System.Collections.Generic;
using System.IO;
using KnightOrder.Battle;
using KnightOrder.Battle.Loading;
using KnightOrder.Battle.Timing;
using NUnit.Framework;
using UnityEngine;

namespace KnightOrder.Tests.Battle
{
    /// <summary>
    /// Story 002: charge timing judgement, the multiplier applied through OnChargeImpact, the window hook
    /// and the timing data loader. Fixtures come from the factory functions below; only the data-file test
    /// reads the shipped battle_charge_timing.json, on purpose, to keep that file valid.
    /// </summary>
    public class ChargeTimingTests
    {
        private const float FRAME_SECONDS = 1f / 60f;

        private static BattleRules MakeBattleRules()
        {
            return new BattleRules
            {
                PowerScale = 1f,
                DefenseScale = 0.5f,
                MinDamage = 1,
                CellsPerTick = 1,
                ImpactRange = 1,
                ImpactIntervalTicks = 3,
                MaxTicks = 200,
                ReferenceTroops = 100f,
                TroopExponent = 0.5f,
                ChargesBeforeMelee = 2,
                DisengageDistance = 3
            };
        }

        private static ChargeTimingRules MakeTimingRules()
        {
            return new ChargeTimingRules
            {
                SecondsPerTick = 0.4f,
                WindowSeconds = 0.2f,
                CueLeadSeconds = 1.2f,
                SuccessPowerMultiplier = 1.5f
            };
        }

        /// <summary>One player unit and one enemy unit facing each other on row 0.</summary>
        private static BattleSetup MakeDuelSetup()
        {
            return new BattleSetup
            {
                PlayerUnits = new List<UnitSetup>
                {
                    new UnitSetup { Troops = 100, ChargePower = 32f, Defense = 10f, StartX = 0, StartY = 0 }
                },
                EnemyUnits = new List<UnitSetup>
                {
                    new UnitSetup { Troops = 100, ChargePower = 26f, Defense = 12f, StartX = 8, StartY = 0 }
                }
            };
        }

        /// <summary>Three player units against three enemy units, row by row.</summary>
        private static BattleSetup MakeLineSetup()
        {
            var setup = new BattleSetup();
            for (int row = 1; row <= 5; row += 2)
            {
                setup.PlayerUnits.Add(new UnitSetup { Troops = 100, ChargePower = 32f, Defense = 10f, StartX = 0, StartY = row });
                setup.EnemyUnits.Add(new UnitSetup { Troops = 100, ChargePower = 26f, Defense = 12f, StartX = 12, StartY = row });
            }

            return setup;
        }

        private enum TapPolicy
        {
            Never,
            InWindow,
            EarlyThenInWindow
        }

        /// <summary>Runs frames until the first player impact is judged, tapping per policy.</summary>
        private static ImpactJudgedEventArgs RunUntilFirstJudgement(ChargeTimingController controller, TapPolicy policy)
        {
            ImpactJudgedEventArgs judged = null;
            controller.OnImpactJudged += (s, e) => judged ??= e;
            bool tappedEarly = false;
            while (judged == null && !controller.Simulation.IsFinished)
            {
                PendingImpact next = controller.Pending.Count > 0 ? controller.Pending[0] : null;
                if (next != null && !next.IsTapped)
                {
                    if (policy == TapPolicy.EarlyThenInWindow && !tappedEarly && next.IsCueVisible(controller.Now)
                        && !next.IsWindowOpen(controller.Now))
                    {
                        Assert.IsTrue(controller.Tap());
                        tappedEarly = true;
                    }
                    else if (policy != TapPolicy.Never && next.IsWindowOpen(controller.Now))
                    {
                        controller.Tap();
                    }
                }
                else if (policy == TapPolicy.EarlyThenInWindow && next != null && next.IsWindowOpen(controller.Now))
                {
                    controller.Tap(); // a second tap must not overwrite the first
                }

                controller.Advance(FRAME_SECONDS);
            }

            return judged;
        }

        [Test]
        public void Judge_TapInsideWindow_ReturnsSuccess()
        {
            Assert.AreEqual(TimingOutcome.Success, ChargeTimingJudge.Judge(2.0f, 0.2f, 1.9f));
        }

        [Test]
        public void Judge_TapOnWindowEdges_ReturnsSuccess()
        {
            Assert.AreEqual(TimingOutcome.Success, ChargeTimingJudge.Judge(2.0f, 0.25f, 1.75f));
            Assert.AreEqual(TimingOutcome.Success, ChargeTimingJudge.Judge(2.0f, 0.25f, 2.0f));
        }

        [Test]
        public void Judge_TapBeforeWindow_ReturnsEarly()
        {
            Assert.AreEqual(TimingOutcome.Early, ChargeTimingJudge.Judge(2.0f, 0.25f, 1.7f));
        }

        [Test]
        public void Judge_NoTapOrTapAfterImpact_ReturnsMiss()
        {
            Assert.AreEqual(TimingOutcome.Miss, ChargeTimingJudge.Judge(2.0f, 0.2f, null));
            Assert.AreEqual(TimingOutcome.Miss, ChargeTimingJudge.Judge(2.0f, 0.2f, 2.01f));
        }

        [Test]
        public void PowerMultiplier_SuccessUsesDataValue_OthersAreDefault()
        {
            Assert.AreEqual(1.5f, ChargeTimingJudge.PowerMultiplier(TimingOutcome.Success, 1.5f));
            Assert.AreEqual(1.0f, ChargeTimingJudge.PowerMultiplier(TimingOutcome.Early, 1.5f));
            Assert.AreEqual(1.0f, ChargeTimingJudge.PowerMultiplier(TimingOutcome.Miss, 1.5f));
        }

        [Test]
        public void Tap_NoCueVisible_IsIgnored()
        {
            var controller = new ChargeTimingController(
                new BattleSimulation(MakeBattleRules(), MakeDuelSetup()), MakeTimingRules());

            Assert.IsFalse(controller.Tap());
        }

        [Test]
        public void FirstImpact_TapInWindow_AppliesSuccessMultiplierToDamage()
        {
            BattleRules rules = MakeBattleRules();
            ChargeTimingRules timing = MakeTimingRules();
            var controller = new ChargeTimingController(new BattleSimulation(rules, MakeDuelSetup()), timing);

            ImpactJudgedEventArgs judged = RunUntilFirstJudgement(controller, TapPolicy.InWindow);

            Assert.AreEqual(TimingOutcome.Success, judged.Outcome);
            Assert.AreEqual(timing.SuccessPowerMultiplier, judged.PowerMultiplier);
            // troops 100 = ReferenceTroops, so the troop multiplier is 1.
            int expectedDamage = Mathf.FloorToInt(32f * timing.SuccessPowerMultiplier * rules.PowerScale - 12f * rules.DefenseScale);
            Assert.AreEqual(100 - expectedDamage, controller.Simulation.Units[judged.DefenderIndex].Troops);
        }

        [Test]
        public void FirstImpact_NoTap_DealsDefaultDamage()
        {
            BattleRules rules = MakeBattleRules();
            var controller = new ChargeTimingController(new BattleSimulation(rules, MakeDuelSetup()), MakeTimingRules());

            ImpactJudgedEventArgs judged = RunUntilFirstJudgement(controller, TapPolicy.Never);

            Assert.AreEqual(TimingOutcome.Miss, judged.Outcome);
            Assert.AreEqual(1.0f, judged.PowerMultiplier);
            int expectedDamage = Mathf.FloorToInt(32f * rules.PowerScale - 12f * rules.DefenseScale);
            Assert.AreEqual(100 - expectedDamage, controller.Simulation.Units[judged.DefenderIndex].Troops);
        }

        [Test]
        public void FirstImpact_TapBeforeWindow_IsEarlyAndLaterTapDoesNotOverride()
        {
            var controller = new ChargeTimingController(
                new BattleSimulation(MakeBattleRules(), MakeDuelSetup()), MakeTimingRules());

            ImpactJudgedEventArgs judged = RunUntilFirstJudgement(controller, TapPolicy.EarlyThenInWindow);

            Assert.AreEqual(TimingOutcome.Early, judged.Outcome);
            Assert.AreEqual(1.0f, judged.PowerMultiplier);
        }

        [Test]
        public void WholeBattle_TapEveryWindow_EveryPlayerImpactSucceeds()
        {
            BattleSetup setup = MakeLineSetup();
            var controller = new ChargeTimingController(
                new BattleSimulation(MakeBattleRules(), setup), MakeTimingRules());
            var outcomes = new List<TimingOutcome>();
            controller.OnImpactJudged += (s, e) => outcomes.Add(e.Outcome);

            while (!controller.Simulation.IsFinished)
            {
                foreach (PendingImpact impact in controller.Pending)
                {
                    if (!impact.IsTapped && impact.IsWindowOpen(controller.Now))
                    {
                        controller.Tap();
                        break;
                    }
                }

                controller.Advance(FRAME_SECONDS);
            }

            // An unpredicted impact would be judged Miss, so all-Success also proves the prediction matched.
            Assert.IsNotEmpty(outcomes);
            CollectionAssert.AreEqual(outcomes.ConvertAll(_ => TimingOutcome.Success), outcomes);
        }

        [Test]
        public void SameSetup_WithAndWithoutTaps_SuccessGivesPlayerMoreRemainingTroops()
        {
            BattleSetup setup = MakeLineSetup();
            BattleResult withoutTaps = new BattleSimulation(MakeBattleRules(), setup).Run();
            var controller = new ChargeTimingController(new BattleSimulation(MakeBattleRules(), setup), MakeTimingRules());
            while (!controller.Simulation.IsFinished)
            {
                if (controller.Pending.Count > 0 && controller.Pending[0].IsWindowOpen(controller.Now))
                {
                    controller.Tap();
                }

                controller.Advance(FRAME_SECONDS);
            }

            BattleResult withTaps = controller.Simulation.GetResult();
            Assert.Greater(withTaps.PlayerTroops - withTaps.EnemyTroops, withoutTaps.PlayerTroops - withoutTaps.EnemyTroops);
        }

        [Test]
        public void WindowModifier_WidensWindow_TurnsEarlyTapIntoSuccess()
        {
            ChargeTimingRules timing = MakeTimingRules();
            var controller = new ChargeTimingController(
                new BattleSimulation(MakeBattleRules(), MakeDuelSetup()), timing, new ScaleWindow(3f));
            ImpactJudgedEventArgs judged = null;
            controller.OnImpactJudged += (s, e) => judged ??= e;
            while (judged == null && !controller.Simulation.IsFinished)
            {
                PendingImpact next = controller.Pending.Count > 0 ? controller.Pending[0] : null;
                float defaultWindowStart = next != null ? next.ImpactTime - timing.WindowSeconds : float.MaxValue;
                if (next != null && !next.IsTapped && next.IsWindowOpen(controller.Now) && controller.Now < defaultWindowStart)
                {
                    controller.Tap(); // inside the widened window, before the default one opens
                }

                controller.Advance(FRAME_SECONDS);
            }

            Assert.IsNotNull(judged);
            Assert.AreEqual(TimingOutcome.Success, judged.Outcome);
        }

        [Test]
        public void ParseRules_ShippedDataFile_LoadsValidRules()
        {
            ChargeTimingRules rules = ChargeTimingDataLoader.ParseRules(ReadData("battle_charge_timing.json"));

            Assert.Greater(rules.SecondsPerTick, 0f);
            Assert.Greater(rules.WindowSeconds, 0f);
            Assert.GreaterOrEqual(rules.CueLeadSeconds, rules.WindowSeconds);
            Assert.GreaterOrEqual(rules.SuccessPowerMultiplier, 1f);
        }

        [Test]
        public void ParseRules_CueShorterThanWindow_Throws()
        {
            const string json = "{\"seconds_per_tick\":0.4,\"window_seconds\":0.5,\"cue_lead_seconds\":0.3,\"success_power_multiplier\":1.5}";

            Assert.Throws<InvalidDataException>(() => ChargeTimingDataLoader.ParseRules(json));
        }

        private static string ReadData(string fileName)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, "Data", "Battle", fileName));
        }

        private sealed class ScaleWindow : IChargeWindowModifier
        {
            private readonly float _scale;

            public ScaleWindow(float scale)
            {
                _scale = scale;
            }

            public float ModifyWindowSeconds(BattleUnit attacker, float baseWindowSeconds) => baseWindowSeconds * _scale;
        }
    }
}
