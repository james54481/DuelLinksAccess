using System.Linq;
namespace DuelLinksAccess
{
    internal static class GateRewardPolicy
    {
        internal static bool Valid(int[] ids) => ids != null && ids.Length > 0
            && ids.Length <= 1000 && ids.All(id => id > 0);
        internal static int Move(int index, int delta, int count)
            => count <= 0 ? 0 : ((index + delta) % count + count) % count;
        internal static bool Same(int[] first, int[] second)
            => first != null && second != null && first.SequenceEqual(second);
    }
}
