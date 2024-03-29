﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using MelodyReactor2;
using UnityEngine;
using YoutubeExplode.Videos;
using Debug = UnityEngine.Debug;

namespace CustomAudioEngine
{
    [HarmonyPatch(typeof(MusicManager), "InitAudioEngine")]
    public class MusicManagerPatch
    {
        public static bool Prefix(MusicManager __instance, IntPtr windowHandle)
        {
            try
            {
                MReactor.CacheMusicSamples = false;
                MReactor.CacheOnsetsAreas = false;
                MReactor.EnableHeldNoteDetection = true;
                MReactor.UseOwnThread = true;
                var customAudioEngine = new CustomAudioEngine(windowHandle);
                customAudioEngine.SetPluginsDirectory(Application.dataPath + "/Plugins/x86_64");

                //get private and internal methods of MusicManager with reflection
                var methods = typeof(MusicManager).GetMethods(System.Reflection.BindingFlags.NonPublic |
                                                              System.Reflection.BindingFlags.Instance);

                var onStatusChangedMethod = methods.First(m => m.Name == "OnStatusChanged");
                var onMusicFileAnalyzedMethod = methods.First(m => m.Name == "OnMusicFileAnalyzed");
                var onMusicFileAnalysisExceptionMethod = methods.First(m => m.Name == "OnMusicFileAnalysisException");

                //This looks like a royal bruh moment, but I hope it works
                customAudioEngine.StatusChanged = (EventHandler)Delegate.Combine(customAudioEngine.StatusChanged,
                    new EventHandler((object sender, EventArgs e) =>
                        onStatusChangedMethod.Invoke(__instance, new object[] { sender, e })));
                customAudioEngine.MusicAnalyzed = (EventHandler)Delegate.Combine(customAudioEngine.MusicAnalyzed,
                    new EventHandler((object sender, EventArgs e) =>
                        onMusicFileAnalyzedMethod.Invoke(__instance, new object[] { sender, e })));
                customAudioEngine.MusicAnalysisException = (EventHandler)Delegate.Combine(
                    customAudioEngine.MusicAnalysisException,
                    new EventHandler((object sender, EventArgs e) =>
                        onMusicFileAnalysisExceptionMethod.Invoke(__instance, new object[] { sender, e })));

                MReactor.Initialize(customAudioEngine);
                customAudioEngine.SetVolume(0.3f);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(FileSelectUIController), "RefreshDrivesList")]
    public class FileSelectUIController_RefreshDrivesList_AddYoutTube
    {
        public static void Postfix(FileSelectUIController __instance)
        {
            __instance.DrivesScrollList.AddItem("Open YT Links File", "", 100, __instance.Sprites[7]);
            __instance.DrivesScrollList.AddItem("YouTube Links", "", 101, __instance.Sprites[7]);
        }
    }
}