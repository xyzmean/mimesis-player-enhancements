using System.Collections.Generic;
using ReluProtocol;
using Steamworks;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotRowContext
    {
        internal int SlotId { get; set; }
        internal bool IsEmpty { get; set; }
        internal SaveSlotEntry? Entry { get; set; }
    }

    internal static class SaveSlotRoomListMapper
    {
        internal static CSteamID ToRowKey(int slotId) => new((ulong)slotId);

        internal static int FromRowKey(CSteamID rowKey) => (int)rowKey.m_SteamID;

        internal static List<PublicRoomListData> BuildRoomListData(
            out Dictionary<CSteamID, SaveSlotRowContext> rowContexts)
        {
            rowContexts = new Dictionary<CSteamID, SaveSlotRowContext>();
            List<PublicRoomListData> rows = [];

            SaveSlotEntry? autosave = SaveSlotDiscovery.TryLoadAutosave();
            if (autosave != null)
            {
                PublicRoomListData row = ToOccupiedRow(autosave);
                rows.Add(row);
                rowContexts[row.lobbyID] = new SaveSlotRowContext
                {
                    SlotId = autosave.SlotId,
                    IsEmpty = false,
                    Entry = autosave,
                };
            }

            foreach (SaveSlotEntry entry in SaveSlotDiscovery.GetManualSaves())
            {
                PublicRoomListData row = ToOccupiedRow(entry);
                rows.Add(row);
                rowContexts[row.lobbyID] = new SaveSlotRowContext
                {
                    SlotId = entry.SlotId,
                    IsEmpty = false,
                    Entry = entry,
                };
            }

            int maxManual = SaveSlotDiscovery.GetMaxManualSlots();
            for (int slotId = SaveSlotLimits.MinManualSlotId; slotId <= maxManual; slotId++)
            {
                if (SaveSlotDiscovery.IsManualSlotOccupied(slotId))
                {
                    continue;
                }

                PublicRoomListData row = ToEmptySlotRow(slotId);
                rows.Add(row);
                rowContexts[row.lobbyID] = new SaveSlotRowContext
                {
                    SlotId = slotId,
                    IsEmpty = true,
                    Entry = null,
                };
            }

            return rows;
        }

        internal static PublicRoomListData ToOccupiedRow(SaveSlotEntry entry)
        {
            MMSaveGameData save = entry.Data;
            int cycle = save.StageCount;
            int repairStatus = cycle == 1
                ? 0
                : save.TramRepaired ? 2 : 1;

            string lobbyName = entry.SlotId == SaveSlotLimits.AutosaveSlotId
                ? SaveSlotDisplayFormatter.FormatAutosaveTitle(save) + " — " + entry.Display.BaseText
                : entry.Display.BaseText;

            return new PublicRoomListData
            {
                lobbyID = ToRowKey(entry.SlotId),
                locale = string.Empty,
                lobbyName = lobbyName,
                cycle = cycle,
                repairStatus = repairStatus,
                PlayerCount = save.PlayerNames?.Count ?? 0,
                password = string.Empty,
            };
        }

        internal static PublicRoomListData ToEmptySlotRow(int slotId)
        {
            return new PublicRoomListData
            {
                lobbyID = ToRowKey(slotId),
                locale = string.Empty,
                lobbyName = SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_NEW_TRAM"),
                cycle = 0,
                repairStatus = 0,
                PlayerCount = 0,
                password = string.Empty,
            };
        }
    }
}
