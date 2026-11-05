namespace KnightOrder.Troops
{
    /// <summary>
    /// One foot-soldier squad of the order. It has only a troop count and a grade (no unit types).
    /// Implements: Story 006.
    /// </summary>
    public sealed class Squad
    {
        /// <summary>Soldiers in the squad; 0 = the squad does not fight until it is reinforced.</summary>
        public int Troops { get; set; }

        /// <summary>Grade, starting at 1; each grade above 1 raises battle power and defense.</summary>
        public int Grade { get; set; } = 1;

        /// <summary>An independent copy.</summary>
        /// <returns>A new squad with the same values.</returns>
        public Squad Clone()
        {
            return new Squad { Troops = Troops, Grade = Grade };
        }
    }
}
