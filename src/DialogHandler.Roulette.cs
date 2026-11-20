using UnityEngine;
using UnityEngine.UI;
namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private void AddRouletteDifficultyButtons(GameObject root)
        {
            // These native buttons are named "vl". The generic layout filter
            // intentionally skips that name; only admit the verified event rows.
            foreach (var button in root.GetComponentsInChildren<Button>(false))
            {
                if (button == null || !button.gameObject.activeInHierarchy || !button.IsInteractable()) continue;
                string path = null;
                var current = button.transform;
                for (int depth = 0; depth < 3 && current != null; depth++, current = current.parent)
                {
                    var data = current.GetComponent<Il2CppYgomSystem.Htjson.HtjsonNode>()?.replaceParam;
                    if (data == null) continue;
                    foreach (string key in new[] { "%datapath%", "%sublooppath%" })
                        if (data.TryGetValue(key, out var value) && value != null
                            && RouletteAccessibilityPolicy.IsDifficultyRow(value.ToString()))
                        { path = value.ToString(); break; }
                    if (path != null) break;
                }
                if (path == null) continue;
                string label = LabelExtractor.StripRichText(Il2CppYgomSystem.Utility.ClientWork
                    .getStringByJsonPath(path + ".label", "")).Trim();
                if (string.IsNullOrWhiteSpace(label)) label = LabelExtractor.GetLabel(button.gameObject);
                if (string.IsNullOrWhiteSpace(label) || LabelExtractor.IsPlaceholderText(label)) continue;
                _items.RemoveAll(item => item.Go != null
                    && (item.Go == button.gameObject || item.Go.transform.IsChildOf(button.transform)));
                _items.Add(new DialogItem { Go = button.gameObject, Label = label, Type = ItemType.Button });
            }
        }
    }
}
