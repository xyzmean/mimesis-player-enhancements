using HarmonyLib;
using MimesisPlayerEnhancement.Features.Persistence;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches;

[HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.OnRegistPlayer))]
public static class VRoomManagerRegisterPatches
{
    [HarmonyPostfix]
    public static void Postfix(ulong steamID)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;
        if (!MimesisSaveManager.TryGetActiveSaveSlotId(out int slotId))
            return;

        StatisticsTracker.OnPlayerRegistered(steamID, slotId);
    }
}
