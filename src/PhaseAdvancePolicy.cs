namespace DuelLinksAccess
{
    public enum PhaseAdvanceTarget { None, Battle, End }
    public static class PhaseAdvancePolicy
    {
        // Availability comes from the game's opened native menu, including turn-one rules.
        public static PhaseAdvanceTarget Choose(string currentPhase, bool battleAvailable, bool endAvailable)
        {
            if (currentPhase == "Main1" || currentPhase == "Main2")
                return battleAvailable ? PhaseAdvanceTarget.Battle : endAvailable ? PhaseAdvanceTarget.End : PhaseAdvanceTarget.None;
            if (currentPhase == "Battle")
                return endAvailable ? PhaseAdvanceTarget.End : PhaseAdvanceTarget.None;
            return PhaseAdvanceTarget.None;
        }
    }
}
