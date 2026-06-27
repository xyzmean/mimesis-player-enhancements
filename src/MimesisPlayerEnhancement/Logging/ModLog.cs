using MelonLoader;

namespace MimesisPlayerEnhancement;

/// <summary>
/// Central logging. Use <see cref="Info"/> for normal operational feedback;
/// <see cref="Debug"/> only emits when <see cref="ModConfig.EnableDebugLogging"/> is true.
/// </summary>
public static class ModLog
{
    public static void Info(string feature, string message) =>
        MelonLogger.Msg($"[{feature}] {message}");

    public static void Warn(string feature, string message) =>
        MelonLogger.Warning($"[{feature}] {message}");

    public static void Error(string feature, string message) =>
        MelonLogger.Error($"[{feature}] {message}");

    public static void Debug(string feature, string message)
    {
        if (ModConfig.EnableDebugLogging.Value)
            MelonLogger.Msg($"[{feature}:debug] {message}");
    }
}
