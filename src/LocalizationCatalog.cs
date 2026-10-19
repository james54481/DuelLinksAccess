using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace DuelLinksAccess
{
    /// <summary>Embedded UTF-8 catalogs with per-key English fallback.</summary>
    public sealed class LocalizationCatalog
    {
        private readonly Dictionary<string, string> _english;
        private readonly Dictionary<string, string> _translations;
        private readonly CultureInfo _culture;
        public string Locale { get; }

        internal LocalizationCatalog(string locale, Dictionary<string, string> english,
            Dictionary<string, string> translations)
        {
            Locale = locale;
            _english = english;
            _translations = translations;
            // MelonLoader can run with globalization-invariant mode enabled.
            // Catalog selection needs no OS culture; use stable formatting for
            // game numbers without constructing an unsupported named culture.
            _culture = CultureInfo.InvariantCulture;
        }

        public static LocalizationCatalog Load(string locale)
        {
            // Explicit supported locales: no arbitrary input in resource paths.
            string selected = string.Equals(locale, "zh-TW", StringComparison.OrdinalIgnoreCase)
                ? "zh-TW" : "en";
            var english = ReadCatalog("en");
            var translated = selected == "en" ? english : ReadCatalog(selected);
            return new LocalizationCatalog(selected, english, translated);
        }

        public static Dictionary<string, string> ReadCatalog(string locale)
        {
            string name = "DuelLinksAccess.Locales." + locale + ".json";
            using Stream stream = typeof(LocalizationCatalog).Assembly.GetManifestResourceStream(name);
            if (stream == null) throw new InvalidOperationException("Missing embedded catalog: " + name);
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            using var json = JsonDocument.Parse(reader.ReadToEnd());
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in json.RootElement.EnumerateObject())
            {
                if (entry.Value.ValueKind != JsonValueKind.String)
                    throw new InvalidOperationException("Non-string translation: " + entry.Name);
                if (!result.TryAdd(entry.Name, entry.Value.GetString()))
                    throw new InvalidOperationException("Duplicate translation key: " + entry.Name);
            }
            return result;
        }

        public bool ContainsKey(string key) => key != null && _english.ContainsKey(key);
        public string Get(string key)
        {
            if (key == null) return "";
            if (_translations.TryGetValue(key, out var value)) return value;
            return _english.TryGetValue(key, out value) ? value : key;
        }
        public string Get(string key, params object[] args)
        {
            string template = Get(key);
            try { return string.Format(_culture, template, args); }
            catch (FormatException)
            {
                if (_english.TryGetValue(key, out var english))
                {
                    try { return string.Format(CultureInfo.InvariantCulture, english, args); }
                    catch (FormatException) { return english; }
                }
                return template;
            }
        }
    }
}
