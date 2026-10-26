using System.Collections.Generic;

namespace DuelLinksAccess
{
    // james54481, 2026-10-25: session-scoped evidence, never ordinal matching.
    public sealed class ObservedCardIdentityStore
    {
        private readonly Dictionary<int, int> _known = new();

        public static int MatchedUid(int itemUid, int visualUid, int mixedId)
            => visualUid > 0 && visualUid == (itemUid > 0 ? itemUid : mixedId) ? visualUid : 0;

        public void Observe(int uid, int cardId, bool visible)
        {
            if (!visible || uid <= 0 || cardId <= 0 || cardId >= 100000) return;
            if (_known.TryGetValue(uid, out int previous) && previous != cardId)
                _known[uid] = 0; // conflicting identity: unknown until next duel
            else if (!_known.ContainsKey(uid))
                _known[uid] = cardId;
        }

        public int Resolve(int uid, bool localActivationList, bool hidden)
            => localActivationList && !hidden && uid > 0 && _known.TryGetValue(uid, out int id) ? id : 0;

        public void Reset() => _known.Clear();
    }
}
