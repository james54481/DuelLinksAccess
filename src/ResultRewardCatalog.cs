using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace DuelLinksAccess
{
    // james54481, 2026-10-26. Deduplicate template nodes by reward row,
    // never by item ID: identical items in three boxes are three rewards.
    public sealed class ResultRewardCatalog
    {
        private static readonly Regex PathPattern = new(@"^DuelResult\.data\.list\.[0-9]+\.reward\.list\.[0-9]+$");
        private readonly Dictionary<string, (int Id, int Count, string Label)> _rows = new();
        private readonly HashSet<string> _conflicts = new();
        public IEnumerable<string> Labels
        {
            get { foreach (var row in _rows.Values) yield return row.Label; }
        }
        public bool Contains(string path) => path != null && _rows.ContainsKey(path);
        public void Add(string path, string type, int id, int count, Func<int, string> getName, string unknown)
        {
            if (path == null || !PathPattern.IsMatch(path) || type != "item"
                || id <= 0 || count < 0 || _conflicts.Contains(path)) return;
            if (_rows.TryGetValue(path, out var old))
            {
                if (old.Id != id || old.Count != count) { _rows.Remove(path); _conflicts.Add(path); }
                return;
            }
            string name = null;
            try { name = getName(id); } catch { }
            if (string.IsNullOrWhiteSpace(name)) name = unknown;
            _rows.Add(path, (id, count, $"{name} ×{count}"));
        }
    }
}
