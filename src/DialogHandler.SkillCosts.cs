using System;
using System.Collections.Generic;
using Il2CppYgomGame.Utility;
using Il2CppYgomSystem.Htjson;
using UnityEngine;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private void AddSkillCostRows(GameObject root)
        {
            var nodes = root.GetComponentsInChildren<HtjsonNode>(false);
            foreach (var node in nodes)
            {
                var data = node?.replaceParam;
                if (data != null
                    && data.TryGetValue("%basepath%", out var basePath) && basePath?.ToString() == "SkillList"
                    && data.TryGetValue("%mode%", out var viewMode) && viewMode?.ToString() == "tradermode")
                {
                    _items.RemoveAll(item => item.Go != null && item.Go.name == "Scrollbar");
                    break;
                }
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in nodes)
            {
                var data = node?.replaceParam;
                if (data == null
                    || !data.TryGetValue("%mode%", out var mode) || mode?.ToString() != "tradermode"
                    || !data.TryGetValue("%sublooppath%", out var path)
                    || path?.ToString()?.StartsWith("SkillList.data.", StringComparison.Ordinal) != true
                    || !data.TryGetValue("%itemid%", out var item)
                    || !int.TryParse(item?.ToString(), out int id) || id <= 0
                    || !data.TryGetValue("%needitem%", out var need)
                    || !int.TryParse(need?.ToString(), out int required) || required <= 0
                    || !data.TryGetValue("%myitem%", out var balance)
                    || !int.TryParse(balance?.ToString(), out int owned) || owned < 0)
                    continue;

                string name = ItemUtil.GetName(id);
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(path.ToString())) continue;
                if (data.TryGetValue("%chara%", out var character)
                    && int.TryParse(character?.ToString(), out int cid) && cid > 0)
                {
                    string characterName = CharaUtil.GetName(cid);
                    if (!string.IsNullOrWhiteSpace(characterName)) name = $"{characterName}，{name}";
                }

                string label = Loc.Get("gate_cost_item", name, required, owned);
                _items.Add(new DialogItem { Go = node.gameObject, Label = label, Type = ItemType.Header });
            }
        }
    }
}
