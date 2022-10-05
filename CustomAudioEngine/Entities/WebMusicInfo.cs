using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MelodyReactor2;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace CustomAudioEngine.Entities
{
    public class WebMusicInfo : MusicInfo
    {
        private string _tempPath;
        public string BaseUrl => this.Filename;

        public string TempPath
        {
            get
            {
                if (_tempPath == null)
                {
                    GetDirectUrlFromYTDLP();
                }
                return _tempPath;
            }
            set => _tempPath = value;
        }

        private void GetDirectUrlFromYTDLP()
        {
            Debug.Log("Downloading");
            //youtube video id regex
            var regex = new System.Text.RegularExpressions.Regex(@"(?<=v=)[^&#]+");
            var match = regex.Match(this.Filename);
            var videoId = match.Value;

            var supposedPath2 = Path.Combine(Directory.GetCurrentDirectory(), "ME2Patches", "YouTubeTemp", $"{videoId}.weba");
            if (File.Exists(supposedPath2))
            {
                _tempPath = supposedPath2;
                return;
            }
            
            Debug.Log(videoId);
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Directory.GetCurrentDirectory(), "yt-dlp.exe"),
                    Arguments = $"-f bestaudio -P \"{Path.Combine(Directory.GetCurrentDirectory(), "ME2Patches", "YouTubeTemp")}\" -o \"%(id)s.weba\" --embed-metadata {BaseUrl}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            var supposedPath = Path.Combine(Directory.GetCurrentDirectory(), "ME2Patches", "YouTubeTemp", $"{videoId}");
            if (File.Exists(supposedPath))
            {
                _tempPath = supposedPath;
            }
            else
            {
                Debug.LogError("Temp File not found");
            }
        }
        
        //~WebMusicInfo()
        //{
        //    if (File.Exists(_tempPath))
        //    {
        //        File.Delete(_tempPath);
        //    }
        //}
    }
}