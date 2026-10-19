namespace DuelLinksAccess
{
    /// <summary>Tracks visible prompts, not the identity of their wording.</summary>
    public sealed class DuelConfirmationState
    {
        private string _announcedText = "";
        private float _blockedUntil;

        public bool Observe(bool visible, string text, float now, out bool announce)
        {
            announce = false;
            if (!visible || string.IsNullOrWhiteSpace(text))
            {
                Reset();
                return false;
            }
            if (now >= _blockedUntil && text != _announcedText)
            {
                _announcedText = text;
                announce = true;
            }
            return true;
        }

        public bool CanRespond(float now) => now >= _blockedUntil;

        public void Responded(float now)
        {
            // A callback may fail to close a prompt. Allow a deliberate retry,
            // including a new prompt with exactly the same tribute count.
            _blockedUntil = now + 0.5f;
            _announcedText = "";
        }

        public void Reset()
        {
            _announcedText = "";
            _blockedUntil = 0f;
        }
    }
}
