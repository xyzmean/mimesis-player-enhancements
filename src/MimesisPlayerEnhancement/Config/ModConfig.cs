using System.IO;
using MelonLoader;
using MelonLoader.Utils;

namespace MimesisPlayerEnhancement;

/// <summary>
/// MelonPreferences-backed configuration. Values are stored in
/// UserData/MimesisPlayerEnhancement.cfg (separate from the global MelonPreferences.cfg).
/// </summary>
public static class ModConfig
{
    private const string CategoryId = "MimesisPlayerEnhancement";

    public static MelonPreferences_Category Category { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableMorePlayers { get; private set; } = null!;
    public static MelonPreferences_Entry<int> MaxPlayers { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableMoreVoices { get; private set; } = null!;
    public static MelonPreferences_Entry<int> MaxVoiceEvents { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnablePersistence { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableDebugLogging { get; private set; } = null!;

    public static void Initialize(MelonLogger.Instance logger)
    {
        Category = MelonPreferences.CreateCategory(CategoryId, "Mimesis Player Enhancement");
        Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "MimesisPlayerEnhancement.cfg"));

        EnableMorePlayers = Category.CreateEntry(
            "EnableMorePlayers",
            true,
            "Enable More Players",
            "Raise the multiplayer player cap above 4.");

        MaxPlayers = Category.CreateEntry(
            "MaxPlayers",
            999,
            "Max Players",
            "Maximum players allowed in a session when More Players is enabled.");

        EnableMoreVoices = Category.CreateEntry(
            "EnableMoreVoices",
            true,
            "Enable More Voices",
            "Raise per-player voice recording limits.");

        MaxVoiceEvents = Category.CreateEntry(
            "MaxVoiceEvents",
            3000,
            "Max Voice Events",
            "Maximum stored voice events per player (default game limit is much lower).");

        EnablePersistence = Category.CreateEntry(
            "EnablePersistence",
            true,
            "Enable Voice Persistence",
            "Save and restore mimic voice recordings across save/load.");

        EnableDebugLogging = Category.CreateEntry(
            "EnableDebugLogging",
            false,
            "Enable Debug Logging",
            "Emit verbose diagnostic lines to the MelonLoader console.");

        MaxPlayers.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 2)
            {
                logger.Warning("MaxPlayers must be at least 2; resetting to 2.");
                MaxPlayers.Value = 2;
            }
        });

        MaxVoiceEvents.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("MaxVoiceEvents must be at least 1; resetting to 1.");
                MaxVoiceEvents.Value = 1;
            }
        });
    }
}
