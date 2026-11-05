using System.Collections.Generic;
using System.IO;
using KnightOrder.Battle;
using KnightOrder.Battle.Loading;
using KnightOrder.Troops;
using KnightOrder.Troops.Loading;
using NUnit.Framework;
using UnityEngine;

namespace KnightOrder.Tests.Troops
{
    /// <summary>
    /// Story 006: squads with only a troop count and a grade; reinforcing and promoting for gold (refused requests
    /// change nothing); grades raising the battle numbers; battle losses written back to the roster; the wiped-out
    /// report. Fixtures come from the factory functions below; only the data-file test reads the shipped troop and
    /// battle files, on purpose, to keep them valid together.
    /// </summary>
    public class TroopTests
    {
        private static TroopRules MakeRules()
        {
            return new TroopRules
            {
                MaxTroopsPerSquad = 150,
                ReinforceGoldPerTroop = 1,
                PromoteGoldCosts = new List<int> { 60, 120, 200 },
                PowerPerGrade = 0.15f,
                DefensePerGrade = 2f
            };
        }

        private static TroopRoster MakeRoster(int gold, int troops = 100)
        {
            var roster = new TroopRoster { Gold = gold };
            for (int i = 0; i < 3; i++)
            {
                roster.Squads.Add(new Squad { Troops = troops, Grade = 1 });
            }

            return roster;
        }

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

        /// <summary>Three player foot units against three enemy units, row by row.</summary>
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

        /// <summary>Troops the enemy facing squad 0 loses on squad 0's first impact.</summary>
        private static int FirstImpactDamageOfSquad0(TroopRoster roster)
        {
            BattleSetup setup = MakeLineSetup();
            TroopDeployment deployment = TroopDeployment.Deploy(setup, roster, MakeRules());
            var sim = new BattleSimulation(MakeBattleRules(), setup);
            BattleUnit squad0 = sim.Units[deployment.UnitIndexOf(0)];
            BattleUnit facing = sim.Units[setup.PlayerUnits.Count];
            while (squad0.ChargesDone == 0 && !sim.IsFinished)
            {
                sim.Step();
            }

            return 100 - facing.Troops;
        }

        [Test]
        public void Reinforce_EnoughGold_AddsTroopsAndSpendsGold()
        {
            TroopRoster roster = MakeRoster(gold: 100, troops: 60);

            Assert.AreEqual(TroopActionResult.Done, roster.Reinforce(0, 30, MakeRules()));

            Assert.AreEqual(90, roster.Squads[0].Troops);
            Assert.AreEqual(70, roster.Gold);
            Assert.AreEqual(60, roster.Squads[1].Troops);
        }

        [Test]
        public void Reinforce_NotEnoughGold_RefusedAndUnchanged()
        {
            TroopRoster roster = MakeRoster(gold: 10, troops: 60);

            Assert.AreEqual(TroopActionResult.NotEnoughGold, roster.Reinforce(0, 20, MakeRules()));

            Assert.AreEqual(60, roster.Squads[0].Troops);
            Assert.AreEqual(10, roster.Gold);
        }

        [Test]
        public void Reinforce_AboveSquadMaximum_RefusedButUpToMaximumAllowed()
        {
            TroopRoster roster = MakeRoster(gold: 100, troops: 140);

            Assert.AreEqual(TroopActionResult.SquadFull, roster.Reinforce(0, 11, MakeRules()));
            Assert.AreEqual(140, roster.Squads[0].Troops);
            Assert.AreEqual(100, roster.Gold);

            Assert.AreEqual(TroopActionResult.Done, roster.Reinforce(0, 10, MakeRules()));
            Assert.AreEqual(150, roster.Squads[0].Troops);
        }

        [TestCase(0, 0)]
        [TestCase(-1, 10)]
        [TestCase(3, 10)]
        public void Reinforce_BadSquadOrCount_InvalidRequestAndUnchanged(int squadIndex, int count)
        {
            TroopRoster roster = MakeRoster(gold: 100, troops: 60);

            Assert.AreEqual(TroopActionResult.InvalidRequest, roster.Reinforce(squadIndex, count, MakeRules()));
            Assert.AreEqual(100, roster.Gold);
            Assert.AreEqual(180, roster.TotalTroops);
        }

