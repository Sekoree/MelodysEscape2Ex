using System.Net;
using MelodyReactor2;

namespace CustomAudioEngine.Entities
{
    public class WebMusicInfo : MusicInfo
    {
        public string BaseUrl { get; set; }
        public string DirectUrl { get; set; }
        
        //public byte[] GetBytes()
        //{
        //    //looks dumb but maybe
        //    using (var client = new WebClient())
        //    {
        //        client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36");
        //        client
        //        return client.DownloadData(DirectUrl);
        //    }
        //}
    }
}