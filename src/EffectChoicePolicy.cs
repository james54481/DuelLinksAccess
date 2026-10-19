namespace DuelLinksAccess
{
    internal static class EffectChoicePolicy
    {
        public static bool DescriptionMatches(string announced, string live)
            => !string.IsNullOrWhiteSpace(announced)
                && string.Equals(announced, live, System.StringComparison.Ordinal);

        public static int Move(int count, int current, int direction)
            => count <= 0 || current < 0 || current >= count ? -1
                : (current + direction + count) % count;

        public static bool CanConfirm(int count, int requested, int actual,
            bool hasDescription, bool enabled, bool pending, bool viewing, bool verified)
            => count > 0 && requested >= 0 && requested < count
                && actual == requested && hasDescription && enabled && !pending && !viewing && verified;
    }
}
