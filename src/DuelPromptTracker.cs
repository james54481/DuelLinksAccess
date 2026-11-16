namespace DuelLinksAccess
{
    // james54481, 2026-11-06: wait for concrete, stable text; never invent a prompt.
    public sealed class DuelPromptTracker
    {
        public bool Pending { get; private set; }
        private float _deadline, _notBefore, _stableAt;
        private string _candidate;
        public void Request(float now)
        {
            if (Pending) return; // Event bursts must not prolong a stale request.
            Pending = true;
            _deadline = now + 1f;
            _notBefore = now + 0.15f;
            _candidate = null;
        }
        public string Observe(float now, string text, bool dedicatedPrompt)
        {
            if (!Pending) return null;
            if (dedicatedPrompt || now > _deadline) { Reset(); return null; }
            if (now < _notBefore) return null;
            if (string.IsNullOrWhiteSpace(text)) { _candidate = null; return null; }
            if (text != _candidate) { _candidate = text; _stableAt = now; return null; }
            if (now - _stableAt < 0.1f) return null;
            Reset();
            return text;
        }
        public void Reset() { Pending = false; _candidate = null; }
    }
    public sealed class OpponentWaitTracker
    {
        private float _since = -1f;
        private bool _announced;
        public bool Observe(float now, bool activeTimer, bool myInput, bool opponentInput, bool modal)
        {
            if (!activeTimer || myInput || !opponentInput || modal) { Reset(); return false; }
            if (_since < 0f) _since = now;
            if (_announced || now - _since < 0.75f) return false;
            _announced = true;
            return true;
        }
        public void Reset() { _since = -1f; _announced = false; }
    }
}
