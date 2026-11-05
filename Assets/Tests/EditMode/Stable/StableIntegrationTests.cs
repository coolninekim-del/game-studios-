using System.Collections.Generic;
using System.IO;
using KnightOrder.Battle;
using KnightOrder.Battle.Loading;
using KnightOrder.Battle.Timing;
using KnightOrder.Duel;
using KnightOrder.Duel.Loading;
using KnightOrder.Stable;
using KnightOrder.Stable.Loading;
using NUnit.Framework;
using UnityEngine;

namespace KnightOrder.Tests.Stable
{
    /// <summary>
    /// Story 005: stable training with a limited number of actions, and the trained profile reaching the next
    /// battle (mounted protagonist's charge power and charge window) and the next duel (lance power, health and
    /// window). Fixtures come from the factory functions below; only the data-file test reads the shipped
    /// stable, battle and duel files, on purpose, to keep them valid together.
    /// </summary>
    public class StableIntegrationTests
    {
        private const float FRAME_SECONDS = 1f / 60f;

        private static StableRules MakeStableRules()
        {
            return new StableRules
            {
                ActionsPerVisit = 3,
                Trainings = new List<TrainingOption>
                {
                    new TrainingOption { Stat = KnightStat.HorseSpeed, Gain = 5 },
                    new TrainingOption { Stat = KnightStat.HorseAgility, Gain = 5 },
                    new TrainingOption { Stat = KnightStat.HorseHp, Gain = 15 },
                    new TrainingOption { Stat = KnightStat.RiderAttack, Gain = 3 },
                    new TrainingOption { Stat = KnightStat.RiderHp, Gain = 15 }
                },
                BattleSpeedPowerScale = 0.02f,
                BattleAgilityWindowScale = 0.02f,
                BattleDefense = 10f,
                BattleStartX = 0,
                BattleStartY = 0
            };
        }

