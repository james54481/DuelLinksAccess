namespace DuelLinksAccess
{
    /// <summary>Han characters are shared by Chinese and Japanese, not a language signal.</summary>
    public static class LabelLanguagePolicy
    {
        public static bool ContainsJapanese(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
            {
                if ((c >= '\u3040' && c <= '\u309f')
                    || (c >= '\u30a0' && c <= '\u30ff')
                    || (c >= '\u31f0' && c <= '\u31ff')
                    || (c >= '\uff66' && c <= '\uff9f'))
                    return true;
            }
            return false;
        }
    }
}
