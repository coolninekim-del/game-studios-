using System;
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
    /// Story 003: enemy arrow volleys in the simulation, the arrow timing judgement applied through
    /// OnArrowVolley, its independence from the charge judgement, and the arrow data keys. Fixtures come from
    /// the factory functions below; only the data-file tests read the shipped JSON, on purpose, to keep those
    /// files valid.
    /// </summary>
    public class ArrowTimingTests
    {
        private const float FRAME_SECONDS = 1f / 60f;
        private const int ARROW_DAMAGE = 12;
        private const float ARROW_SUCCESS_MULTIPLIER = 0.25f;

        private static BattleRules MakeBattleRules(int arrowRange = 4)
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
                DisengageDistance = 3,
                ArrowRange = arrowRange,
                ArrowDamage = ARROW_DAMAGE,
                ArrowIntervalTicks = 6
            };
        }

        /// <summary>Charge window 0.2 s and arrow window 0.25 s, so a tap 0.23 s early splits them.</summary>
        private static ChargeTimingRules MakeTimingRules()
        {
            return new ChargeTimingRules
            {
                SecondsPerTick = 0.4f,
                WindowSeconds = 0.2f,
                CueLeadSeconds = 1.2f,
                SuccessPowerMultiplier = 1.5f,
                ArrowWindowSeconds = 0.25f,
                ArrowCueLeadSeconds = 1.0f,
                ArrowSuccessDamageMultiplier = ARROW_SUCCESS_MULTIPLIER
            };
        }

        /// <summary>
        /// One player unit against one enemy unit 8 cells away on row 0. Both close 1 cell per tick, so the
        /// enemy's first volley is at tick 2 (distance 4) and the charge impact at tick 4.
        /// </summary>
        private static BattleSetup MakeDuelSetup()
        {
            return new BattleSetup
            {
                PlayerUnits = new List<UnitSetup> { MakePlayer(0, 0) },
                EnemyUnits = new List<UnitSetup> { MakeEnemy(8, 0) }
            };
        }

        /// <summary>
        /// The duel plus a second enemy 12 cells away behind the first. It closes on the player 2 cells per
        /// tick and first comes within arrow range at tick 4, the same tick as the player's charge impact.
        /// </summary>
        private static BattleSetup MakeSameTickSetup()
        {
            BattleSetup setup = MakeDuelSetup();
            setup.EnemyUnits.Add(MakeEnemy(12, 0));
            return setup;
        }

        /// <summary>Three player units against three enemy units, row by row (the sample layout).</summary>
        private static BattleSetup MakeLineSetup()
        {
            var setup = new BattleSetup();
            for (int row = 1; row <= 5; row += 2)
            {
                setup.PlayerUnits.Add(MakePlayer(0, row));
                setup.EnemyUnits.Add(MakeEnemy(12, row));
            }

            return setup;
        }

        private static UnitSetup MakePlayer(int x, int y)
        {
            return new UnitSetup { Troops = 100, ChargePower = 32f, Defense = 10f, StartX = x, StartY = y };
        }

        private static UnitSetup MakeEnemy(int x, int y)
        {
            return new UnitSetup { Troops = 100, ChargePower = 26f, Defense = 12f, StartX = x, StartY = y };
        }

        /// <summary>Advances frame by frame until <paramref name="condition"/> holds or the battle ends.</summary>
        private static void AdvanceUntil(ChargeTimingController controller, Func<bool> condition)
        {
            while (!condition() && !controller.Simulation.IsFinished)
            {
                controller.Advance(FRAME_SECONDS);
            }
        }

        private static PendingImpact FirstArrow(ChargeTimingController controller)
        {
            foreach (PendingImpact impact in controller.Pending)
            {
                if (impact.Kind == TimingCueKind.Arrow)
                {
                    return impact;
                }
            }

            return null;
        }

        [Test]
        public void ArrowVolley_NoController_KillsArrowDamageTroops()
        {
            // Arrange
            var simulation = new BattleSimulation(MakeBattleRules(), MakeDuelSetup());
            int volleyTick = -1;
            simulation.OnArrowVolley += (s, e) => volleyTick = volleyTick < 0 ? simulation.Tick : volleyTick;

            // Act
            while (volleyTick < 0 && !simulation.IsFinished)
            {
                simulation.Step();
            }

            // Assert
            Assert.AreEqual(2, volleyTick);
            Assert.AreEqual(100 - ARROW_DAMAGE, simulation.Units[0].Troops);
        }

        [Test]
        public void ArrowVolley_ArrowRangeZero_NeverFires()
        {
            // Arrange
            var simulation = new BattleSimulation(MakeBattleRules(arrowRange: 0), MakeLineSetup());
            int volleys = 0;
            simulation.OnArrowVolley += (s, e) => volleys++;

            // Act
            simulation.Run();

            // Assert
            Assert.AreEqual(0, volleys);
        }

        [Test]
        public void ArrowVolley_WholeBattle_OnlyAtChargingPlayersBetweenImpactAndArrowRange()
        {
            // Arrange
            BattleRules rules = MakeBattleRules();
            var simulation = new BattleSimulation(rules, MakeLineSetup());
            int volleys = 0;
            simulation.OnArrowVolley += (s, e) =>
            {
                volleys++;
                int distance = Math.Max(Math.Abs(e.Archer.X - e.Target.X), Math.Abs(e.Archer.Y - e.Target.Y));
                Assert.AreEqual(BattleSide.Enemy, e.Archer.Side);
                Assert.AreEqual(BattleSide.Player, e.Target.Side);
                Assert.AreEqual(UnitPhase.Charging, e.Target.Phase);
                Assert.Greater(distance, rules.ImpactRange);
                Assert.LessOrEqual(distance, rules.ArrowRange);
            };

            // Act
            simulation.Run();

            // Assert
            Assert.Greater(volleys, 0);
        }

        [Test]
        public void ArrowTap_InsideWindow_ReducesDamageByDataMultiplier()
        {
            // Arrange
            var controller = new ChargeTimingController(new BattleSimulation(MakeBattleRules(), MakeDuelSetup()), MakeTimingRules());
            ArrowJudgedEventArgs judged = null;
            controller.OnArrowJudged += (s, e) => judged ??= e;

            // Act
            AdvanceUntil(controller, () => FirstArrow(controller).IsWindowOpen(controller.Now));
            Assert.IsTrue(controller.Tap());
            AdvanceUntil(controller, () => judged != null);

            // Assert
            Assert.AreEqual(TimingOutcome.Success, judged.Outcome);
            Assert.AreEqual(ARROW_SUCCESS_MULTIPLIER, judged.DamageMultiplier);
            Assert.AreEqual(100 - (int)Math.Floor(ARROW_DAMAGE * ARROW_SUCCESS_MULTIPLIER), controller.Simulation.Units[0].Troops);
        }

        [Test]
        public void ArrowTap_BeforeWindow_IsEarlyAndTakesFullDamage()
        {
            // Arrange
            var controller = new ChargeTimingController(new BattleSimulation(MakeBattleRules(), MakeDuelSetup()), MakeTimingRules());
            ArrowJudgedEventArgs judged = null;
            controller.OnArrowJudged += (s, e) => judged ??= e;
            PendingImpact arrow = FirstArrow(controller);

            // Act
            AdvanceUntil(controller, () => arrow.IsCueVisible(controller.Now) && controller.Now < arrow.WindowStartTime - 0.1f);
            Assert.IsTrue(controller.Tap());
            AdvanceUntil(controller, () => arrow.IsWindowOpen(controller.Now));
            controller.Tap(); // a second tap must not overwrite the first
            AdvanceUntil(controller, () => judged != null);

            // Assert
            Assert.AreEqual(TimingOutcome.Early, judged.Outcome);
            Assert.AreEqual(1f, judged.DamageMultiplier);
            Assert.AreEqual(100 - ARROW_DAMAGE, controller.Simulation.Units[0].Troops);
        }

        [Test]
        public void ArrowNoTap_IsMissAndTakesFullDamage()
        {
            // Arrange
            var controller = new ChargeTimingController(new BattleSimulation(MakeBattleRules(), MakeDuelSetup()), MakeTimingRules());
            ArrowJudgedEventArgs judged = null;
            controller.OnArrowJudged += (s, e) => judged ??= e;

            // Act
            AdvanceUntil(controller, () => judged != null);

            // Assert
            Assert.AreEqual(TimingOutcome.Miss, judged.Outcome);
            Assert.AreEqual(100 - ARROW_DAMAGE, controller.Simulation.Units[0].Troops);
        }

        [Test]
        public void ArrowAndChargeSameTick_OneTapBetweenWindows_JudgedIndependently()
        {
            // Arrange
            var controller = new ChargeTimingController(new BattleSimulation(MakeBattleRules(), MakeSameTickSetup()), MakeTimingRules());
            ArrowJudgedEventArgs arrowAtImpact = null;
            ImpactJudgedEventArgs charge = null;
            controller.OnImpactJudged += (s, e) => charge ??= e;
            controller.OnArrowJudged += (s, e) =>
            {
                if (controller.Simulation.Tick == 4)
                {
                    arrowAtImpact = e;
                }
            };
            const float impactTime = 4 * 0.4f;

            // Act: 0.23 s before the impact is inside the arrow window (0.25) but before the charge window (0.2).
            AdvanceUntil(controller, () => controller.Now >= impactTime - 0.23f);
            Assert.IsTrue(controller.Tap());
            AdvanceUntil(controller, () => charge != null);

            // Assert
            Assert.IsNotNull(arrowAtImpact, "the second enemy's volley must land on the impact tick");
            Assert.AreEqual(4, controller.Simulation.Tick);
            Assert.AreEqual(TimingOutcome.Success, arrowAtImpact.Outcome);
            Assert.AreEqual(TimingOutcome.Early, charge.Outcome);
            Assert.AreEqual(1f, charge.PowerMultiplier);
        }

        [Test]
        public void ArrowAndChargeSameTick_TapInsideBothWindows_BothSucceed()
        {
            // Arrange
            var controller = new ChargeTimingController(new BattleSimulation(MakeBattleRules(), MakeSameTickSetup()), MakeTimingRules());
            ArrowJudgedEventArgs arrowAtImpact = null;
            ImpactJudgedEventArgs charge = null;
            controller.OnImpactJudged += (s, e) => charge ??= e;
            controller.OnArrowJudged += (s, e) =>
            {
                if (controller.Simulation.Tick == 4)
                {
                    arrowAtImpact = e;
                }
            };
            const float impactTime = 4 * 0.4f;

            // Act
            AdvanceUntil(controller, () => controller.Now >= impactTime - 0.1f);
            Assert.IsTrue(controller.Tap());
            AdvanceUntil(controller, () => charge != null);

            // Assert
            Assert.AreEqual(TimingOutcome.Success, arrowAtImpact.Outcome);
            Assert.AreEqual(TimingOutcome.Success, charge.Outcome);
            Assert.AreEqual(1.5f, charge.PowerMultiplier);
        }

        [Test]
        public void ParseRules_ShippedBattleRules_EnablesArrows()
        {
            BattleRules rules = BattleDataLoader.ParseRules(ReadData("battle_rules.json"));

            Assert.Greater(rules.ArrowRange, rules.ImpactRange);
            Assert.Greater(rules.ArrowDamage, 0);
            Assert.GreaterOrEqual(rules.ArrowIntervalTicks, 1);
        }

        [Test]
        public void ParseRules_ArrowRangeNotBeyondImpactRange_Throws()
        {
            string json = ReadData("battle_rules.json").Replace("\"arrow_range\": 4", "\"arrow_range\": 1");

            Assert.Throws<InvalidDataException>(() => BattleDataLoader.ParseRules(json));
        }

        [Test]
        public void ParseTimingRules_ShippedDataFile_HasValidArrowValues()
        {
            ChargeTimingRules rules = ChargeTimingDataLoader.ParseRules(ReadData("battle_charge_timing.json"));

            Assert.Greater(rules.ArrowWindowSeconds, 0f);
            Assert.GreaterOrEqual(rules.ArrowCueLeadSeconds, rules.ArrowWindowSeconds);
            Assert.That(rules.ArrowSuccessDamageMultiplier, Is.InRange(0f, 1f));
        }

        [Test]
        public void ParseTimingRules_ArrowMultiplierAboveOne_Throws()
        {
            const string json = "{\"seconds_per_tick\":0.4,\"window_seconds\":0.2,\"cue_lead_seconds\":1.2,"
                + "\"success_power_multiplier\":1.5,\"arrow_window_seconds\":0.25,\"arrow_cue_lead_seconds\":1.0,"
                + "\"arrow_success_damage_multiplier\":1.5}";

            Assert.Throws<InvalidDataException>(() => ChargeTimingDataLoader.ParseRules(json));
        }

        private static string ReadData(string fileName)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, "Data", "Battle", fileName));
        }
    }
}
