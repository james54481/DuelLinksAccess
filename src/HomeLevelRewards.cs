using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Il2CppYgomGame.Menu;
using Il2CppYgomSystem.UI;

namespace DuelLinksAccess
{
    // Reads only the active character panel; never derives a reward from a level number.
    internal static class HomeLevelRewards
    {
        internal sealed class Entry
        {
            internal GameObject Go;
            internal string Label;
            internal Action Activate;
            internal Func<string> LiveLabel;
        }
        private static readonly Dictionary<string, int> KnownCards = new();
        private static int _request;
        private static string Key(HomeStatePanel panel, GameObject row)
            => panel.cid + ":" + row.GetInstanceID() + ":" + AccessibleUiText.Read(row) + ":" + TextureCardId(row);
        private static string RewardLabel(HomeStatePanel panel, GameObject row, string text, string reward)
        {
            int id = TextureCardId(row);
            if (id > 0 || KnownCards.TryGetValue(Key(panel, row), out id)) reward = CardFormatter.GetName(id);
            return Loc.Get("home_level_reward", text, string.IsNullOrWhiteSpace(reward) ? "UNKNOWN" : reward);
        }
        private static int TextureCardId(GameObject row)
        {
            if (row == null || !row.activeInHierarchy) return 0;
            // Card rewards here are Images with unnamed sprites and a numeric texture.
            // Item sprites (ItemA####), the shared atlas and font textures are excluded.
            var textures = row.GetComponentsInChildren<Image>(false)
                .Where(image => image != null && image.gameObject.activeInHierarchy
                    && string.IsNullOrEmpty(image.sprite?.name) && image.mainTexture != null)
                .Select(image => image.mainTexture.name);
            int id = HomeRewardCardPolicy.Resolve(textures);
            if (id <= 0) return 0;
            string name = CardFormatter.GetName(id);
            return string.IsNullOrWhiteSpace(name) || name == Loc.Get("duel_unknown_card") ? 0 : id;
        }
        internal static bool IsHandledScrollbar(GameObject root, GameObject go)
        {
            try
            {
                if (go == null) return false;
                var panel = Panel(root);
                return panel != null && (panel.profileScrollRect?.verticalScrollbar?.gameObject == go
                    || panel.profileScrollRect?.horizontalScrollbar?.gameObject == go
                    || panel.stateScrollRect?.horizontalScrollbar?.gameObject == go
                    || panel.stateScrollRect?.verticalScrollbar?.gameObject == go);
            }
            catch { return false; }
        }
        internal static HomeStatePanel Panel(GameObject root)
        {
            if (root == null || root.name != "Home") return null;
            var managers = ViewControllerManager.namedManager;
            if (managers == null || !managers.TryGetValue("content", out var manager)) return null;
            var top = manager?.GetStackTopViewController();
            return top != null && top.gameObject == root ? new HomeViewController(top.Pointer).crntStatePanel : null;
        }
        internal static List<Entry> Read(GameObject root)
        {
            var result = new List<Entry>();
            try
            {
                var panel = Panel(root);
                if (panel == null) return result;
                string rank = panel.rankText != null ? LabelExtractor.StripRichText(panel.rankText.text) : "UNKNOWN";
                result.Add(new Entry { Go = root, Label = Loc.Get("home_current_level", rank) });
                if (panel.profileText != null && !string.IsNullOrWhiteSpace(panel.profileText.text))
                    result.Add(new Entry { Go = root, Label = LabelExtractor.StripRichText(panel.profileText.text) });
                var content = panel.stateScrollRect?.content;
                if (content == null) return result;
                for (int i = 0; i < content.childCount; i++)
                {
                    var row = content.GetChild(i).gameObject;
                    if (!row.activeInHierarchy) continue;
                    string text = AccessibleUiText.Read(row);
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    var names = new List<string>();
                    foreach (var card in row.GetComponentsInChildren<Il2CppYgomGame.Deck.CardBase>(false))
                        if (card.Mrk > 0) names.Add(CardFormatter.GetName(card.Mrk));
                    foreach (var card in row.GetComponentsInChildren<Il2CppYgomGame.Htjson.CardWidget>(false))
                        if (card.loaded && card.iid.GetCategory().ToString() == "CARD" && card.iid.GetMrk() > 0)
                            names.Add(CardFormatter.GetName(card.iid.GetMrk()));
                    foreach (var image in row.GetComponentsInChildren<Image>(false))
                    {
                        int? id = GateCostPolicy.ItemId(image.sprite?.name);
                        if (id.HasValue) names.Add(LabelExtractor.StripRichText(Il2CppYgomGame.Utility.ItemUtil.GetName(id.Value)));
                    }
                    int textureId = TextureCardId(row);
                    if (textureId > 0) names.Add(CardFormatter.GetName(textureId));
                    string reward = string.Join("、", names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
                    string label = RewardLabel(panel, row, text, reward);
                    var control = row.GetComponentInChildren<Selectable>(false);
                    var target = control?.gameObject;
                    result.Add(new Entry { Go = row, Label = label,
                        LiveLabel = () => RewardLabel(panel, row, text, reward),
                        Activate = target == null ? null : () => Activate(panel, row, target, textureId > 0 || string.IsNullOrWhiteSpace(reward)) });
                    DebugLogger.Log(LogCategory.Handler, "HomeRewards", $"{row.name}: {label}; components=" +
                        string.Join(",", row.GetComponentsInChildren<Component>(false).Select(c => c.GetIl2CppType().Name).Distinct()) +
                        "; images=" + string.Join(",", row.GetComponentsInChildren<Image>(false).Select(c => c.sprite?.name)) +
                        "; textures=" + string.Join(",", row.GetComponentsInChildren<Graphic>(false).Select(c => c.mainTexture?.name)));
                }
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "HomeRewards", ex.Message); }
            return result;
        }
        private static System.Collections.IEnumerator ReadOpenedCard(HomeStatePanel panel, GameObject row, string key, int request, int beforeId, bool beforeVisible)
        {
            yield return new WaitForSeconds(0.3f);
            float deadline = Time.unscaledTime + 2f;
            while (Time.unscaledTime < deadline)
            {
                if (request != _request || panel == null || row == null || !row.activeInHierarchy
                    || Key(panel, row) != key) yield break;
                // Require the original Home panel to remain current, even when a generic VC wrapper is returned.
                var managers = ViewControllerManager.namedManager;
                if (managers == null || !managers.TryGetValue("content", out var manager)) yield break;
                var top = manager?.GetStackTopViewController();
                if (top?.gameObject?.name != "Home" || Panel(top.gameObject)?.Pointer != panel.Pointer) yield break;
                var detail = CardDetailViewControllerManager.Instance?.GetCardDetailVC();
                if (detail?.gameObject?.activeInHierarchy == true)
                {
                    int id = detail.GetMrk();
                    int expected = TextureCardId(row);
                    if (expected <= 0 && KnownCards.TryGetValue(key, out int known)) expected = known;
                    if (HomeRewardDetailPolicy.CanRead(beforeVisible, beforeId, id, expected))
                    {
                        KnownCards[key] = id;
                        DebugLogger.Log(LogCategory.Handler, "HomeRewards", $"Opened reward {row.name}: mrk={id}");
                        ScreenReader.Say(CardFormatter.FormatVerbose(id));
                        yield break;
                    }
                }
                yield return null;
            }
            ScreenReader.Say(Loc.Get("home_reward_detail_unknown"));
        }
        private static void Activate(HomeStatePanel panel, GameObject row, GameObject target, bool cardReward)
        {
            if (row == null || target == null || !row.activeInHierarchy) return;
            var control = target.GetComponent<Selectable>();
            if (control != null && !control.IsInteractable()) { ScreenReader.Say(Loc.Get("ui_action_unavailable")); return; }
            // Invoke the same pointer handler as the native reward, without guessing a VC method.
            var data = new PointerEventData(EventSystem.current);
            string key = Key(panel, row);
            var previous = CardDetailViewControllerManager.Instance?.GetCardDetailVC();
            bool beforeVisible = previous?.gameObject?.activeInHierarchy == true;
            int beforeId = beforeVisible ? previous.GetMrk() : 0;
            int request = ++_request;
            if (!ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler))
            { ScreenReader.Say(Loc.Get("screen_click_error")); return; }
            if (cardReward || KnownCards.ContainsKey(key))
                MelonLoader.MelonCoroutines.Start(ReadOpenedCard(panel, row, key, request, beforeId, beforeVisible));

        }
    }
}
