using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomGame.Menu;

namespace DuelLinksAccess
{
    /// <summary>Native row bindings, captured after the game populates each portrait.
    /// No cell identity is inferred from hierarchy order or scroll position.</summary>
    internal static class CharacterPortraitBindings
    {
        private sealed class RowBinding
        {
            public CharaFilterRow Row;
            public int DialogId;
            public int[] CharacterIds;
            public int[] ButtonIds;
        }

        private static readonly Dictionary<int, RowBinding> Rows = new();
        private static bool _registered;

        public static void Register(HarmonyLib.Harmony harmony)
        {
            if (_registered) return;
            try
            {
                var target = AccessTools.Method(typeof(CharaFilterRow), "SetImage");
                if (target == null) throw new MissingMethodException("CharaFilterRow.SetImage");
                var observer = typeof(CharacterPortraitBindings).GetMethod(nameof(Capture),
                    BindingFlags.NonPublic | BindingFlags.Static);
                harmony.Patch(target, postfix: new HarmonyMethod(observer));
                _registered = true;
                MelonLogger.Msg("[CharacterPortrait] R10 native SetImage observer installed");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[CharacterPortrait] binding unavailable: {ex.Message}");
            }
        }

        // SetImage(imageIds, selFlag, selectBase, listener, checkMarkFunc, cfd, grayedList).
        // Observe only: neither arguments nor the game's selected state are rewritten.
        private static void Capture(CharaFilterRow __instance,
            Il2CppSystem.Collections.Generic.List<int> __0, CharaFilterDialogViewController __5)
        {
            try
            {
                if (__instance == null) return;
                int rowId = __instance.GetInstanceID();
                Rows.Remove(rowId); // Recycled rows must discard their former identities.
                if (__0 == null || __5 == null || !(__5.isCharaSelect || __5.isGate)) return;
                var buttons = __instance.buttons;
                if (buttons == null) return;
                var ids = new int[__0.Count];
                var buttonIds = new int[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    ids[i] = __0[i];
                    buttonIds[i] = i < buttons.Length && buttons[i] != null
                        ? buttons[i].GetInstanceID() : -1;
                }
                var dead = new List<int>();
                foreach (var pair in Rows)
                    if (pair.Value.Row == null || pair.Value.Row.gameObject == null)
                        dead.Add(pair.Key);
                foreach (int key in dead) Rows.Remove(key);
                if (Rows.Count >= 256) Rows.Clear();
                Rows[rowId] = new RowBinding
                {
                    Row = __instance, DialogId = __5.GetInstanceID(),
                    CharacterIds = ids, ButtonIds = buttonIds
                };
                MelonLogger.Msg($"[CharacterPortrait] bind dialog={__5.GetInstanceID()} row={rowId} cids=[{string.Join(",", ids)}]");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[CharacterPortrait] capture failed: {ex.Message}");
            }
        }

        private static bool TryClick(CharaFilterDialogViewController vc, int cid)
        {
            // A native click can repopulate rows synchronously, so iterate a snapshot.
            foreach (var row in new List<RowBinding>(Rows.Values))
            {
                if (row.DialogId != vc.GetInstanceID() || row.Row == null
                    || !row.Row.gameObject.activeInHierarchy) continue;
                int column = CharacterChoicePolicy.FindPortraitIndex(cid, row.CharacterIds);
                var buttons = row.Row.buttons;
                if (column < 0 || buttons == null || column >= buttons.Length) continue;
                var button = buttons[column];
                if (button == null || !button.gameObject.activeInHierarchy
                    || button.GetInstanceID() != row.ButtonIds[column] || !button.IsInteractable()) continue;
                MelonLogger.Msg($"[CharacterPortrait] native click cid={cid} row={row.Row.GetInstanceID()} column={column} button={button.GetInstanceID()}");
                // Runs the SetImage-installed checkAction callback: native selection
                // frame placement and OnClickIcon both occur in the same game path.
                button.onClick.Invoke();
                try { Reveal(vc.scrollRect, button.gameObject); }
                catch (Exception ex)
                {
                    // A layout failure must not turn an already-dispatched click
                    // into a second click or lose the game's selected identity.
                    MelonLogger.Warning($"[CharacterPortrait] reveal failed: {ex.Message}");
                }
                return true;
            }
            return false;
        }

        private static void Reveal(ScrollRect scroll, GameObject target)
        {
            if (scroll == null || scroll.content == null || scroll.viewport == null || target == null) return;
            Canvas.ForceUpdateCanvases();
            var viewport = scroll.viewport;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target.transform);
            if (bounds.min.y >= viewport.rect.yMin && bounds.max.y <= viewport.rect.yMax) return;
            float travel = scroll.content.rect.height - viewport.rect.height;
            if (travel <= 0f) return;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition
                + (bounds.center.y - viewport.rect.center.y) / travel);
        }

        public static IEnumerator Select(CharaFilterDialogViewController vc, int cid,
            Func<bool> isCurrentDialog, Action<bool> finished)
        {
            bool clicked = false;
            float originalScroll = 1f;
            try
            {
                if (vc.scrollRect != null) originalScroll = vc.scrollRect.verticalNormalizedPosition;
                for (int step = 0; step <= 7 && isCurrentDialog(); step++)
                {
                    if (TryClick(vc, cid)) { clicked = true; break; }
                    if (step == 7 || vc.scrollRect == null) break;
                    vc.scrollRect.StopMovement();
                    // Search only to instantiate native rows; identity always comes
                    // from Capture, never from this estimated scroll position.
                    vc.scrollRect.verticalNormalizedPosition = 1f - step / 6f;
                    yield return null;
                    yield return null;
                }
                if (!clicked && isCurrentDialog() && vc.scrollRect != null)
                    vc.scrollRect.verticalNormalizedPosition = originalScroll;
            }
            finally
            {
                MelonLogger.Msg($"[CharacterPortrait] selection cid={cid} clicked={clicked}");
                finished(clicked && isCurrentDialog());
            }
        }
    }
}
