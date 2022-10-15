using System;
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

    [HarmonyPatch(typeof(FileSelectUIController), "OnDriveButtonClicked")]
    public class FileSelectUIController_OnDriveButtonClicked_HandleYouTube
    {
        public static bool Prefix(FileSelectUIController __instance, ref object sender, ref EventArgs e)
        {
            //get private field SelectedAction of FileSelectUIController with reflection
            var field = typeof(FileSelectUIController).GetField("SelectedAction",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int selectedAction = (int)field.GetValue(__instance);
            if (selectedAction != 0)
            {
                return true;
            }

            __instance.HideMusicInfo();
            var button = sender as ButtonColorText;

            //get selectedDriveFullPath from private field in FileSelectUIController with reflection
            field = typeof(FileSelectUIController).GetField("selectedDriveFullPath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(__instance, button.FullPathTag);

            var linkFile = Path.Combine(Directory.GetCurrentDirectory(), "youtube_links.txt");
            if (!File.Exists(linkFile))
            {
                File.WriteAllText(linkFile, "//Put your youtube links here, one per line");
            }

            if (button?.EnumIDTag == 100)
            {
                Process.Start(Path.Combine(Directory.GetCurrentDirectory(), "youtube_links.txt"));
                return false;
            }
            else if (button?.EnumIDTag == 101)
            {
                //get private string lastOpenedPath of FileSelectUIController with reflection
                var lastOpenedPathField = typeof(FileSelectUIController).GetField("lastOpenedPath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                lastOpenedPathField.SetValue(__instance, null);
                __instance.FoldersScrollList.ClearItems();
                __instance.ClearFilesList(false);
                __instance.FilesScrollList.SetHeader("YouTube Links");
                bool isUsingUINavigation = __instance.TrackController.InputController.IsUsingUINavigation;
                //read youtube links from file
                var links = File.ReadAllLines(linkFile).Where(x => !x.StartsWith("//")).ToList();
                var parsedLinks = new List<string>();
                if (links.Any())
                {
                    parsedLinks.AddRange(from t in links
                        select VideoId.TryParse(t)
                        into id
                        where id != null
                        select $"[ME2Ex][YT]_{id.Value}");

                    __instance.FilesScrollList.AddFilesItems(parsedLinks, isUsingUINavigation, __instance.Sprites[0]);
                }

                __instance.HideMusicInfo();
                return false;
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(AudioEngine), "MusicFile", MethodType.Getter)]
    public class AudioEngine_get_MusicFile_HandleYouTube
    {
        public static void Postfix(AudioEngine __instance, ref string __result)
        {
            if (!(__instance is CustomAudioEngine customAudioEngine) || !customAudioEngine.CurrentIsYouTube) 
                return;

            var videoId = VideoId.TryParse(__result);
            if (videoId == null)
            {
                __result = null;
                return;
            }
            __result = $"[ME2Ex][YT]_{videoId.Value}";
        }
    }
}