        private static KnightProfile MakeProfile()
        {
            return new KnightProfile { RiderAttack = 30, RiderHp = 100, HorseSpeed = 20, HorseHp = 40, HorseAgility = 10 };
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

        private static DuelRules MakeDuelRules()
        {
            return new DuelRules
            {
                ApproachSeconds = 2.4f,
                GuardRevealSeconds = 1.4f,
                WindowSeconds = 0.3f,
                TurnaroundSeconds = 1.0f,
                SpeedPowerScale = 0.02f,
                AgilityWindowScale = 0.02f,
                SuccessMultiplier = 1.0f,
                EarlyMultiplier = 0.4f,
                GuardedMultiplier = 0.25f,
                MaxPasses = 12
            };
        }

        private static DuelSetup MakeDuelSetup(KnightProfile hero)
        {
            return new DuelSetup
            {
                Hero = KnightConversion.ToDuelist(hero),
                Boss = new DuelistStats { RiderAttack = 25, RiderHp = 120, HorseSpeed = 20, HorseHp = 40 },
                BossGuardPattern = new List<LanceAim> { LanceAim.High },
                VictoryGrowthReward = 3
            };
        }

        /// <summary>The mounted protagonist alone on row 0 against one enemy foot unit.</summary>
        private static BattleSetup MakeHeroBattle(KnightProfile hero)
        {
            var setup = new BattleSetup
            {
                EnemyUnits = new List<UnitSetup>
                {
                    new UnitSetup { Troops = 100, ChargePower = 26f, Defense = 12f, StartX = 8, StartY = 0 }
                }
            };
            KnightConversion.AddToBattle(setup, hero, MakeStableRules());
            return setup;
        }

        /// <summary>Troops the enemy loses on the protagonist's first impact.</summary>
        private static int FirstImpactDamage(BattleSetup setup)
        {
            var sim = new BattleSimulation(MakeBattleRules(), setup);
            BattleUnit hero = sim.Units[0];
            BattleUnit enemy = sim.Units[1];
            while (hero.ChargesDone == 0 && !sim.IsFinished)
            {
                sim.Step();
            }

            return 100 - enemy.Troops;
        }

        private static KnightProfile Trained(KnightStat stat)
        {
            var visit = new StableSession(MakeStableRules(), MakeProfile());
            Assert.That(visit.Train(stat), Is.Not.Null);
            return visit.Profile;
        }

        [Test]
        public void Train_ActionLimitReached_FurtherTrainingRefusedAndProfileUnchanged()
        {
            var visit = new StableSession(MakeStableRules(), MakeProfile());

            Assert.That(visit.Train(KnightStat.HorseSpeed), Is.Not.Null);
            Assert.That(visit.Train(KnightStat.HorseSpeed), Is.Not.Null);
            Assert.That(visit.Train(KnightStat.HorseAgility), Is.Not.Null);
            Assert.That(visit.ActionsLeft, Is.EqualTo(0));

            Assert.That(visit.Train(KnightStat.RiderAttack), Is.Null);
            Assert.That(visit.Profile.HorseSpeed, Is.EqualTo(30));
            Assert.That(visit.Profile.HorseAgility, Is.EqualTo(15));
            Assert.That(visit.Profile.RiderAttack, Is.EqualTo(30));
        }

        [Test]
        public void Train_RaisesStatByGain_ResultHoldsBeforeAndAfter()
        {
            KnightProfile start = MakeProfile();
            var visit = new StableSession(MakeStableRules(), start);

            TrainingResult result = visit.Train(KnightStat.HorseHp);

            Assert.That(result.Stat, Is.EqualTo(KnightStat.HorseHp));
            Assert.That(result.StatBefore, Is.EqualTo(40));
            Assert.That(result.StatAfter, Is.EqualTo(55));
            Assert.That(result.Before.HorseHp, Is.EqualTo(40));
            Assert.That(visit.Profile.HorseHp, Is.EqualTo(55));
            Assert.That(visit.ActionsLeft, Is.EqualTo(2));
            Assert.That(start.HorseHp, Is.EqualTo(40), "the caller's profile is copied, not trained");
        }

        [Test]
        public void Train_StatWithoutTraining_RefusedWithoutSpendingAnAction()
        {
            StableRules rules = MakeStableRules();
            rules.Trainings.RemoveAll(t => t.Stat == KnightStat.RiderHp);
            var visit = new StableSession(rules, MakeProfile());

            Assert.That(visit.Train(KnightStat.RiderHp), Is.Null);
            Assert.That(visit.ActionsLeft, Is.EqualTo(3));
        }

        [Test]
        public void HorseSpeedTraining_NextBattle_MountedChargeHitsHarder()
        {
            int before = FirstImpactDamage(MakeHeroBattle(MakeProfile()));
            int after = FirstImpactDamage(MakeHeroBattle(Trained(KnightStat.HorseSpeed)));

            // round(30 * (1 + 20 * 0.02)) = 42 and round(30 * (1 + 25 * 0.02)) = 45, minus defense 12 * 0.5.
            Assert.That(before, Is.EqualTo(36));
            Assert.That(after, Is.EqualTo(39));
        }

        [Test]
        public void MountedUnit_FewTroopsLeft_ImpactDoesNotScaleWithTroops()
        {
            BattleSetup mounted = MakeHeroBattle(MakeProfile());
            mounted.PlayerUnits[0].Troops = 25;
            BattleSetup foot = MakeHeroBattle(MakeProfile());
            foot.PlayerUnits[0].Troops = 25;
            foot.PlayerUnits[0].IsMounted = false;

            // Mounted: 42 - 6 = 36. Foot: 42 * (25 / 100) ^ 0.5 - 6 = 15.
            Assert.That(FirstImpactDamage(mounted), Is.EqualTo(36));
            Assert.That(FirstImpactDamage(foot), Is.EqualTo(15));
        }

        [Test]
        public void HorseAgilityTraining_NextBattle_OnlyProtagonistChargeWindowWidens()
        {
            KnightProfile trained = Trained(KnightStat.HorseAgility);
            var setup = new BattleSetup();
            setup.PlayerUnits.Add(new UnitSetup { Troops = 100, ChargePower = 32f, Defense = 10f, StartX = 0, StartY = 2 });
            setup.EnemyUnits.Add(new UnitSetup { Troops = 100, ChargePower = 26f, Defense = 12f, StartX = 8, StartY = 0 });
            setup.EnemyUnits.Add(new UnitSetup { Troops = 100, ChargePower = 26f, Defense = 12f, StartX = 8, StartY = 2 });
            KnightConversion.AddToBattle(setup, trained, MakeStableRules());
            var controller = new ChargeTimingController(new BattleSimulation(MakeBattleRules(), setup), MakeTimingRules(),
                new HorseWindowModifier(trained, MakeStableRules()));

            float? heroWindow = null;
            float? footWindow = null;
            for (int frame = 0; frame < 3000 && (heroWindow == null || footWindow == null); frame++)
            {
                controller.Advance(FRAME_SECONDS);
                foreach (PendingImpact impact in controller.Pending)
                {
                    if (impact.Kind != TimingCueKind.Charge)
                    {
                        continue;
                    }

                    if (controller.Simulation.Units[impact.AttackerIndex].IsMounted)
                    {
                        heroWindow = impact.WindowSeconds;
                    }
                    else
                    {
                        footWindow = impact.WindowSeconds;
                    }
                }
            }

            // Agility 10 + 5 = 15: 0.2 * (1 + 15 * 0.02) = 0.26; foot soldiers keep 0.2.
            Assert.That(heroWindow, Is.EqualTo(0.26f).Within(1e-5f));
            Assert.That(footWindow, Is.EqualTo(0.2f).Within(1e-5f));
        }

        [Test]
        public void Training_NextDuel_LancePowerHealthAndWindowGrow()
        {
            var visit = new StableSession(MakeStableRules(), MakeProfile());
            visit.Train(KnightStat.HorseSpeed);
            visit.Train(KnightStat.HorseHp);
            visit.Train(KnightStat.HorseAgility);

            var before = new DuelSimulation(MakeDuelRules(), MakeDuelSetup(MakeProfile()));
            var after = new DuelSimulation(MakeDuelRules(), MakeDuelSetup(visit.Profile));

            Assert.That(before.HeroPower, Is.EqualTo(42));
            Assert.That(after.HeroPower, Is.EqualTo(45));
            Assert.That(before.HeroMaxHp, Is.EqualTo(140));
            Assert.That(after.HeroMaxHp, Is.EqualTo(155));
            Assert.That(before.HeroWindowSeconds, Is.EqualTo(0.36f).Within(1e-5f));
            Assert.That(after.HeroWindowSeconds, Is.EqualTo(0.39f).Within(1e-5f));
        }

        [Test]
        public void Numbers_MatchWhatTheBattleAndDuelUse()
        {
            KnightProfile profile = MakeProfile();
            KnightNumbers numbers = KnightConversion.Numbers(profile, MakeStableRules(), MakeTimingRules(), MakeDuelRules());
            var duel = new DuelSimulation(MakeDuelRules(), MakeDuelSetup(profile));
            UnitSetup unit = KnightConversion.ToBattleUnit(profile, MakeStableRules());

            Assert.That(numbers.BattleChargePower, Is.EqualTo((int)unit.ChargePower).And.EqualTo(42));
            Assert.That(numbers.BattleHealth, Is.EqualTo(unit.Troops).And.EqualTo(140));
            Assert.That(numbers.BattleWindowSeconds, Is.EqualTo(0.24f).Within(1e-5f));
            Assert.That(numbers.DuelLancePower, Is.EqualTo(duel.HeroPower));
            Assert.That(numbers.DuelHealth, Is.EqualTo(duel.HeroMaxHp));
            Assert.That(numbers.DuelWindowSeconds, Is.EqualTo(duel.HeroWindowSeconds));
        }

        [Test]
        public void AddToBattle_StartCellTaken_Throws()
        {
            var setup = new BattleSetup();
            setup.PlayerUnits.Add(new UnitSetup { Troops = 100, ChargePower = 32f, Defense = 10f, StartX = 0, StartY = 0 });

            Assert.Throws<System.InvalidOperationException>(() => KnightConversion.AddToBattle(setup, MakeProfile(), MakeStableRules()));
        }

        [Test]
        public void ShippedDataFiles_LoadTogether_StartNumbersAsDocumented()
        {
            StableRules rules = StableDataLoader.ParseRules(ReadData("Stable", "stable_rules.json"));
            KnightProfile start = StableDataLoader.ParseProfile(ReadData("Stable", "stable_start_profile.json"));
            BattleSetup battle = BattleDataLoader.ParseSetup(ReadData("Battle", "battle_sample_setup.json"));
            ChargeTimingRules timing = ChargeTimingDataLoader.ParseRules(ReadData("Battle", "battle_charge_timing.json"));
            DuelRules duel = DuelDataLoader.ParseRules(ReadData("Duel", "duel_rules.json"));

            Assert.DoesNotThrow(() => KnightConversion.AddToBattle(battle, start, rules));
            KnightNumbers numbers = KnightConversion.Numbers(start, rules, timing, duel);
            Assert.That(rules.ActionsPerVisit, Is.EqualTo(3));
            Assert.That(numbers.BattleChargePower, Is.EqualTo(42));
            Assert.That(numbers.BattleHealth, Is.EqualTo(140));
            Assert.That(numbers.BattleWindowSeconds, Is.EqualTo(0.264f).Within(1e-5f));
            Assert.That(numbers.DuelLancePower, Is.EqualTo(42));
            Assert.That(numbers.DuelWindowSeconds, Is.EqualTo(0.36f).Within(1e-5f));
        }

        [TestCase("{ \"actions_per_visit\": 0, \"trainings\": [ { \"stat\": \"horse_speed\", \"gain\": 5 } ] }")]
        [TestCase("{ \"actions_per_visit\": 3, \"trainings\": [ { \"stat\": \"horse_wings\", \"gain\": 5 } ] }")]
        [TestCase("{ \"actions_per_visit\": 3, \"trainings\": [ { \"stat\": \"horse_speed\", \"gain\": 5 }, { \"stat\": \"horse_speed\", \"gain\": 2 } ] }")]
        [TestCase("{ \"actions_per_visit\": 3, \"trainings\": [ { \"stat\": \"horse_speed\", \"gain\": 0 } ] }")]
        public void ParseRules_InvalidData_Throws(string json)
        {
            Assert.Throws<InvalidDataException>(() => StableDataLoader.ParseRules(json));
        }

        [Test]
        public void ParseProfile_NoHealth_Throws()
        {
            Assert.Throws<InvalidDataException>(() => StableDataLoader.ParseProfile(
                "{ \"rider_attack\": 30, \"rider_hp\": 0, \"horse_speed\": 20, \"horse_hp\": 0, \"horse_agility\": 10 }"));
        }

        private static string ReadData(string folder, string fileName)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, "Data", folder, fileName));
        }
    }
}
