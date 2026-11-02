namespace DuelLinksAccess
{
    internal static class BannerDuplicatePolicy
    {
        internal static bool HideCover(bool nativeCover, bool hasUsableSwitch)
            => nativeCover && hasUsableSwitch;
        internal static string ArtKey(string[] sprites, string[] textures)
        {
            if (sprites == null || textures == null || sprites.Length + textures.Length == 0) return null;
            // Only this verified carousel wrapper is absent from list clones.
            // Preserve every content sprite, its order and every card texture.
            var content = System.Array.FindAll(sprites, s => s != "GUI_T_HomeBannerBg1(Clone)");
            return content.Length + textures.Length == 0 ? null
                : string.Join("|", content) + ";" + string.Join("|", textures);
        }

        internal static bool IsLoopCopy(bool loop, int count, int index, string[] signatures)
        {
            if (!loop || count <= 0 || signatures == null || signatures.Length != count + 2
                || index < count || index >= signatures.Length || string.IsNullOrEmpty(signatures[index])) return false;
            for (int i = 0; i < count; i++)
                if (signatures[i] == signatures[index]) return true;
            return false;
        }
    }
}
