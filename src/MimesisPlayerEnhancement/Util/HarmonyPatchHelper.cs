using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace MimesisPlayerEnhancement.Util;

public static class HarmonyPatchHelper
{
    public struct PatchApplyResult
    {
        public int Applied;
        public int Failed;
    }

    public static IEnumerable<Type> GetNestedPatchTypes(Type containerType) =>
        containerType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);

    public static IEnumerable<Type> GetNamespacePatchTypes(Type anchorType, string suffix = ".Patches") =>
        anchorType.Assembly.GetTypes()
            .Where(t => t.Namespace == anchorType.Namespace + suffix
                        && t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);

    public static PatchApplyResult ApplyPatchTypes(HarmonyLib.Harmony harmony, string feature, IEnumerable<Type> patchTypes)
    {
        int applied = 0;
        int failed = 0;

        foreach (var patchType in patchTypes)
        {
            try
            {
                var results = harmony.CreateClassProcessor(patchType).Patch();
                if (results != null && results.Count > 0)
                {
                    applied += results.Count;
                }
                else
                {
                    failed++;
                    if (patchType.GetMethod("TargetMethod", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null)
                        ModLog.Warn(feature, $"Patch {patchType.Name} — TargetMethod returned no patches");
                    else
                        ModLog.Warn(feature, $"Patch class {patchType.Name} did not apply to any methods.");
                }
            }
            catch (Exception ex)
            {
                failed++;
                ModLog.Warn(feature, $"Patch {patchType.Name} failed — {ex.Message}");
            }
        }

        return new PatchApplyResult { Applied = applied, Failed = failed };
    }

    public static void LogPatchSummary(string feature, PatchApplyResult result) =>
        ModLog.Info(feature, $"Patches applied — {result.Applied} patch(es), {result.Failed} failure(s).");

    public static void LogPatchAudit(string feature, HarmonyLib.Harmony harmony, IEnumerable<(string label, MethodBase? method)> checks)
    {
        var patched = harmony.GetPatchedMethods().ToList();
        var applied = new List<string>();
        var missing = new List<string>();

        foreach (var (label, method) in checks)
        {
            if (method == null)
                missing.Add($"{label} (type/method not found)");
            else if (IsPatched(patched, method))
                applied.Add(label);
            else
                missing.Add(label);
        }

        if (applied.Count > 0)
            ModLog.Debug(feature, $"Patch audit — applied: {string.Join(", ", applied)}");

        foreach (string label in missing)
            ModLog.Warn(feature, $"Patch audit — not applied: {label}");
    }

    public static bool IsPatched(IReadOnlyCollection<MethodBase> patched, MethodBase? expected)
    {
        if (expected == null)
            return false;

        foreach (var candidate in patched)
        {
            if (candidate.MetadataToken == expected.MetadataToken && ReferenceEquals(candidate.Module, expected.Module))
                return true;

            if (candidate.Name != expected.Name)
                continue;

            if (candidate.DeclaringType != expected.DeclaringType)
                continue;

            var candidateParams = candidate.GetParameters();
            var expectedParams = expected.GetParameters();
            if (candidateParams.Length != expectedParams.Length)
                continue;

            bool paramsMatch = true;
            for (int i = 0; i < candidateParams.Length; i++)
            {
                if (candidateParams[i].ParameterType != expectedParams[i].ParameterType)
                {
                    paramsMatch = false;
                    break;
                }
            }

            if (paramsMatch)
                return true;
        }

        return false;
    }
}
