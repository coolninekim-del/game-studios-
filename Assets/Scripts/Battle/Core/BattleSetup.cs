using System.Collections.Generic;

namespace KnightOrder.Battle
{
    /// <summary>
    /// The two knight orders placed on the grid at battle start.
    /// Implements: Story 001.
    /// </summary>
    public sealed class BattleSetup
    {
        /// <summary>Cavalry units of the player's order.</summary>
        public List<UnitSetup> PlayerUnits { get; set; } = new List<UnitSetup>();

        /// <summary>Cavalry units of the enemy order.</summary>
        public List<UnitSetup> EnemyUnits { get; set; } = new List<UnitSetup>();
    }
}
