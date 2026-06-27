using System.Collections.Generic;
using HarmonyLib;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))]
    public static class MaintenanceRoomPatches
    {
        private const string Feature = "Persistence";

        [HarmonyPostfix]
        public static void Postfix(int saveSlotID, List<string> playerNames, bool isAutoSave, MsgErrorCode __result)
        {
            if (__result != MsgErrorCode.Success)
                return;
            if (!MimesisSaveManager.IsHost())
                return;

            int slotId = isAutoSave ? 0 : saveSlotID;
            ModLog.Info(Feature, $"Game save triggered — persisting mimic voices for slot {slotId}.");
            MimesisSaveManager.SaveMimesisData(slotId);
        }
    }
}
