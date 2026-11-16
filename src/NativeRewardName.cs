using Il2CppYgomSystem.Utility;
using Il2CppYgomGame.Utility;
namespace DuelLinksAccess
{
    internal static class NativeRewardName
    {
        internal static string FromPath(string path)
        {
            try
            {
                var raw = ClientWork.getObjectByJsonPath(path, null);
                if (!ReferenceEquals(raw, null))
                {
                    var iid = new ItemUtil.ItemID(raw);
                    if (!iid.IsError())
                    {
                        string name = LabelExtractor.StripRichText(ItemUtil.GetName(iid));
                        if (!string.IsNullOrWhiteSpace(name)) return name;
                    }
                }
            }
            catch (System.Exception ex)
            { DebugLogger.Log(LogCategory.Handler, "RewardIdentity", $"Native ID resolution failed at {path}: {ex.Message}"); }
            int? legacy = RewardIdentityPolicy.LegacyId(ClientWork.getStringByJsonPath(path, ""));
            if (!legacy.HasValue) return null;
            try { return LabelExtractor.StripRichText(ItemUtil.GetName(legacy.Value)); }
            catch { return null; }
        }
    }
}
