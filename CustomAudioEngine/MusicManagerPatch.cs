using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CustomAudioEngine.Entities;
using HarmonyLib;
using MelodyReactor2;
using UnityEngine;

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
            __instance.DrivesScrollList.AddItem("YouTube URLs", "", 100, __instance.Sprites[7]);
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
            var button = sender as ButtonColorText;
            if (button?.EnumIDTag == 100)
            {
                __instance.FoldersScrollList.ClearItems();
                __instance.ClearFilesList(false);
                __instance.FilesScrollList.SetHeader("YouTube URLs");
                bool isUsingUINavigation = __instance.TrackController.InputController.IsUsingUINavigation;
                if (!File.Exists("youtube_links.txt"))
                {
                    File.WriteAllText("youtube_links.txt", string.Empty);
                }
                var links = File.ReadAllLines("youtube_links.txt");
                __instance.FilesScrollList.AddFilesItems(links, isUsingUINavigation, __instance.Sprites[0]);
                return false;
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(TrackLoadUIController), "GetMusicLoadingStatusText")]
    public class TrackLoadUIController_GetMusicLoadingStatusText_Patch
    {
        public static void Postfix(MusicLoadingStatus status, int percentage, ref string __result)
        {
            if (percentage == 100)
            {
                return;
            }

            if ((int)status == 100)
            {
                __result = "Loading YouTube URL...";
            }
        }
    }
    
    [HarmonyPatch(typeof(GameController), "ExportTrackToCache")]
    public class GameController_ExportTrackToCache_SanatizeYouTubeURL
    {
        public static bool Prefix(GameController __instance, ref MusicData musicData, ref TrackDefinition trackDefinition)
        {
            Console.WriteLine("Exporting track to cache: " + musicData.MusicInfo.Filename);
            //string text = Path.GetFileName(musicData.MusicInfo.Filename) + "_" + ((int)MGame.CurrentDifficultyRules.ObstacleDensity).ToString(CultureInfo.InvariantCulture) + ".txt";
            if (musicData.MusicInfo is WebMusicInfo webMusicInfo)
            {
                Console.WriteLine("Sanatizing YouTube URL");
                var videoId = Regex.Match(webMusicInfo.BaseUrl, @"v=(?<id>[^&]+)").Groups["id"].Value;
                var text = "[ME2Ex][YT]_" + videoId + "_" + ((int)MGame.CurrentDifficultyRules.ObstacleDensity).ToString(CultureInfo.InvariantCulture) + ".txt";
                string text2 = Path.Combine(__instance.CacheDirectoryPath, text);
                try
                {
                    if (!Directory.Exists(__instance.CacheDirectoryPath))
                    {
                        Directory.CreateDirectory(__instance.CacheDirectoryPath);
                    }
                    trackDefinition.DisplayBPM = (int)Math.Round(musicData.MusicInfo.BPM);
                    string trackCacheData = musicData.GetTrackCacheData(trackDefinition, "1.03");
                    Console.WriteLine("Proposed filename: " + text2);
                    if (File.Exists(text2))
                    {
                        if (File.ReadAllText(text2) != trackCacheData)
                        {
                            Debug.Log("[cache] Track Cache differ, overwriting! " + text);
                            File.WriteAllText(text2, trackCacheData);
                        }
                        else
                        {
                            Debug.Log("[cache] Identical Cache Files, no action taken: " + text);
                        }
                    }
                    else
                    {
                        Debug.Log("[cache] Wrote track cache file: " + text);
                        File.WriteAllText(text2, trackCacheData);
                    }
                }
                catch (Exception exception)
                {
                    Debug.Log("Error while trying to save Track Cache file: '" + text2 + "'");
                    Debug.LogException(exception);
                }
                return false;
            }
            return true;
        }
    }
    
    //Man patching base lib classes wew
    [HarmonyPatch(typeof(Path), "GetFileName")]
    public class GameController_AnalyseMusicFileAsync_Patch
    {
        static readonly Regex regex = new Regex(@"^https:\/\/www\.youtube\.com\/watch\?v=(?<id>[a-zA-Z0-9_-]{11})$");
        
        public static bool Prefix(ref string __result, ref string path)
        {
            var match = regex.Match(path);
            if (match.Success)
            {
                var id = match.Groups["id"].Value;
                __result = $"[ME2Ex][YT]_{id}";
                return false;
            }
            return true;
        }
    }
}