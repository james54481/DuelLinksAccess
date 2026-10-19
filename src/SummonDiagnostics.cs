using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Il2CppYgomGame.Duel;

namespace DuelLinksAccess
{
    /// <summary>
    /// Optional observers only. No game commands, argument edits or return overrides.
    /// Registered after game-ready, never from assembly attributes or startup.
    /// </summary>
    public static class SummonDiagnostics
    {
        private static bool _registered;
        private static long _sequence;
        private static float _nextFieldRead;
        private static int _fieldReadsRemaining;

        public static void Register(HarmonyLib.Harmony harmony)
        {
            if (_registered) return;
            _registered = true;
            int installed = 0;
            installed += Patch(harmony, typeof(RunEffectWorker2D), "OnTapLocator", nameof(Tap), false);
            installed += Patch(harmony, typeof(RunEffectWorker2D), "OnDoCardCommand", nameof(Command), false);
            installed += Patch(harmony, typeof(CardCommand), "Open", nameof(PopupOpen), false);
            installed += Patch(harmony, typeof(CardCommand), "OnCommand", nameof(PopupCommand), false);
            installed += Patch(harmony, typeof(EmotionalList), "Open", nameof(ListOpen), true);
            installed += Patch(harmony, typeof(EmotionalList), "OnClickCard", nameof(ListClick), false);
            installed += Patch(harmony, typeof(EmotionalList), "SelectIndex", nameof(ListSelect), false);
            installed += Patch(harmony, typeof(EmotionalList), "OnDecide", nameof(ListDecide), false);
            installed += Patch(harmony, typeof(Engine), "DLL_DuelComDoCommand", nameof(EngineCommand), false);
            installed += Patch(harmony, typeof(Engine), "DLL_DuelListSetIndex", nameof(EngineListIndex), false);
            Write($"READY observers={installed}/10 build=R6-native-summon-list-entry");
        }

        private static int Patch(HarmonyLib.Harmony harmony, Type type, string name, string observer, bool after)
        {
            try
            {
                var target = AccessTools.Method(type, name);
                if (target == null) { Write($"UNAVAILABLE {type.Name}.{name}"); return 0; }
                if (HarmonyPatchPolicy.IsOwned(HarmonyLib.Harmony.GetPatchInfo(target)?.Owners, harmony.Id)) return 1;
                var handler = typeof(SummonDiagnostics).GetMethod(observer, BindingFlags.NonPublic | BindingFlags.Static);
                if (after) harmony.Patch(target, postfix: new HarmonyMethod(handler));
                else harmony.Patch(target, prefix: new HarmonyMethod(handler));
                Write($"INSTALLED {target}");
                return 1;
            }
            catch (Exception ex) { Write($"UNAVAILABLE {type.Name}.{name}: {ex.Message}"); return 0; }
        }

        private static void Write(string text)
        {
            try { MelonLogger.Msg($"[SummonTrace] seq={++_sequence} frame={UnityEngine.Time.frameCount} {text}"); }
            catch { /* Logging failure must not change gameplay. */ }
        }

        private static string Origin()
        {
            try
            {
                foreach (var frame in new StackTrace().GetFrames())
                    if (frame.GetMethod()?.DeclaringType == typeof(DuelFieldNavigator))
                        return "mod-keyboard-stack";
            }
            catch { }
            // Native IL2CPP callbacks do not preserve their managed caller stack.
            return "UNKNOWN-use-test-order";
        }

        private static string Target(int player, int position, int index)
        {
            try
            {
                if (player != DuelState.MyPlayerNum()) return "identity=not-local-not-recorded";
                var card = position == 13 ? DuelState.GetHandCard(player, index)
                    : DuelState.GetFieldCard(player, position, index);
                return card.HasValue ? $"adapterCid={card.Value.CardDbId} adapterUid={card.Value.UniqueId}"
                    : "identity=UNKNOWN";
            }
            catch { return "identity=UNKNOWN"; }
        }

        private static void Tap(int __0, int __1, int __2)
            => Write($"OnTapLocator player={__0} position={__1} index={__2} {Target(__0,__1,__2)} origin={Origin()}");

        private static void Command(int __0, int __1, int __2, Engine.CommandType __3)
            => Write($"OnDoCardCommand player={__0} position={__1} index={__2} cmd={__3} " +
                $"{Target(__0,__1,__2)} origin={Origin()}");

        private static void EngineCommand(int __0, int __1, int __2, int __3)
            => Write($"DLL_DuelComDoCommand player={__0} position={__1} index={__2} commandId={__3} origin={Origin()}");

