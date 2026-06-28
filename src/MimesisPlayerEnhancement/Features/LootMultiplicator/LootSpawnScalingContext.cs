using System;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator;

/// <summary>
/// Guards against recursive scaling when duplicating non-stackable loot piles.
/// </summary>
internal static class LootSpawnScalingContext
{
    [ThreadStatic]
    private static int _duplicateDepth;

    internal static bool IsDuplicating => _duplicateDepth > 0;

    internal static void BeginDuplicating() => _duplicateDepth++;

    internal static void EndDuplicating() => _duplicateDepth = Math.Max(0, _duplicateDepth - 1);
}
