namespace DuelLinksAccess
{
    public sealed class DeckSaveState
    {
        private long _owner;
        private float _started;
        private bool _pending;
        public void Begin(long owner, float now, bool changed)
        { _owner = owner; _started = now; _pending = changed; }
        public bool Complete(long owner, bool changesRemain)
        {
            if (!_pending || owner != _owner || changesRemain) return false;
            _pending = false;
            return true;
        }
        public bool Expired(float now)
        {
            if (!_pending || now - _started < 30f) return false;
            _pending = false;
            return true;
        }
    }
}
