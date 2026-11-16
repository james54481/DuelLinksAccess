using System;
using System.Collections.Generic;
namespace DuelLinksAccess
{
    public static class DeckRecipePolicy
    {
        public static string Summary(params string[] visibleFields)
        {
            var parts = new List<string>();
            foreach (var value in visibleFields)
            {
                string text = value?.Trim();
                if (!string.IsNullOrWhiteSpace(text) && !parts.Contains(text)) parts.Add(text);
            }
            return string.Join("，", parts);
        }
    }
}
