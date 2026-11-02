using UnityEngine;
using System.Linq;
using Il2CppYgomGame.Single.OverLap;

namespace DuelLinksAccess
{
    internal static class HomeBannerAccessibility
    {
        private static string ArtKey(SingleShopHtjsonBanner entry)
        {
            if (entry == null) return null;
            var images = entry.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                .Where(i => i != null && i.sprite != null).Select(i => i.sprite.name).ToArray();
            var textures = entry.GetComponentsInChildren<UnityEngine.UI.RawImage>(true)
                .Where(i => i != null && i.texture != null).Select(i => i.texture.name).ToArray();
            return BannerDuplicatePolicy.ArtKey(images, textures);
        }

        internal static bool Redundant(GameObject root, GameObject go)
        {
            if (root == null || root.name != "Single" || go == null) return false;
            try
            {
                var owner = go.GetComponentInParent<SingleShopBanner>();
                if (owner != null && owner.listOpen && owner.inputCover == go)
                {
                    bool hasSwitch = owner.GetComponentsInChildren<UnityEngine.UI.Selectable>(true)
                        .Any(s => s != null && s.gameObject.activeInHierarchy && s.IsInteractable()
                            && s.gameObject.name == "ListOpenButton");
                    if (BannerDuplicatePolicy.HideCover(true, hasSwitch)) return true;
                }
                var entry = go.GetComponentInParent<SingleShopHtjsonBanner>();
                var carousel = entry?.GetComponentInParent<SingleShopBanner>();
                if (carousel == null || !carousel.LoopScroll || carousel.itemNum <= 0) return false;
                string key = ArtKey(entry);
                if (key == null) return false;
                // Prefer actual expanded-list entries over their scrolling copies.
                if (carousel.listOpen && carousel.OpenBannerList != null)
                    foreach (var visible in carousel.OpenBannerList)
                    {
                        var other = visible?.GetComponent<SingleShopHtjsonBanner>();
                        if (other != null && other.gameObject.activeInHierarchy && other != entry && ArtKey(other) == key)
                        {
                            bool itselfInList = false;
                            foreach (var candidate in carousel.OpenBannerList) if (candidate == entry.gameObject) itselfInList = true;
                            if (!itselfInList) return true;
                        }
                    }
                var parent = entry.transform.parent;
                if (parent == null || parent.name != "Content") return false;
                var siblings = parent.GetComponentsInChildren<SingleShopHtjsonBanner>(false)
                    .Where(e => e != null && e.transform.parent == parent)
                    .OrderBy(e => e.transform.GetSiblingIndex()).ToArray();
                int index = System.Array.FindIndex(siblings, e => e == entry);
                return BannerDuplicatePolicy.IsLoopCopy(carousel.LoopScroll, carousel.itemNum, index, siblings.Select(ArtKey).ToArray());
            }
            catch { return false; }
        }
        internal static void Diagnose(GameObject root)
        {
            if (!Main.DebugMode || root == null || root.name != "Single") return;
            try
            {
                foreach (var carousel in root.GetComponentsInChildren<SingleShopBanner>(true))
                    if (carousel != null && carousel.gameObject.activeInHierarchy)
                        DebugLogger.Log(LogCategory.Handler, "HomeBannerData",
                            $"carousel id={carousel.GetInstanceID()} listOpen={carousel.listOpen} loop={carousel.LoopScroll} itemNum={carousel.itemNum} page={carousel.SnapPage} openList={carousel.OpenBannerList?.Count ?? -1}");
                foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
                {
                    var go = button?.gameObject;
                    if (go == null || !go.activeInHierarchy || (go.name != "card"
                        && go.name != "ShopBanner(Clone)" && go.name != "ShopHtjsonBannerEntry(Clone)")) continue;
                    string path = go.name;
                    for (var p = go.transform.parent; p != null && p != root.transform; p = p.parent) path = p.name + "/" + path;
                    string types = string.Join(",", go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetIl2CppType().Name));
                    string text = string.Join(" | ", go.GetComponentsInChildren<UnityEngine.UI.Text>(true)
                        .Where(t => t != null).Select(t => LabelExtractor.StripRichText(t.text)));
                    DebugLogger.Log(LogCategory.Handler, "HomeBannerData", $"path={path} components=[{types}] text=[{text}]");
                    var bannerData = go.GetComponent<SingleShopHtjsonBanner>();
                    if (bannerData != null)
                    {
                        string dataPath = bannerData.dataPath;
                        DebugLogger.Log(LogCategory.Handler, "HomeBannerData", $"banner dataPath={dataPath} template={bannerData.jsonFilePath}");
                        var carousel = bannerData.GetComponentInParent<SingleShopBanner>();
                        bool inOpenList = false;
                        if (carousel?.OpenBannerList != null)
                            foreach (var candidate in carousel.OpenBannerList)
                                if (candidate == go) inOpenList = true;
                        DebugLogger.Log(LogCategory.Handler, "HomeBannerData",
                            $"entry id={go.GetInstanceID()} parent={go.transform.parent?.name} sibling={go.transform.GetSiblingIndex()} inOpenList={inOpenList}");
                        var sprites = go.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                            .Where(i => i != null && i.sprite != null).Select(i => i.sprite.name).Distinct();
                        var textures = go.GetComponentsInChildren<UnityEngine.UI.RawImage>(true)
                            .Where(i => i != null && i.texture != null).Select(i => i.texture.name).Distinct();
                        DebugLogger.Log(LogCategory.Handler, "HomeBannerData", $"art sprites=[{string.Join(" | ", sprites)}] textures=[{string.Join(" | ", textures)}]");
                        DumpBannerParams(bannerData.node?.GetReplaceParam(), "entry");
                        DumpBannerParams(bannerData.node?.parentContext?.GetReplaceParam(), "context");
                        if (!string.IsNullOrWhiteSpace(dataPath))
                            foreach (string suffix in new[] { ".name", ".title", ".label", ".pack.name", ".data.name", ".banner.name", ".shop.name" })
                            {
                                string value = Il2CppYgomSystem.Utility.ClientWork.getStringByJsonPath(dataPath + suffix, "");
                                if (!string.IsNullOrWhiteSpace(value)) DebugLogger.Log(LogCategory.Handler, "HomeBannerData", $"native {suffix}={value}");
                            }
                    }
                    var card = go.GetComponent<Il2CppYgomGame.Htjson.CardWidget>();
                    if (card != null)
                    {
                        int mrk = card.iid.GetMrk();
                        string cardName = mrk > 0 ? CardFormatter.GetName(mrk) : "UNKNOWN";
                        DebugLogger.Log(LogCategory.Handler, "HomeBannerData", $"card loaded={card.loaded} category={card.iid.GetCategory()} mrk={mrk} name={cardName} detailOwner={card.isDetailOwner} hasLink={card.link != null}");
                    }
                    for (var t = go.transform; t != null && t != root.transform; t = t.parent)
                    {
                        var node = t.GetComponent<Il2CppYgomSystem.Htjson.HtjsonNode>();
                        if (node?.replaceParam == null) continue;
                        foreach (var pair in node.replaceParam)
                        {
                            // Read only public label/binding identity fields,
                            // not URLs or unrelated account data.
                            string key = pair.Key;
                            if (!(key.Contains("path") || key.Contains("name") || key.Contains("title")
                                || key.Contains("label") || key.Contains("mrk") || key.Contains("iid") || key.Contains("pack"))) continue;
                            if (key.ToLowerInvariant().Contains("url")) continue;
                            string value = pair.Value?.ToString() ?? "null";
                            if (value.Length > 180) value = value.Substring(0, 180);
                            DebugLogger.Log(LogCategory.Handler, "HomeBannerData", $"node={t.name} binding {key}={value}");
                        }
                    }
                }
            }
            catch (System.Exception ex) { DebugLogger.Log(LogCategory.Handler, "HomeBannerData", ex.Message); }
        }

