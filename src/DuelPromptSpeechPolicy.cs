using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace DuelLinksAccess
{
    // james54481, 2026-11-06: applies only to event-derived informational text,
    // never the native yes/no, effect-choice or card-picker handlers.
    public sealed class DuelPromptSpeechPolicy
    {
        private readonly Queue<(string Text, float At)> _recent = new();
        public static bool IsInformationalNoise(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (Regex.IsMatch(text.Trim(), @"^已[選选][擇择]\s*[「『]\s*[」』][。.!！]?$")) return true;
            // james54481, 2026-11-07: DialogMix emits automatic placement text
            // during either player's animations. This is not a native input prompt.
            if (Regex.IsMatch(text.Trim(), @"^[選选][擇择][「『].+[」』]的位置[。.!！]?$")) return true;
            // Preserve actionable questions even if field-check wording coexists.
            if (text.Contains("是否發動", StringComparison.Ordinal)
                || text.Contains("是否发动", StringComparison.Ordinal)
                || text.IndexOf("Would you like to activate", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return Regex.IsMatch(text.Trim(),
                @"(?:是否確認場地|是否确认场地|Check the field)\s*[?？]$", RegexOptions.IgnoreCase);
        }
        public bool TryAccept(string text, float now)
        {
            if (IsInformationalNoise(text)) return false;
            string normalized = Regex.Replace(text.Trim(), @"\s+", " ");
            while (_recent.Count > 0 && now - _recent.Peek().At >= 2f) _recent.Dequeue();
            foreach (var entry in _recent)
                if (entry.Text == normalized) return false;
            if (_recent.Count >= 32) _recent.Dequeue();
            _recent.Enqueue((normalized, now));
            return true;
        }
        public void Reset() => _recent.Clear();
    }
}
