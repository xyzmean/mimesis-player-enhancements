namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonPickRollContext
    {
        internal static bool InRollDiceDungeon { get; private set; }

        internal static bool IsReroll { get; private set; }

        internal static int PickIndex { get; private set; }

        internal static void BeginRoll(bool reroll)
        {
            InRollDiceDungeon = true;
            IsReroll = reroll;
            PickIndex = 0;
        }

        internal static void EndRoll()
        {
            InRollDiceDungeon = false;
            IsReroll = false;
            PickIndex = 0;
        }

        internal static void AdvancePick()
        {
            PickIndex++;
        }

        internal static bool ShouldClearRerollExcludes()
        {
            return InRollDiceDungeon && IsReroll && PickIndex == 0 && DungeonPickResolver.ShouldIgnoreDungeonExcludeList();
        }
    }
}
