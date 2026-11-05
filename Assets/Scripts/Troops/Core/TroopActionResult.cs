namespace KnightOrder.Troops
{
    /// <summary>
    /// Outcome of a reinforce or promote request. Anything but Done leaves the roster unchanged.
    /// Implements: Story 006.
    /// </summary>
    public enum TroopActionResult
    {
        /// <summary>Applied: troops or grade raised and gold spent.</summary>
        Done,

        /// <summary>The order has less gold than the cost.</summary>
        NotEnoughGold,

        /// <summary>The reinforcement would take the squad above its maximum troops.</summary>
        SquadFull,

        /// <summary>The squad is already at the highest grade.</summary>
        MaxGrade,

        /// <summary>The squad index or the troop count is out of range.</summary>
        InvalidRequest
    }
}
