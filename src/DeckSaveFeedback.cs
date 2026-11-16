using System;
using HarmonyLib;
using UnityEngine;
using Il2CppYgomGame.Deck;

namespace DuelLinksAccess
{
    internal static class DeckSaveFeedback
    {
        private static readonly DeckSaveState State = new();
        private static bool _registered;
        internal static void Register(HarmonyLib.Harmony harmony)
        {
            if (_registered) return;
            try
            {
                var request = AccessTools.Method(typeof(DeckEdit2ViewController), "saveCommon", new[] { typeof(DeckEdit2ViewController.SAVEFOR) });
                var complete = AccessTools.Method(typeof(DeckEdit2ViewController), "onCompleteUpdateDeck", Type.EmptyTypes);
                if (request == null || complete == null) return;
                harmony.Patch(request, prefix: new HarmonyMethod(typeof(DeckSaveFeedback), nameof(Requested)));
                harmony.Patch(complete, postfix: new HarmonyMethod(typeof(DeckSaveFeedback), nameof(Completed)));
                _registered = true;
                DebugLogger.Log(LogCategory.Handler, "DeckSave", "Native save observers registered");
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "DeckSave", ex.ToString()); }
        }
        private static void Requested(DeckEdit2ViewController __instance)
        {
            try
            {
                bool changed = __instance.saveNeeded();
                State.Begin(__instance.Pointer.ToInt64(), Time.unscaledTime, changed);
                DebugLogger.Log(LogCategory.Handler, "DeckSave", $"Native save requested; changes={changed}");
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "DeckSave", ex.Message); }
        }
        private static void Completed(DeckEdit2ViewController __instance)
        {
            try
            {
                bool remaining = __instance.saveNeeded();
                bool confirmed = State.Complete(__instance.Pointer.ToInt64(), remaining);
                DebugLogger.Log(LogCategory.Handler, "DeckSave", $"Native update completed; changesRemain={remaining} confirmed={confirmed}");
                if (confirmed) ScreenReader.Say(Loc.Get("deck_saved"));
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "DeckSave", ex.Message); }
        }
        internal static void Update()
        {
            if (State.Expired(Time.unscaledTime))
                ScreenReader.Say(Loc.Get("deck_save_unconfirmed"));
        }
    }
}
