using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private GameObject _eventDetailTabRoot;
        private string _eventDetailSelectedTab;
        private float _eventDetailPollDelay;

        private static List<(RectTransform Rect, string Label, bool Selected)> ReadEventDetailTabs(GameObject root)
        {
            var tabs = new List<(RectTransform Rect, string Label, bool Selected)>();
            foreach (var tab in root.GetComponentsInChildren<Il2CppYgomSystem.UI.YgomTabButton>(false))
            {
                var label = tab.isSelected ? tab.selectLabel ?? tab.normalLabel : tab.normalLabel;
                string name = LabelExtractor.StripRichText(label?.text ?? "").Trim();
                if (EventDetailText.IsTabLabel(name) && tab.transform.TryCast<RectTransform>() is RectTransform rect)
                    tabs.Add((rect, name, tab.isSelected));
            }
            if (tabs.Count == 2) return tabs;
            tabs.Clear();
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(false))
            {
                var normal = rect.Find("Normal");
                var selected = rect.Find("Select");
                if (normal == null || selected == null) continue;
                string label = AccessibleUiText.Read((selected.gameObject.activeInHierarchy ? selected : normal).gameObject);
                if (EventDetailText.IsTabLabel(label)) tabs.Add((rect, label, selected.gameObject.activeInHierarchy));
            }
            if (tabs.Count == 2) return tabs;
            tabs.Clear();
            // Older Htjson event introductions use sibling native Buttons;
            // their selected tab is non-interactable rather than Select/Normal.
            foreach (var button in root.GetComponentsInChildren<Button>(false))
            {
                if (button.gameObject.name != "btn" || button.transform.parent?.name != "hl") continue;
                string name = LabelExtractor.StripRichText(LabelExtractor.GetLabel(button.gameObject)).Trim();
                if (EventDetailText.IsTabLabel(name) && button.transform.TryCast<RectTransform>() is RectTransform rect)
                    tabs.Add((rect, name, !button.interactable));
            }
            return tabs;
        }

        private static bool HasEventDetailTabs(GameObject root)
        {
            var tabs = ReadEventDetailTabs(root);
            return tabs.Count == 2 && tabs[0].Label != tabs[1].Label;
        }

        private void AddEventDetailParagraphs(GameObject root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var order = new Dictionary<GameObject, int>();
            for (int i = 0; i < transforms.Length; i++)
                order[transforms[i].gameObject] = i;
            var seenObjects = new HashSet<GameObject>();
            var spoken = new HashSet<string>();
            foreach (var item in _items)
                foreach (string paragraph in EventDetailText.Paragraphs(item.Label)) spoken.Add(paragraph);
            foreach (var transform in transforms)
            {
                var go = transform.gameObject;
                if (!go.activeInHierarchy || !seenObjects.Add(go)) continue;
                bool scrollContent = false;
                for (var parent = transform.parent; parent != null && parent != root.transform; parent = parent.parent)
                    if (parent.name == "ScrollContent") { scrollContent = true; break; }
                if (!scrollContent || go.GetComponentInParent<Scrollbar>() != null) continue;
                // A clickable reward already carries its own text and native action.
                if (_items.Any(item => item.Type != ItemType.Header && item.Go != null
                    && transform.IsChildOf(item.Go.transform))) continue;
                var accessor = go.GetComponent<Il2CppYgomSystem.UI.YgomTextAccessor>();
                var text = go.GetComponent<Text>();
                string value = LabelExtractor.StripRichText(accessor != null ? accessor.text : text?.text ?? "");
                foreach (string paragraph in EventDetailText.Paragraphs(value))
                {
                    if (LabelExtractor.IsPlaceholderText(paragraph) || !spoken.Add(paragraph)) continue;
                    int insert = _items.FindIndex(item => item.Go != null
                        && order.TryGetValue(item.Go, out int index) && index > order[go]);
                    var info = new DialogItem { Go = go, SettingsTarget = go,
                        Label = paragraph, Type = ItemType.Header };
                    if (insert < 0) _items.Add(info); else _items.Insert(insert, info);
                }
            }
        }

        // Native Htjson tabs can replace their content without replacing the dialog VC.
        // Observe the selected tab as well as clicks made through our keyboard list.
        private void PollEventDetailTabs()
        {
            if (!_scanned || _eventDetailTabRoot == null
                || GetTopVcRoot("dialog") != _eventDetailTabRoot) return;
            _eventDetailPollDelay -= Time.deltaTime;
            if (_eventDetailPollDelay > 0f) return;
            _eventDetailPollDelay = 0.2f;
            var tabs = ReadEventDetailTabs(_eventDetailTabRoot);
            if (tabs.Count != 2 || tabs[0].Label == tabs[1].Label) return;
            var selectedTab = tabs.Find(tab => tab.Selected);
            string selected = selectedTab.Label;
            if (string.IsNullOrEmpty(selected) || selected == _eventDetailSelectedTab) return;
            if (!EventDetailText.IsTabLabel(selected)) return;
            _eventDetailSelectedTab = selected;
            _pendingPlayerIconTab = selectedTab.Rect;
            _playerIconRefreshRoot = _eventDetailTabRoot;
            _scanned = false;
            _scanDelay = 0.35f;
            _scanAttempts = 0;
        }
    }
}
