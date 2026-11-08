using System;
using HarmonyLib;
using UnityEngine;

namespace ShadowsOfDoubtCoop.Patches
{
    [HarmonyPatch(typeof(SessionData), nameof(SessionData.PauseGame))]
    public static class PauseGamePatch
    {
        public static bool Prefix(bool showPauseText, bool delayOverride, bool openDesktopMode)
        {
            bool isCoop = CoopNetworkManager.Instance != null && CoopNetworkManager.Instance.IsConnected;
            Plugin.Log.LogInfo($"[Pause] Intercepted: PauseGame(showText={showPauseText}, desktop={openDesktopMode}) -> Suppress: {isCoop}");

            if (isCoop)
            {
                // In co-op, prevent gameplay pause simulation halt
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(NewDoor), nameof(NewDoor.SetOpen))]
    public static class DoorOpenPatch
    {
        public static void Postfix(NewDoor __instance, float newAjar, Actor actor, bool skipAnimation, float speedMultiplier)
        {
            if (CoopNetworkManager.IsApplyingRemote) return;

            int wallId = (__instance.wall != null) ? __instance.wall.id : 0;
            bool isConnected = CoopNetworkManager.Instance != null && CoopNetworkManager.Instance.IsConnected;
            Plugin.Log.LogInfo($"[Doors] Intercepted: SetOpen(wallId={wallId}, ajar={newAjar:F2}) -> Net: {(isConnected ? "BROADCAST" : "STANDBY")}");

            if (isConnected)
            {
                CoopNetworkManager.Instance!.SendDoorState(wallId, newAjar > 0.1f, __instance.isLocked);
            }
        }
    }

    [HarmonyPatch(typeof(NewDoor), nameof(NewDoor.SetLocked))]
    public static class DoorLockPatch
    {
        public static void Postfix(NewDoor __instance, bool val, Actor actor, bool playSound)
        {
            if (CoopNetworkManager.IsApplyingRemote) return;

            int wallId = (__instance.wall != null) ? __instance.wall.id : 0;
            bool isConnected = CoopNetworkManager.Instance != null && CoopNetworkManager.Instance.IsConnected;
            Plugin.Log.LogInfo($"[Doors] Intercepted: SetLocked(wallId={wallId}, val={val}) -> Net: {(isConnected ? "BROADCAST" : "STANDBY")}");

            if (isConnected)
            {
                CoopNetworkManager.Instance!.SendDoorState(wallId, !__instance.isClosed, val);
            }
        }
    }

    [HarmonyPatch(typeof(MurderController), nameof(MurderController.Update))]
    public static class MurderControllerPatch
    {
        public static bool Prefix()
        {
            // On Guest client, suppress autonomous murder generation; Host remains authoritative
            if (CoopNetworkManager.Instance != null && CoopNetworkManager.Instance.IsConnected && !CoopNetworkManager.Instance.IsHost)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CasePanelController), nameof(CasePanelController.PinToCasePanel), new Type[] { typeof(Case), typeof(Evidence), typeof(Evidence.DataKey), typeof(bool), typeof(Vector2), typeof(bool) })]
    public static class CasePinPatch
    {
        public static void Postfix(Case toCase, Evidence ev, Evidence.DataKey evKey, bool forceAutoPin, Vector2 localPostion, bool debugFlag)
        {
            if (CoopNetworkManager.IsApplyingRemote) return;

            int caseId = (toCase != null) ? toCase.id : -1;
            string evId = (ev != null) ? ev.evID : "";
            bool isConnected = CoopNetworkManager.Instance != null && CoopNetworkManager.Instance.IsConnected;
            Plugin.Log.LogInfo($"[CaseBoard] Intercepted: PinToCasePanel(caseId={caseId}, evId={evId}, pos=({localPostion.x:F1}, {localPostion.y:F1})) -> Net: {(isConnected ? "BROADCAST" : "STANDBY")}");

            if (isConnected && !string.IsNullOrEmpty(evId))
            {
                CoopNetworkManager.Instance!.SendCasePin(caseId, evId, localPostion.x, localPostion.y, true);
            }
        }
    }

    [HarmonyPatch(typeof(CasePanelController), nameof(CasePanelController.UnPinFromCasePanel))]
    public static class CaseUnpinPatch
    {
        public static void Postfix(Case thisCase, Evidence ev)
        {
            if (CoopNetworkManager.IsApplyingRemote) return;

            int caseId = (thisCase != null) ? thisCase.id : -1;
            string evId = (ev != null) ? ev.evID : "";
            bool isConnected = CoopNetworkManager.Instance != null && CoopNetworkManager.Instance.IsConnected;
            Plugin.Log.LogInfo($"[CaseBoard] Intercepted: UnPinFromCasePanel(caseId={caseId}, evId={evId}) -> Net: {(isConnected ? "BROADCAST" : "STANDBY")}");

            if (isConnected && !string.IsNullOrEmpty(evId))
            {
                CoopNetworkManager.Instance!.SendCasePin(caseId, evId, 0f, 0f, false);
            }
        }
    }
}
