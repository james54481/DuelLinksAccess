namespace DuelLinksAccess
{
    // james54481, 2026-10-26: wait for result UI without retrying each frame.
    public sealed class ResultScanState
    {
        private float _nextAttempt;
        private int _failures;
        private string _lastError;
        public bool TryBegin(float now)
        {
            if (now < _nextAttempt) return false;
            _nextAttempt = now + 0.5f;
            return true;
        }
        public bool Failed(float now, string error)
        {
            _failures = System.Math.Min(_failures + 1, 5);
            _nextAttempt = now + _failures;
            bool changed = error != _lastError;
            _lastError = error;
            return changed;
        }
        public void Reset()
        {
            _nextAttempt = 0;
            _failures = 0;
            _lastError = null;
        }
    }
}
