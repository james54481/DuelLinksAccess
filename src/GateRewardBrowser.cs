using System;
using System.Linq;
using UnityEngine;
using Il2CppYgomSystem.UI;
using Il2CppYgomGame.CardList;

namespace DuelLinksAccess
{
    internal static class GateRewardBrowser
    {
        private sealed class Snapshot
        {
            internal GameObject View;
            internal int[] Cards;
            internal string Title;
        }
        private static Snapshot _snapshot;
        private static GameObject _gate;
        private static int _index;

        private static Snapshot Read(GameObject gate)
        {
            if (gate == null || gate.name != "Gate" || !gate.activeInHierarchy) return null;
            try
            {
                var managers = ViewControllerManager.namedManager;
                if (managers == null || !managers.TryGetValue("content", out var content)
                    || content?.GetStackTopViewController()?.gameObject != gate
                    || !managers.TryGetValue("subcontent", out var sub) || sub == null) return null;
                var top = sub.GetStackTopViewController();
                if (top?.gameObject == null || top.gameObject.name != "NpcReward_PC"
                    || !top.gameObject.activeInHierarchy) return null;
                var vc = new NpcRewardViewController(top.Pointer);
                if (!vc.setupDone || vc.cardList == null) return null;
                int[] ids = vc.cardList.ToArray();
                if (!GateRewardPolicy.Valid(ids)) return null;
                return new Snapshot { View = top.gameObject, Cards = ids,
                    Title = LabelExtractor.StripRichText(vc.title) ?? "" };
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "GateRewards", "read: " + ex.Message);
                return null;
            }
        }

        internal static string EntryLabel(GameObject gate)
        {
            var data = Read(gate);
            return data == null ? null : Loc.Get("gate_rewards_entry", data.Cards.Length);
        }

        internal static void Begin(GameObject gate)
        {
            var data = Read(gate);
            if (data == null) { ScreenReader.Say(Loc.Get("gate_rewards_unavailable")); return; }
            _snapshot = data; _gate = gate; _index = 0;
            DebugLogger.Log(LogCategory.Handler, "GateRewards",
                $"browse title={data.Title} count={data.Cards.Length} first={data.Cards[0]}");
            ScreenReader.Say(Loc.Get("gate_rewards_help", data.Cards.Length));
            Speak(false, true);
        }

        // Read-only list navigation. Card identities come from the native
        // reward list, not screen position or a recycled image cell.
        internal static bool Handle(GameObject gate)
        {
            if (_snapshot == null) return false;
            if (_gate == null || gate != _gate || !gate.activeInHierarchy)
            { _snapshot = null; _gate = null; return false; }
            var current = Read(gate);
            if (current == null || current.View != _snapshot.View || current.Title != _snapshot.Title
                || !GateRewardPolicy.Same(current.Cards, _snapshot.Cards))
            {
                _snapshot = null; _gate = null;
                ScreenReader.Say(Loc.Get("gate_rewards_changed"));
                return true;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.Escape) || InputManager.TryConsumeKeyDown(KeyCode.Backspace))
            {
                _snapshot = null; _gate = null;
                ScreenReader.Say(Loc.Get("gate_rewards_exit"));
            }
            else if (InputManager.TryConsumeKeyDownOrRepeat(KeyCode.DownArrow)
                || InputManager.TryConsumeKeyDownOrRepeat(KeyCode.RightArrow))
            { _index = GateRewardPolicy.Move(_index, 1, _snapshot.Cards.Length); Speak(false); }
            else if (InputManager.TryConsumeKeyDownOrRepeat(KeyCode.UpArrow)
                || InputManager.TryConsumeKeyDownOrRepeat(KeyCode.LeftArrow))
            { _index = GateRewardPolicy.Move(_index, -1, _snapshot.Cards.Length); Speak(false); }
            else if (InputManager.TryConsumeKeyDown(KeyCode.V) || InputManager.TryConsumeKeyDown(KeyCode.Return)
                || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)) Speak(true);
            else if (InputManager.TryConsumeKeyDown(KeyCode.Tab)) Speak(false);
            else if (InputManager.TryConsumeKeyDown(KeyCode.T))
                ScreenReader.Say(Loc.Get("gate_rewards_help", _snapshot.Cards.Length));
            return true;
        }

        private static void Speak(bool verbose, bool queued = false)
        {
            if (_snapshot == null) return;
            int id = _snapshot.Cards[_index];
            string text = verbose ? CardFormatter.FormatVerbose(id) : CardFormatter.FormatCompact(id);
            string announcement = Loc.Get("gate_rewards_card", _index + 1, _snapshot.Cards.Length, text);
            if (queued) ScreenReader.SayQueued(announcement); else ScreenReader.Say(announcement);
        }
    }
}
