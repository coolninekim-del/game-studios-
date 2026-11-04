using System.Collections.Generic;
using System.IO;
using KnightOrder.Battle;
using KnightOrder.Battle.Loading;
using NUnit.Framework;

namespace KnightOrder.Tests.Battle
{
    /// <summary>
    /// Auto-battle movement on tiles (Story 001 rules, one unit per tile): units never share a cell, two
    /// units closing on each other collide on adjacent cells, and among equally near enemies a unit targets
    /// the one straight ahead in its row. Fixtures come from the factory functions below.
    /// </summary>
    public class AutoBattleTests
    {
        private static BattleRules MakeRules()
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
                ArrowRange = 4,
                ArrowDamage = 12,
                ArrowIntervalTicks = 6
            };
        }

        private static UnitSetup MakeUnit(int x, int y)
        {
            return new UnitSetup { Troops = 100, ChargePower = 30f, Defense = 10f, StartX = x, StartY = y };
        }

        /// <summary>One unit per side on row 0, an even distance apart (both would step into the middle cell).</summary>
        private static BattleSetup MakeEvenDuelSetup()
        {
            return new BattleSetup
            {
                PlayerUnits = new List<UnitSetup> { MakeUnit(0, 0) },
                EnemyUnits = new List<UnitSetup> { MakeUnit(8, 0) }
            };
        }

        /// <summary>Three lanes, every unit at the same Chebyshev distance from every enemy (the sample layout).</summary>
        private static BattleSetup MakeLineSetup()
        {
            var setup = new BattleSetup();
            for (int row = 1; row <= 5; row += 2)
            {
                setup.PlayerUnits.Add(MakeUnit(0, row));
                setup.EnemyUnits.Add(MakeUnit(12, row));
            }

            return setup;
        }

        /// <summary>Uneven sides packed next to each other, so units queue behind allies and swap targets.</summary>
        private static BattleSetup MakeCrowdedSetup()
        {
            return new BattleSetup
            {
                PlayerUnits = new List<UnitSetup> { MakeUnit(0, 0), MakeUnit(0, 1), MakeUnit(-1, 0), MakeUnit(-1, 1) },
                EnemyUnits = new List<UnitSetup> { MakeUnit(8, 0), MakeUnit(8, 1), MakeUnit(9, 0) }
            };
        }

        private static void AssertNoSharedCell(BattleSimulation simulation)
        {
            var cells = new HashSet<(int, int)>();
            foreach (BattleUnit unit in simulation.Units)
            {
                if (unit.IsAlive)
                {
                    Assert.IsTrue(cells.Add((unit.X, unit.Y)), "two living units on (" + unit.X + ", " + unit.Y + ") at tick " + simulation.Tick);
                }
            }
        }

        [Test]
        public void EvenDistanceDuel_FirstImpact_UnitsOnAdjacentCells()
        {
            // Arrange
            var simulation = new BattleSimulation(MakeRules(), MakeEvenDuelSetup());
            int impactDistance = -1;
            simulation.OnChargeImpact += (s, e) =>
            {
                if (impactDistance < 0)
                {
                    impactDistance = System.Math.Max(System.Math.Abs(e.Attacker.X - e.Defender.X), System.Math.Abs(e.Attacker.Y - e.Defender.Y));
                }
            };

            // Act
            while (impactDistance < 0 && !simulation.IsFinished)
            {
                simulation.Step();
            }

            // Assert
            Assert.AreEqual(1, impactDistance);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void WholeBattle_NoTwoLivingUnitsShareACell(int layout)
        {
            // Arrange
            BattleSetup setup = layout == 0 ? MakeEvenDuelSetup() : layout == 1 ? MakeLineSetup() : MakeCrowdedSetup();
            var simulation = new BattleSimulation(MakeRules(), setup);

            // Act + Assert (every tick)
            while (!simulation.IsFinished)
            {
                simulation.Step();
                AssertNoSharedCell(simulation);
            }

            Assert.IsFalse(simulation.GetResult().ReachedTickCap, "units must not block each other forever");
        }

        [Test]
        public void EquallyNearEnemies_EachLaneChargesTheEnemyInItsRow()
        {
            // Arrange
            var simulation = new BattleSimulation(MakeRules(), MakeLineSetup());
            var firstDefenderRow = new Dictionary<BattleUnit, int>();
            var startRow = new Dictionary<BattleUnit, int>();
            foreach (BattleUnit unit in simulation.Units)
            {
                startRow[unit] = unit.Y;
            }

            simulation.OnChargeImpact += (s, e) =>
            {
                if (e.Attacker.Side == BattleSide.Player && !firstDefenderRow.ContainsKey(e.Attacker))
                {
                    firstDefenderRow[e.Attacker] = startRow[e.Defender];
                }
            };

            // Act
            while (firstDefenderRow.Count < 3 && !simulation.IsFinished)
            {
                simulation.Step();
            }

            // Assert
            Assert.AreEqual(3, firstDefenderRow.Count);
            foreach (KeyValuePair<BattleUnit, int> pair in firstDefenderRow)
            {
                Assert.AreEqual(startRow[pair.Key], pair.Value);
            }
        }

        [Test]
        public void ParseSetup_TwoUnitsOnOneCell_Throws()
        {
            const string json = "{\"player_units\":[{\"troops\":100,\"charge_power\":30,\"defense\":10,\"start_x\":0,\"start_y\":0}],"
                + "\"enemy_units\":[{\"troops\":100,\"charge_power\":30,\"defense\":10,\"start_x\":0,\"start_y\":0}]}";

            Assert.Throws<InvalidDataException>(() => BattleDataLoader.ParseSetup(json));
        }
    }
}