        private static void EngineListIndex(int __0)
            => Write($"DLL_DuelListSetIndex index={__0} origin={Origin()}");

        private static void PopupOpen(int __0, int __2, int __3, int __4, int __5)
            => Write($"CardCommand.Open uid={__0} player={__2} position={__3} index={__4} cardIndex={__5} origin={Origin()}");

        private static void PopupCommand(CardCommand __instance, DuelCommandButton __0)
        {
            try
            {
                Write($"CardCommand.OnCommand player={__instance.cmdPlayer} position={__instance.cmdPosition} " +
                    $"index={__instance.cmdIndex} cmd={__0?.cmd} origin={Origin()}");
            }
            catch (Exception ex) { Write($"PopupCommand unavailable: {ex.Message}"); }
        }

        private static void ListOpen(EmotionalList __instance) => ListState("EmotionalList.Open", __instance);
        private static void ListDecide(EmotionalList __instance) => ListState("EmotionalList.OnDecide", __instance);
        private static void ListSelect(EmotionalList __instance, int __0)
        {
            Write($"EmotionalList.SelectIndex dataIndex={__0} origin={Origin()}");
            ListState("BeforeSelect", __instance);
        }

        private static void ListClick(EmotionalList __instance, ListCard __0)
        {
            try
            {
                Write($"EmotionalList.OnClickCard dataIndex={__0?.Index} listCardUid={__0?.UniqueID} origin={Origin()}");
                ListState("BeforeClick", __instance);
            }
            catch (Exception ex) { Write($"ListClick unavailable: {ex.Message}"); }
        }

        private static void ListState(string phase, EmotionalList list)
        {
            try
            {
                if (list == null) return;
                var selected = new List<string>();
                if (list.selectedList != null)
                    for (int i = 0; i < Math.Min(16, list.selectedList.Count); i++)
                        selected.Add(list.selectedList[i].ToString());
                Write($"{phase} type={list.listType} min={list.selectMinNum} max={list.selectMaxNum} " +
                    $"items={list.itemList?.Count} visuals={list.cardList?.Count} selected=[{string.Join(",",selected)}] " +
                    $"decideActive={list.decideButton?.gameObject?.activeInHierarchy} decideEnabled={list.decideButton?.interactable}");
                if (list.cardList == null) return;
                for (int i = 0; i < Math.Min(16, list.cardList.Count); i++)
                {
                    var card = list.cardList[i];
                    if (card == null) continue;
                    int index = card.Index;
                    var items = list.itemList;
                    if (items == null || index < 0 || index >= items.Count || items[index] == null) continue;
                    var item = items[index];
                    // Opaque selection IDs only; do not dump hidden card identities.
                    Write($"ListMap visual={i} dataIndex={index} listCardUid={card.UniqueID} " +
                        $"itemUid={item.uniqueId} mixedId={item.mixedId}");
                }
            }
            catch (Exception ex) { Write($"ListState unavailable: {ex.Message}"); }
        }

        public static void ObserveEffect(int id, int p1, int p2, int p3)
        {
            try
            {
                var type = (Engine.ViewType)id;
                if (type == Engine.ViewType.DuelStart || type == Engine.ViewType.DuelEnd)
                { _fieldReadsRemaining = 0; Write($"EVENT {type}"); return; }
                if (type != Engine.ViewType.RunSpSummon && type != Engine.ViewType.RunSummon
                    && type != Engine.ViewType.RunCommand && type != Engine.ViewType.RunList) return;
                Write($"EVENT {type} id={id} p1={p1} p2={p2} p3={p3}");
                if (type == Engine.ViewType.RunSpSummon || type == Engine.ViewType.RunSummon)
                { _fieldReadsRemaining = 3; _nextFieldRead = UnityEngine.Time.unscaledTime + 0.5f; }
            }
            catch { }
        }

        public static void Update()
        {
            if (_fieldReadsRemaining <= 0 || UnityEngine.Time.unscaledTime < _nextFieldRead) return;
            _fieldReadsRemaining--;
            _nextFieldRead = UnityEngine.Time.unscaledTime + 1.5f;
            if (!DuelState.InDuel || DuelState.DuelEnded) { _fieldReadsRemaining = 0; return; }
            foreach (int locate in new[] { 1, 2, 3, 5, 6 })
                Write($"PostSummonField player={DuelState.MyPlayerNum()} position={locate} " +
                    Target(DuelState.MyPlayerNum(), locate, 0));
        }
    }
}
