using System;
using Discord;
using HarmonyLib;
using MelodyReactor2;
using UnityEngine;

namespace DiscordRPC
{
    public class Shared
    {
        public static Discord.Discord DiscordRpcClient { get; set; }
    }

    #region Init + Callbacks

    [HarmonyPatch(typeof(MGame), nameof(MGame.Initialize))]
    public class DiscordInitialize
    {
        public static void Postfix()
        {
            Debug.Log("Discord RPC: Initializing");
            //replace with your discord apps client ID
            Shared.DiscordRpcClient = new Discord.Discord(1025837502206054451, (UInt64)Discord.CreateFlags.Default);
            Shared.DiscordRpcClient.SetLogHook(LogLevel.Debug,
                (level, message) => { Debug.Log($"Log {level} {message}"); });
            Debug.Log("Discord RPC: Initialized");
        }
    }

    [HarmonyPatch(typeof(GameController), "Update")]
    public class DiscordCallbacks
    {
        public static void Postfix()
        {
            if (Shared.DiscordRpcClient != null)
            {
                Shared.DiscordRpcClient.RunCallbacks();
            }
        }
    }

    #endregion

    #region Presence Updates

    [HarmonyPatch(typeof(FileSelectUIController), "Show")]
    public class FileSelectUIController_Show
    {
        public static void Postfix()
        {
            if (Shared.DiscordRpcClient == null) 
                return;
            
            var activityManager = Shared.DiscordRpcClient.GetActivityManager();
            var activity = new Activity
            {
                State = "Browsing Songs",
                Assets =
                {
                    LargeImage = "melody"
                },
                Timestamps =
                {
                    Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            };
            activityManager.UpdateActivity(activity, result => { });
        }
    }
    
    [HarmonyPatch(typeof(MainMenuUIController), "Show")]
    public class MainMenuUIController_Show
    {
        public static void Postfix()
        {
            if (Shared.DiscordRpcClient == null) 
                return;
            
            var activityManager = Shared.DiscordRpcClient.GetActivityManager();
            var activity = new Activity
            {
                State = "In the Main Menu",
                Assets =
                {
                    LargeImage = "melody"
                },
                Timestamps =
                {
                    Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            };
            activityManager.UpdateActivity(activity, result => { });
        }
    }
    
    [HarmonyPatch(typeof(SettingsUIController), "Show")]
    public class SettingsUIController_Show
    {
        public static void Postfix()
        {
            if (Shared.DiscordRpcClient == null) 
                return;
            
            var activityManager = Shared.DiscordRpcClient.GetActivityManager();
            var activity = new Activity
            {
                State = "Changing Settings",
                Assets =
                {
                    LargeImage = "melody"
                },
                Timestamps =
                {
                    Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            };
            activityManager.UpdateActivity(activity, result => { });
        }
    }
    
    [HarmonyPatch(typeof(CustomizationUIController), "Show")]
    public class CustomizationUIController_Show
    {
        public static void Postfix()
        {
            if (Shared.DiscordRpcClient == null) 
                return;
            
            var activityManager = Shared.DiscordRpcClient.GetActivityManager();
            var activity = new Activity
            {
                State = "Customizing Melody",
                Assets =
                {
                    LargeImage = "melody"
                },
                Timestamps =
                {
                    Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            };
            activityManager.UpdateActivity(activity, result => { });
        }
    }
    
    [HarmonyPatch(typeof(EndScoreUIController), "PopulateData")]
    public class EndScoreUIController_PopulateData
    {
        public static void Postfix(EndScoreUIController __instance, MusicData musicData, Track track)
        {
            if (Shared.DiscordRpcClient == null) 
                return;
            
            var songName = __instance.TextTitle.text;
            var activityManager = Shared.DiscordRpcClient.GetActivityManager();
            var hearts = "";
            if (!track.PerfectChainTargetAchieved)
            {
                for (var i = 0; i < 5; i++)
                {
                    var hitTarget = track.ScoreTargets[i];
                    if (track.Score >= hitTarget)
                    {
                        hearts += "❤";
                    }
                    else
                    {
                        hearts += "🖤";
                    }
                }
            }
            else
            {
                hearts = "💛💛💛💛💛";
            }

            var perfectSanitize = __instance.TextMaxChainValue.text.Split(' ')[0];
            var activity = new Activity
            {
                State = "Escaped : " + songName,
                Details = $"{hearts} | Difficulty: {__instance.TextDifficultyValue.text} | Best Chain: {perfectSanitize}",
                Assets =
                {
                    LargeImage = "melody",
                    LargeText = $"Score: {track.Score} | Score Target: {track.ScoreTargets[4]}",
                },
                Timestamps =
                {
                    Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            };
            activityManager.UpdateActivity(activity, result => { });
        }
    }
    
    [HarmonyPatch(typeof(MusicController), "PlayPreloadedStream")]
    public class MusicController_PlayPreloadedStream
    {
        public static void Postfix(MusicController __instance)
        {
            if (Shared.DiscordRpcClient == null) 
                return;
            
            var musicInfo = MReactor.AudioEngine.MusicData.MusicInfo;
            var activityManager = Shared.DiscordRpcClient.GetActivityManager();
            var activity = new Activity
            {
                State = "Escaping : " + musicInfo.TagTitle,
                //Details = $"Difficulty: {__instance.Track.} | Best Chain: {__instance.TrackData.BestChain}",
                Assets =
                {
                    LargeImage = "melody"
                },
                Timestamps =
                {
                    End = (long) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds +
                        (long) musicInfo.Duration - (long) musicInfo.Position
                }
            };
            activityManager.UpdateActivity(activity, result => { });
        }
    }
    
    [HarmonyPatch(typeof(MusicController), "PauseStream")]
    public class PauseMenuUIController_Show
    {
        public static void Postfix()
        {
            if (Shared.DiscordRpcClient == null) 
                return;
            
            var activityManager = Shared.DiscordRpcClient.GetActivityManager();
            var activity = new Activity
            {
                State = "Paused",
                Assets =
                {
                    LargeImage = "melody"
                },
                Timestamps =
                {
                    Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            };
            activityManager.UpdateActivity(activity, result => { });
        }
    }
    
    [HarmonyPatch(typeof(MusicController), "ResumeStream")]
    public class MusicController_ResumeStream
    {
        public static void Postfix()
        {
            if (Shared.DiscordRpcClient == null) 
                return;
            
            var musicInfo = MReactor.AudioEngine.MusicData.MusicInfo;
            var activityManager = Shared.DiscordRpcClient.GetActivityManager();
            var activity = new Activity
            {
                State = "Escaping : " + musicInfo.TagTitle,
                Assets =
                {
                    LargeImage = "melody"
                },
                Timestamps =
                {
                    End = (long) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds +
                        (long) musicInfo.Duration - (long) musicInfo.Position
                }
            };
            activityManager.UpdateActivity(activity, result => { });
        }
    }

    #endregion
}