        private static void DumpBannerParams(Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object> data, string source)
        {
            if (data == null) return;
            foreach (var pair in data)
            {
                string key = pair.Key;
                string type = pair.Value?.GetIl2CppType()?.Name ?? "null";
                string value = "(not logged)";
                string plain = key.Trim('%').ToLowerInvariant();
                if (plain == "name" || plain == "title" || plain == "label" || plain == "datapath" || plain == "pack_name")
                    value = pair.Value?.ToString() ?? "null";
                if (value.Length > 180) value = value.Substring(0, 180);
                DebugLogger.Log(LogCategory.Handler, "HomeBannerData", $"{source} param key={key} type={type} value={value}");
            }
        }

        private static SingleShopBanner Find(GameObject go)
        {
            if (go == null || go.name != "ListOpenButton") return null;
            bool home = false;
            for (var t = go.transform; t != null; t = t.parent)
                if (t.name == "Single") { home = true; break; }
            return home ? go.GetComponentInParent<SingleShopBanner>() : null;
        }

        internal static string Label(GameObject go)
        {
            try
            {
                if (go != null && go.activeInHierarchy)
                {
                    var entry = go.GetComponentInParent<SingleShopHtjsonBanner>();
                    if (entry != null && entry.GetComponentInParent<SingleShopBanner>() != null)
                    {
                        var card = go.GetComponent<Il2CppYgomGame.Htjson.CardWidget>();
                        if (card != null)
                        {
                            string name = CardName(card);
                            return name == null ? null : Loc.Get("home_banner_card", name);
                        }
                        if (go == entry.gameObject)
                        {
                            if (entry.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                                .Any(i => i != null && i.sprite != null && i.sprite.name == "Special11_Bnr_YDP_1"))
                                return Loc.Get("home_banner_duelist_point");
                            var names = entry.GetComponentsInChildren<Il2CppYgomGame.Htjson.CardWidget>(true)
                                .Where(c => c != null && c.gameObject.activeInHierarchy).Select(CardName)
                                .Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToArray();
                            return names.Length == 0 ? Loc.Get("home_banner_unknown")
                                : Loc.Get("home_banner_cards", string.Join("、", names));
                        }
                    }
                }
                var banner = Find(go);
                return banner == null ? null : Loc.Get(banner.listOpen ? "home_banner_close" : "home_banner_open");
            }
            catch { return null; }
        }

        private static string CardName(Il2CppYgomGame.Htjson.CardWidget card)
        {
            if (card == null || !card.loaded || card.iid.GetCategory().ToString() != "CARD") return null;
            int id = card.iid.GetMrk();
            if (id <= 0) return null;
            string name = CardFormatter.GetName(id);
            return string.IsNullOrWhiteSpace(name) || name == Loc.Get("duel_unknown_card") ? null : name;
        }

        internal static bool Activate(GameObject go)
        {
            var banner = Find(go);
            if (banner == null) return false;
            banner.OnListButton();
            return true;
        }
    }
}
