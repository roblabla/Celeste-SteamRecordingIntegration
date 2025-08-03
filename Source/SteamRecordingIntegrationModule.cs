using System;
using System.Diagnostics;
using System.Text;

namespace Celeste.Mod.SteamRecordingIntegration;

public class SteamRecordingIntegrationModule : EverestModule {
    public static SteamRecordingIntegrationModule Instance { get; private set; }

    public override Type SettingsType => typeof(SteamRecordingIntegrationModuleSettings);
    public static SteamRecordingIntegrationModuleSettings Settings => (SteamRecordingIntegrationModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(SteamRecordingIntegrationModuleSession);
    public static SteamRecordingIntegrationModuleSession Session => (SteamRecordingIntegrationModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(SteamRecordingIntegrationModuleSaveData);
    public static SteamRecordingIntegrationModuleSaveData SaveData => (SteamRecordingIntegrationModuleSaveData) Instance._SaveData;

    private long areaStartTime = Stopwatch.GetTimestamp();
    private long levelStartTime = Stopwatch.GetTimestamp();
    private long lastSpawnTime = Stopwatch.GetTimestamp();

    public SteamRecordingIntegrationModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(SteamRecordingIntegrationModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(SteamRecordingIntegrationModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        Steamworks.SteamTimeline.SetTimelineGameMode(Steamworks.ETimelineGameMode.k_ETimelineGameMode_Menus);
        Everest.Events.Player.OnSpawn += Player_OnSpawn;
        Everest.Events.Player.OnDie += Player_OnDie;
        Everest.Events.Level.OnEnter += Level_OnEnter;
        Everest.Events.Level.OnTransitionTo += Level_OnTransitionTo;
        Everest.Events.Level.OnExit += Level_OnExit;
    }

    private void Player_OnSpawn(Player obj)
    {
        lastSpawnTime = Stopwatch.GetTimestamp();
    }

    private void Player_OnDie(Player obj)
    {
        // Player died, let's show this in the steamworks timeline if the last death was a long time ago.
        float lastAttemptTime = (float)Stopwatch.GetElapsedTime(this.lastSpawnTime).TotalSeconds;

        // To avoid making a huge mess of the timeline, let's only add a death marker if the last death was more than 10 seconds ago.
        if (lastAttemptTime > 10)
        {
            Steamworks.SteamTimeline.AddTimelineEvent("steam_death", "Player Death", "The player has died", 0, 0, 0, Steamworks.ETimelineEventClipPriority.k_ETimelineEventClipPriority_None);
        }
    }

    // We enter a new Area.
    private void Level_OnEnter(Session session, bool fromSaveData)
    {
        // First, set the level name and gamemode to playing.
        string levelName = AreaData.Get(session.Area).Name + "/" + session.Level;
        Steamworks.SteamTimeline.SetTimelineStateDescription(levelName, 0);
        Steamworks.SteamTimeline.SetTimelineGameMode(Steamworks.ETimelineGameMode.k_ETimelineGameMode_Playing);

        // Next, set all the stopwatches.
        areaStartTime = Stopwatch.GetTimestamp();
        levelStartTime = Stopwatch.GetTimestamp();
        lastSpawnTime = Stopwatch.GetTimestamp();
    }

    // We transition from one level to the next within an area.
    private void Level_OnTransitionTo(Level level, LevelData next, Microsoft.Xna.Framework.Vector2 direction)
    {
        // Change the level name to fit the one we transitioned to.
        string nextLevelName = AreaData.Get(level.Session.Area).Name + "/" + next.Name;
        Steamworks.SteamTimeline.SetTimelineStateDescription(next.Name, 0);

        // Create an event for completing the previous level.
        string levelName = AreaData.Get(level.Session.Area).Name + "/" + level.Session.Level;
        float lastAttemptTime = (float)Stopwatch.GetElapsedTime(this.lastSpawnTime).TotalSeconds;
        this.MakeRangedEvent("steam_flag", "Screen Completed", "Screen completed", 1, lastAttemptTime, Steamworks.ETimelineEventClipPriority.k_ETimelineEventClipPriority_Standard);

        // Set all the relevant stopwatches.
        levelStartTime = Stopwatch.GetTimestamp();
        lastSpawnTime = Stopwatch.GetTimestamp();
    }

    // We end an Area, either because we completed it or because we died/saved and quitted/restarted.
    private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
    {
        switch (mode)
        {
            case LevelExit.Mode.Completed:
            case LevelExit.Mode.CompletedInterlude:
                // If the area is completed, make an event for it.
                string levelName = AreaData.Get(level.Session.Area).Name;
                float lastAttemptTime = (float)Stopwatch.GetElapsedTime(this.lastSpawnTime).TotalSeconds;
                this.MakeRangedEvent("steam_heart", "Level Completed", "Level " + levelName + " completed", 1, lastAttemptTime, Steamworks.ETimelineEventClipPriority.k_ETimelineEventClipPriority_Featured);
                break;
        }
        switch (mode)
        {
            case LevelExit.Mode.Completed:
            case LevelExit.Mode.CompletedInterlude:
            case LevelExit.Mode.GiveUp:
            case LevelExit.Mode.SaveAndQuit:
                // If the user is quitting a Level, we need to set the gamemode to something other than Playing.
                Steamworks.SteamTimeline.SetTimelineGameMode(Steamworks.ETimelineGameMode.k_ETimelineGameMode_Menus);
                break;
        }
    }

    public override void Unload() {
        Steamworks.SteamTimeline.ClearTimelineStateDescription(0);
        Steamworks.SteamTimeline.SetTimelineGameMode(Steamworks.ETimelineGameMode.k_ETimelineGameMode_Invalid);
        Everest.Events.Player.OnSpawn -= Player_OnSpawn;
        Everest.Events.Player.OnDie -= Player_OnDie;
        Everest.Events.Level.OnEnter -= Level_OnEnter;
        Everest.Events.Level.OnTransitionTo -= Level_OnTransitionTo;
        Everest.Events.Level.OnExit -= Level_OnExit;
    }

    private void MakeRangedEvent(string icon, string title, string description, uint priority, float howFarBack, Steamworks.ETimelineEventClipPriority clipPriority)
    {
        // Make a ranged event for easy clipping.
        Steamworks.SteamTimeline.AddTimelineEvent(icon, title, description, priority, -howFarBack, howFarBack, clipPriority);
        // And an instantaneous event so the logo shows up in the timeline. 
        Steamworks.SteamTimeline.AddTimelineEvent(icon, title, description, 0, 0, 0, Steamworks.ETimelineEventClipPriority.k_ETimelineEventClipPriority_None);
    }
}