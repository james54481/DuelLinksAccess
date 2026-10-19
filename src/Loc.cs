namespace DuelLinksAccess
{
    /// <summary>Mod-owned messages only; never translates game-provided text.</summary>
    public static class Loc
    {
        private static LocalizationCatalog _catalog = LocalizationCatalog.Load("en");
        public static string Locale => _catalog.Locale;
        public static void Initialize() { }
        public static void SetLocale(string locale)
            => System.Threading.Interlocked.Exchange(ref _catalog, LocalizationCatalog.Load(locale));
        public static string Get(string key) => _catalog.Get(key);
        public static string Get(string key, params object[] args) => _catalog.Get(key, args);
        public static string GetShortcutLabel(string key)
        {
            string resourceKey = "shortcut_" + key;
            return _catalog.ContainsKey(resourceKey) ? Get(resourceKey) : key;
        }
    }
}
