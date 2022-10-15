using System.Net;
using MelodyReactor2;

namespace CustomAudioEngine.Entities
{
    public class WebMusicInfo : MusicInfo
    {
        public string BaseUrl { get; set; }
        
        public byte[] DataBytes { get; set; }
    }
}