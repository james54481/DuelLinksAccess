using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomSystem.Htjson;
using Il2CppYgomSystem.Utility;
using MelonLoader;

namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private static string AlbumBoundPath(GameObject go)
        {
            for (var t = go?.transform; t != null; t = t.parent)
            {
                var data = t.GetComponent<HtjsonNode>()?.replaceParam;
                if (data == null) continue;
                foreach (string key in new[] { "%datapath%", "%sublooppath%" })
                    if (data.TryGetValue(key, out var value) && value != null
                        && value.ToString().StartsWith("Album.data.", StringComparison.Ordinal)) return value.ToString();
            }
            return null;
        }

        private static string AlbumTitle(string path)
            => LabelExtractor.StripRichText(AlbumTextPolicy.Title(
                ClientWork.getStringByJsonPath(path, ""), LabelExtractor.ResolveCharaName)).Trim();

        private void AlbumLink(GameObject root, HtjsonNode node, string path, string expected)
        {
            if (root != _screenRoot || !LabelExtractor.IsAlive(root) || !root.activeInHierarchy
                || node == null || !LabelExtractor.IsAlive(node.gameObject)) return;
            var link = ClientWork.getObjectByJsonPath(path, null);
            if (ReferenceEquals(link, null) || link.ToString() != expected) return;
            node.GetReceiver()?.HtjsonLink(link, root);
            MelonLogger.Msg($"[SceneAlbum] Open native content {path}");
        }

        private void AddSceneAlbumContents(GameObject root)
        {
            if (root == null || (root.name != "eventselect" && root.name != "Album")) return;
            _items.RemoveAll(i => i.Go != null && i.Go.GetComponentInParent<Scrollbar>() != null);
            foreach (var item in _items)
            {
                if (item.Go?.name == "BackButton") item.Label = Loc.Get("screen_back");
                if (item.Go?.name == "HomeButton") item.Label = Loc.Get("deck_view_home");
            }
            var nodes = root.GetComponentsInChildren<HtjsonNode>(false);
            var context = nodes.FirstOrDefault(n => n != null && n.GetReceiver() != null);
            if (context == null) return;
            if (root.name == "eventselect")
            {
                var rows = ClientWork.getListByJsonPath("Album.data.eventlist", null);
                if (rows == null) return;
                int inserted = 0;
                for (int index = 0; index < rows.Count; index++)
                {
                    string row = "Album.data.eventlist." + index;
                    string title = AlbumTitle(row + ".banner.title");
                    if (string.IsNullOrWhiteSpace(title)) title = AlbumTitle(row + ".banner.text");
                    var link = ClientWork.getObjectByJsonPath(row + ".banner.link", null);
                    if (string.IsNullOrWhiteSpace(title) || ReferenceEquals(link, null)) continue;
                    string visible = "";
                    foreach (var node in nodes)
                    {
                        if (node.GetComponentInParent<Scrollbar>() != null) continue;
                        var rect = node.transform.TryCast<RectTransform>();
                        if (rect == null || rect.rect.height > Screen.height * 0.45f) continue;
                        string binding = AlbumBoundPath(node.gameObject);
                        if (binding == row || binding == row + ".banner")
                        {
                            string text = AccessibleUiText.Read(node.gameObject);
                            if (AlbumTextPolicy.Cost(text, out _, out _)) { visible = text; break; }
                        }
                    }
                    string label = title;
                    if (AlbumTextPolicy.Cost(visible, out long cost, out long held))
                        label = Loc.Get("album_locked_cost", title, cost, held);
                    else if (ClientWork.getBoolByJsonPath(row + ".banner.lock", false))
                        label = Loc.Get("album_locked_unknown", title);
                    string linkPath = row + ".banner.link", expected = link.ToString();
                    _items.Insert(inserted++, new ScreenItem { Go = root, Label = label, Type = ItemType.Button,
                        NativeAction = () => AlbumLink(root, context, linkPath, expected) });
                    MelonLogger.Msg($"[SceneAlbumEvent] row={index} label={label} lock={ClientWork.getStringByJsonPath(row + ".banner.lock", "")}");
                }
                return;
            }
            foreach (var item in _items.ToArray())
            {
                string path = AlbumBoundPath(item.Go);
                if (AlbumTextPolicy.IsPlayPath(path))
                {
                    string title = AlbumTitle(path + ".title");
                    var link = ClientWork.getObjectByJsonPath(path + ".link", null);
                    if (string.IsNullOrWhiteSpace(title) || ReferenceEquals(link, null)) continue;
                    item.Label = Loc.Get("album_play", title);
                    if (ClientWork.getBoolByJsonPath(path + ".voice", false)) item.Label += Loc.Get("album_voice");
                    string linkPath = path + ".link", expected = link.ToString();
                    item.NativeAction = () => AlbumLink(root, context, linkPath, expected);
                }
                else if (AlbumTextPolicy.IsCategoryPath(path))
                {
                    var button = item.Go?.GetComponent<Button>();
                    if (button == null || !button.IsInteractable()) continue;
                    // Header scopes can inherit the final loop row's path.
                    // The native button text is already resolved and belongs
                    // to this control; do not replace it with inherited data.
                    string category = AlbumTextPolicy.Category(item.Label);
                    if (!string.IsNullOrWhiteSpace(category)) item.Label = category;
                    item.NativeAction = () =>
                    {
                        if (root != _screenRoot || button == null || !button.gameObject.activeInHierarchy
                            || !button.IsInteractable()) return;
                        _noticeTabFocus = item.Label;
                        button.onClick.Invoke();
                        _tabRescanDelay = 0.35f;
                    };
                }
            }
        }

        // james54481, 2026-11-11: Album's image rows have no Selectable.
        // Read native row text and dispatch its own Htjson link, never build URLs.
        private void AddSceneAlbumSeries(GameObject root)
        {
            if (root == null || root.name != "worldselect") return;
            var rows = ClientWork.getListByJsonPath("Album.data.worldlist", null);
            if (rows == null) return;
            _items.RemoveAll(i => i.Go != null && i.Go.GetComponentInParent<Scrollbar>() != null);
            foreach (var item in _items)
            {
                if (item.Go?.name == "BackButton") item.Label = Loc.Get("screen_back");
                if (item.Go?.name == "HomeButton") item.Label = Loc.Get("deck_view_home");
            }
            HtjsonNode contextNode = null;
            foreach (var node in root.GetComponentsInChildren<HtjsonNode>(false))
                if (node != null && node.GetReceiver() != null) { contextNode = node; break; }
            if (contextNode == null) return;
            int inserted = 0;
            for (int index = 0; index < rows.Count; index++)
            {
                string path = "Album.data.worldlist." + index;
                var link = ClientWork.getObjectByJsonPath(path + ".link", null);
                if (ReferenceEquals(link, null)) continue;
                string label = LabelExtractor.StripRichText(ClientWork.getStringByJsonPath(path + ".text", "")).Trim();
                string seriesImage = ClientWork.getStringByJsonPath(path + ".seriesimg", "");
                string characterImage = ClientWork.getStringByJsonPath(path + ".charaimg", "");
                string seriesKey = AlbumSeriesLabels.Key(seriesImage) ?? AlbumSeriesLabels.Key(characterImage);
                if (string.IsNullOrWhiteSpace(label) && seriesKey != null) label = Loc.Get(seriesKey);
                if (string.IsNullOrWhiteSpace(label)) label = Loc.Get("album_unknown_series", index + 1);
                string expectedLink = link.ToString();
                var dict = ClientWork.getDictionaryByJsonPath(path, null);
                if (dict != null)
                    foreach (var entry in dict)
                        if (entry.Key != "link") MelonLogger.Msg($"[SceneAlbum] row={index} {entry.Key}={ClientWork.getStringByJsonPath(path + "." + entry.Key, "")}");
                MelonLogger.Msg($"[SceneAlbum] row={index} label={label}");
                var nodeForAction = contextNode;
                _items.Insert(inserted++, new ScreenItem
                {
                    Go = root, Label = label, Type = ItemType.Button,
                    NativeAction = () =>
                    {
                        if (root != _screenRoot || !LabelExtractor.IsAlive(root) || !root.activeInHierarchy
                            || !LabelExtractor.IsAlive(nodeForAction.gameObject)) return;
                        var currentLink = ClientWork.getObjectByJsonPath(path + ".link", null);
                        if (ReferenceEquals(currentLink, null) || currentLink.ToString() != expectedLink) return;
                        var receiver = nodeForAction.GetReceiver();
                        if (receiver == null) return;
                        receiver.HtjsonLink(currentLink, root);
                        MelonLogger.Msg($"[SceneAlbum] Activated native row path={path}");
                    }
                });
            }
        }
    }
}
