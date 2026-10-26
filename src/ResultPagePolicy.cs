using System;
using System.Collections.Generic;
namespace DuelLinksAccess
{
    // james54481, 2026-10-26. Result pages must retain numeric values.
    public static class ResultPagePolicy
    {
        public static bool IsAdvance(string value)
        {
            switch (value?.Trim().ToUpperInvariant())
            {
                case "NEXT": case "OK": case "END": case "WATCH AGAIN": case "RATE":
                case "下一個": case "下一步": case "下一個頁面": case "確定": case "結束":
                case "下一个": case "确定": case "结束": return true;
                default: return false;
            }
        }
        public static string Compose(IEnumerable<string> values)
        {
            var parts = new List<string>();
            bool futureRewards = false;
            foreach (var value in values)
            {
                string text = value?.Trim();
                if (string.IsNullOrEmpty(text)) continue;
                if (IsAdvance(text)) { futureRewards = false; continue; }
                if (text == "角色升級獎勵" || text == "角色升级奖励"
                    || text == "Character Level-Up Rewards") { futureRewards = true; continue; }
                if (!futureRewards) parts.Add(text);
            }
            return string.Join(". ", parts);
        }
    }
    public sealed class ResultPageAnnouncement
    {
        private string _pending, _spoken;
        private float _since;
        public bool Observe(string text, float now)
        {
            if (text != _pending) { _pending = text; _since = now; return false; }
            if (string.IsNullOrEmpty(text) || text == _spoken || now - _since < 0.8f) return false;
            _spoken = text;
            return true;
        }
        public void Reset() { _pending = _spoken = null; _since = 0; }
    }
}
