namespace DuelLinksAccess
{
    // Once offered, retain the choice even if native availability flickers.
    public sealed class DrawSkillChoice
    {
        public bool Offered { get; private set; }
        public bool Submitted { get; private set; }
        public bool NormalChosen { get; private set; }
        public bool BlockDraw { get; private set; }

        public void Observe(bool eligibleDraw, bool? available)
        {
            if (!eligibleDraw) { Reset(); return; }
            if (available == true && !NormalChosen && !Submitted) Offered = true;
            BlockDraw = !NormalChosen && (Offered || Submitted || available == null);
        }

        public void ChooseNormal()
        { NormalChosen = true; Submitted = false; BlockDraw = false; }
        public void SkillSubmitted()
        { Submitted = true; BlockDraw = true; }
        public void Reopen() { Submitted = false; }
        public void Reset()
        { Offered = Submitted = NormalChosen = BlockDraw = false; }
    }
}
