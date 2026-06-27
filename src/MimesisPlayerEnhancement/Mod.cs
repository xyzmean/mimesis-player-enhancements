using MelonLoader;

[assembly: MelonInfo(typeof(MimesisPlayerEnhancement.Mod), "MimesisPlayerEnhancement", MimesisPlayerEnhancement.VersionInfo.ModuleVersion, "kalle")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]
[assembly: HarmonyDontPatchAll]

namespace MimesisPlayerEnhancement;

public sealed class Mod : MelonMod
{
    private HarmonyLib.Harmony? _harmony;

    public override void OnInitializeMelon()
    {
        ModConfig.Initialize(LoggerInstance);

        _harmony = new HarmonyLib.Harmony("com.mimesis.playerenhancement");

        if (ModConfig.EnableMorePlayers.Value)
            Features.MorePlayers.MorePlayersPatches.Apply(_harmony);

        if (ModConfig.EnableMoreVoices.Value)
            Features.MoreVoices.MoreVoicesPatches.Apply(_harmony);

        if (ModConfig.EnablePersistence.Value)
            Features.Persistence.PersistencePatches.Apply(_harmony);

        LoggerInstance.Msg(
            $"MimesisPlayerEnhancement v{VersionInfo.ModuleVersion} loaded — " +
            $"MorePlayers={ModConfig.EnableMorePlayers.Value}, " +
            $"MoreVoices={ModConfig.EnableMoreVoices.Value} (max {ModConfig.MaxVoiceEvents.Value}), " +
            $"Persistence={ModConfig.EnablePersistence.Value}, " +
            $"DebugLogging={ModConfig.EnableDebugLogging.Value}");

        if (ModConfig.EnableMorePlayers.Value)
            ModLog.Info("MorePlayers", $"Enabled — session player cap raised to {ModConfig.MaxPlayers.Value}.");

        if (ModConfig.EnableMoreVoices.Value)
            ModLog.Info("MoreVoices", $"Enabled — per-player voice event cap set to {ModConfig.MaxVoiceEvents.Value}.");

        if (ModConfig.EnablePersistence.Value)
            ModLog.Info("Persistence", "Enabled — mimic voices will be saved with game saves.");
    }

    public override void OnUpdate()
    {
        if (ModConfig.EnablePersistence.Value)
            Features.Persistence.SpeechEventPoolManager.ProcessDeferredUpdates();
    }

    public override void OnDeinitializeMelon()
    {
        _harmony?.UnpatchSelf();
    }
}
