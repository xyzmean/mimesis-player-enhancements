using System.Linq;
using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.Persistence;

public static class PersistencePatches
{
    private const string Feature = "Persistence";

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        var patchNamespace = typeof(PersistencePatches).Namespace + ".Patches";
        var patchTypes = typeof(PersistencePatches).Assembly
            .GetTypes()
            .Where(t => t.Namespace == patchNamespace && t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);

        foreach (var type in patchTypes)
            harmony.CreateClassProcessor(type).Patch();

        ModLog.Info(Feature, "Patches applied.");
    }
}