        [Test]
        public void Promote_EnoughGold_RaisesGradeAndSpendsThatGradesCost()
        {
            TroopRoster roster = MakeRoster(gold: 200);

            Assert.AreEqual(TroopActionResult.Done, roster.Promote(1, MakeRules()));
            Assert.AreEqual(2, roster.Squads[1].Grade);
            Assert.AreEqual(140, roster.Gold);

            Assert.AreEqual(TroopActionResult.Done, roster.Promote(1, MakeRules()));
            Assert.AreEqual(3, roster.Squads[1].Grade);
            Assert.AreEqual(20, roster.Gold);
        }

        [Test]
        public void Promote_NotEnoughGold_RefusedAndUnchanged()
        {
            TroopRoster roster = MakeRoster(gold: 59);

            Assert.AreEqual(TroopActionResult.NotEnoughGold, roster.Promote(0, MakeRules()));
            Assert.AreEqual(1, roster.Squads[0].Grade);
            Assert.AreEqual(59, roster.Gold);
        }

        [Test]
        public void Promote_HighestGrade_RefusedAndUnchanged()
        {
            TroopRoster roster = MakeRoster(gold: 1000);
            roster.Squads[0].Grade = 4;

            Assert.AreEqual(TroopActionResult.MaxGrade, roster.Promote(0, MakeRules()));
            Assert.AreEqual(4, roster.Squads[0].Grade);
            Assert.AreEqual(1000, roster.Gold);
        }

        [Test]
        public void Deploy_HigherGrade_RaisesChargePowerAndDefenseByRules()
        {
            TroopRoster roster = MakeRoster(gold: 0, troops: 80);
            roster.Squads[0].Grade = 3;
            BattleSetup setup = MakeLineSetup();

            TroopDeployment.Deploy(setup, roster, MakeRules());

            // Grade 3: 32 * (1 + 2 * 0.15) = 41.6 and 10 + 2 * 2 = 14; grade 1 keeps the setup's values.
            Assert.AreEqual(41.6f, setup.PlayerUnits[0].ChargePower, 1e-4f);
            Assert.AreEqual(14f, setup.PlayerUnits[0].Defense, 1e-4f);
            Assert.AreEqual(32f, setup.PlayerUnits[1].ChargePower, 1e-4f);
            Assert.AreEqual(10f, setup.PlayerUnits[1].Defense, 1e-4f);
            Assert.AreEqual(80, setup.PlayerUnits[2].Troops);
        }

        [Test]
        public void HigherGrade_NextBattle_SquadHitsHarder()
        {
            TroopRoster plain = MakeRoster(gold: 0);
            TroopRoster promoted = MakeRoster(gold: 0);
            promoted.Squads[0].Grade = 2;

            // 32 - 12 * 0.5 = 26; grade 2: floor(36.8 - 6) = 30.
            Assert.AreEqual(26, FirstImpactDamageOfSquad0(plain));
            Assert.AreEqual(30, FirstImpactDamageOfSquad0(promoted));
        }

        [Test]
        public void BattleLosses_RecordSurvivors_RosterKeepsEachSquadsRemainingTroops()
        {
            TroopRoster roster = MakeRoster(gold: 0);
            BattleSetup setup = MakeLineSetup();
            TroopDeployment deployment = TroopDeployment.Deploy(setup, roster, MakeRules());
            var sim = new BattleSimulation(MakeBattleRules(), setup);
            sim.Run();

            deployment.RecordSurvivors(roster, sim);

            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(sim.Units[deployment.UnitIndexOf(i)].Troops, roster.Squads[i].Troops, "squad " + i);
            }

            Assert.Less(roster.TotalTroops, 300, "the battle cost troops");
            Assert.AreEqual(1, roster.Squads[0].Grade, "grades are not changed by a battle");
        }

