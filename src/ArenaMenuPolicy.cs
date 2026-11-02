using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DuelLinksAccess
{
    internal static class ArenaMenuPolicy
    {
        public static bool PreferDeckEditor(string screen, string childName, bool activeButton,
            string bannerPath, int cardId) => screen == "PvpMainMenu" && childName == "card"
                && activeButton && cardId > 0 && IsBannerPath(bannerPath);

        public static int RewardCategory(string path)
        {
            if (path == null) return -1;
            var match = Regex.Match(path,
                @"^\$\.PvP\.bannerList\.[0-9]{1,9}\.data\.page\.reward\.category\.([01])\.list\.[0-9]{1,9}$");
            return match.Success ? match.Groups[1].Value[0] - '0' : -1;
        }

        public static bool IsRankedPage(string path) => path != null && Regex.IsMatch(path,
            @"^\$\.PvP\.bannerList\.[0-9]{1,9}\.data\.page$");

        public static bool IsDuelButton(string path) => path != null && path.EndsWith(".button.duel", StringComparison.Ordinal)
            && IsRankedPage(path.Substring(0, path.Length - ".button.duel".Length));

        public static string ReplayLockActionKey(IEnumerable<string> sprites)
        {
            string found = null;
            foreach (string sprite in sprites)
            {
                // Both states verified by locking and restoring the same replay.
                string key = sprite == "GUI_T_ComButton60_0" ? "arena_replay_lock"
                    : sprite == "GUI_T_ComButton61_0" ? "arena_replay_unlock" : null;
                if (key == null) continue;
                if (found != null && found != key) return null;
                found = key;
            }
            return found;
        }

        public static string SpriteTitleKey(IEnumerable<string> sprites)
        {
            string found = null;
            foreach (string sprite in sprites)
            {
                string key = sprite == "GUI_T_ModeSelect_PVPTitle12" ? "arena_tournament"
                    : sprite == "GUI_T_ModeSelect_PVPTitle11" ? "arena_kc_points" : null;
                if (key == null) continue;
                if (found != null && found != key) return null;
                found = key;
            }
            return found;
        }

        public static bool IsReplayRow(string path) => path != null && Regex.IsMatch(path,
            @"^\$\.PvP\.bannerList\.[0-9]{1,9}\.data\.replay\.tab\.[0-9]{1,9}\.list\.[0-9]{1,9}$");

        public static bool HasLockIcon(IEnumerable<string> sprites)
        {
            foreach (string sprite in sprites)
            {
                // Exact sprite of the replay control whose mouse and keyboard
                // activation both opened the native lock confirmation dialog.
                // This identifies the control, not its current protection state.
                if (sprite == "GUI_T_ComButton60_0") return true;
                if (sprite != null && Regex.IsMatch(sprite,
                    @"(?:^|_)(?:unlocked|unlock|locked|lock)(?:[0-9]*$|_)", RegexOptions.IgnoreCase)) return true;
            }
            return false;
        }

        public static string TitleKey(string description, int rule, string rank, bool characterDeck)
        {
            if (characterDeck) return "arena_character_deck";
            if (!string.IsNullOrWhiteSpace(rank))
                return rule == 1 ? "arena_rank_speed" : rule == 2 ? "arena_rank_rush" : null;
            // These exact native descriptions were matched to the visible
            // banner titles during the 2026-10-27 audit. Never use row order.
            return description?.Trim() switch
            {
                "一起決鬥吧！" => "arena_room",
                "和各地決鬥好手即時對戰！" => "arena_casual",
                "和好友相互切磋！" => "arena_friend",
                "重新觀看最喜愛的決鬥吧！" => "arena_replays",
                "你按區域劃分的對戰結果" => "arena_regional",
                _ => null
            };
        }

        // Server list positions are identities for this scan, never mode names.
        public static bool IsBannerPath(string path)
        {
            const string prefix = "$.PvP.bannerList.";
            if (path == null || !path.StartsWith(prefix, StringComparison.Ordinal)) return false;
            string suffix = path.Substring(prefix.Length);
            return suffix.Length > 0 && suffix.Length <= 9
                && Array.TrueForAll(suffix.ToCharArray(), c => c >= '0' && c <= '9');
        }

        public static float ScrollCorrection(float bottom, float top, float viewBottom,
            float viewTop, float travel)
        {
            if (travel <= 0 || viewTop <= viewBottom) return 0;
            if (top - bottom > viewTop - viewBottom) return (top - viewTop) / travel;
            if (bottom < viewBottom) return (bottom - viewBottom) / travel;
            if (top > viewTop) return (top - viewTop) / travel;
            return 0;
        }
    }
}
