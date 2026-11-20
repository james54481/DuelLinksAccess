using System;
using System.Collections.Generic;

namespace DuelLinksAccess
{
    public static class EventDetailText
    {
        public static bool IsTabLabel(string text) => text == "遊戲玩法" || text == "推薦獎勵";
        public static IEnumerable<string> Paragraphs(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string paragraph = line.Trim();
                if (paragraph.Length > 0) yield return paragraph;
            }
        }
    }
}