        [Test]
        public void EmptySquad_Deploy_DoesNotFightAndStaysEmpty()
        {
            TroopRoster roster = MakeRoster(gold: 0);
            roster.Squads[1].Troops = 0;
            BattleSetup setup = MakeLineSetup();

            TroopDeployment deployment = TroopDeployment.Deploy(setup, roster, MakeRules());
            var sim = new BattleSimulation(MakeBattleRules(), setup);
            sim.Run();
            deployment.RecordSurvivors(roster, sim);

            Assert.AreEqual(2, setup.PlayerUnits.Count);
            Assert.AreEqual(0, deployment.UnitIndexOf(0));
            Assert.AreEqual(-1, deployment.UnitIndexOf(1));
            Assert.AreEqual(1, deployment.UnitIndexOf(2));
            Assert.AreEqual(5, setup.PlayerUnits[1].StartY, "squad 2 keeps its own start cell");
            Assert.AreEqual(0, roster.Squads[1].Troops);
        }

        [Test]
        public void Deploy_MountedUnitInSetup_LeftUntouchedAndNotASquad()
        {
            BattleSetup setup = MakeLineSetup();
            var hero = new UnitSetup { Troops = 140, ChargePower = 42f, Defense = 10f, StartX = 0, StartY = 4, IsMounted = true };
            setup.PlayerUnits.Insert(1, hero);
            TroopRoster roster = MakeRoster(gold: 0, troops: 70);
            roster.Squads[1].Grade = 2;

            TroopDeployment deployment = TroopDeployment.Deploy(setup, roster, MakeRules());

            Assert.AreEqual(140, hero.Troops);
            Assert.AreEqual(42f, hero.ChargePower);
            Assert.AreEqual(2, deployment.UnitIndexOf(1), "squad 1 is the second foot unit, after the hero");
            Assert.AreEqual(36.8f, setup.PlayerUnits[2].ChargePower, 1e-4f);
        }

        [Test]
        public void IsWipedOut_AllSquadsEmpty_TrueOtherwiseFalse()
        {
            TroopRoster roster = MakeRoster(gold: 0, troops: 0);
            Assert.IsTrue(roster.IsWipedOut);

            roster.Squads[2].Troops = 1;
            Assert.IsFalse(roster.IsWipedOut);
        }

        [Test]
        public void Deploy_SquadCountDiffersFromFootUnits_Throws()
        {
            TroopRoster roster = MakeRoster(gold: 0);
            roster.Squads.Add(new Squad { Troops = 50, Grade = 1 });

            Assert.Throws<System.InvalidOperationException>(() => TroopDeployment.Deploy(MakeLineSetup(), roster, MakeRules()));
        }

        [Test]
        public void ShippedDataFiles_LoadAndFitTheSampleBattle()
        {
            TroopRules rules = TroopDataLoader.ParseRules(ReadData("Troops", "troops_rules.json"));
            TroopRoster roster = TroopDataLoader.ParseRoster(ReadData("Troops", "troops_start_roster.json"), rules);
            BattleSetup setup = BattleDataLoader.ParseSetup(ReadData("Battle", "battle_sample_setup.json"));

            Assert.DoesNotThrow(() => TroopDeployment.Deploy(setup, roster, rules));
            Assert.AreEqual(4, rules.MaxGrade);
            Assert.AreEqual(100, roster.Gold);
            Assert.AreEqual(300, roster.TotalTroops);
        }

        [TestCase("{ \"max_troops_per_squad\": 0, \"reinforce_gold_per_troop\": 1 }")]
        [TestCase("{ \"max_troops_per_squad\": 150, \"reinforce_gold_per_troop\": -1 }")]
        [TestCase("{ \"max_troops_per_squad\": 150, \"reinforce_gold_per_troop\": 1, \"promote_gold_costs\": [60, -5] }")]
        public void ParseRules_InvalidData_Throws(string json)
        {
            Assert.Throws<InvalidDataException>(() => TroopDataLoader.ParseRules(json));
        }

        [TestCase("{ \"gold\": 100, \"squads\": [] }")]
        [TestCase("{ \"gold\": 100, \"squads\": [ { \"troops\": 151, \"grade\": 1 } ] }")]
        [TestCase("{ \"gold\": 100, \"squads\": [ { \"troops\": 100, \"grade\": 5 } ] }")]
        [TestCase("{ \"gold\": -1, \"squads\": [ { \"troops\": 100, \"grade\": 1 } ] }")]
        public void ParseRoster_InvalidData_Throws(string json)
        {
            Assert.Throws<InvalidDataException>(() => TroopDataLoader.ParseRoster(json, MakeRules()));
        }

        private static string ReadData(string folder, string fileName)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, "Data", folder, fileName));
        }
    }
}
