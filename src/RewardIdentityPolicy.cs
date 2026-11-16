using System.Globalization;
namespace DuelLinksAccess
{
    internal static class RewardIdentityPolicy
    {
        internal static int? LegacyId(string value)
            => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int id) && id > 0 ? id : null;
        // Verified by opening this exact reward icon: native title SR/UR精選票.
        // Do not infer names of other tickets from the shared Tc prefix.
        internal static string SpriteKey(string sprite)
            => sprite == "ItemTc0061" ? "reward_sr_ur_choice_ticket" : null;
    }
}
