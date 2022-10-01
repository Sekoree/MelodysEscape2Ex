using System;
using Discord;
using HarmonyLib;
using UnityEngine;

namespace DiscordRPC
{
    [HarmonyPatch(typeof(MGame), nameof(MGame.Initialize))]
    public class DiscordInitialize
    {
        public static Discord.Discord DiscordRpcClient { get; set; }
        
        public static void Postfix()
        {
            Debug.Log("Discord RPC: Initializing");
            var clientId = "Client ID Here";
            DiscordRpcClient = new Discord.Discord(Int64.Parse(clientId), (UInt64)Discord.CreateFlags.Default);
            DiscordRpcClient.SetLogHook(Discord.LogLevel.Debug, (level, message) =>
            {
                Debug.Log($"Log {level} {message}");
            });

            var testActivity = new Activity()
            {
                State = "Somewhere",
                Details = "Doing something",
                Assets = new ActivityAssets()
                {
                    LargeImage = "testimage",
                    LargeText = "Yep idk",
                    SmallImage = "testimage",
                    SmallText = "Hello there"
                }
            };
            DiscordRpcClient.GetActivityManager().UpdateActivity(testActivity, (res) =>
            {
                Debug.Log($"Update {res}");
            });
            Debug.Log("Discord RPC: Initialized");
        }
    }
    
    [HarmonyPatch(typeof(GameController), "Update")]
    public class DiscordCallbacks
    {
        public static void Postfix()
        {
            if (DiscordInitialize.DiscordRpcClient != null)
            {
                DiscordInitialize.DiscordRpcClient.RunCallbacks();
            }
        }
    }
}