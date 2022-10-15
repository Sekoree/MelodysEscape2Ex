// See https://aka.ms/new-console-template for more information

using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace ME2StreamingUtil;

public class Program
{
    private static readonly YoutubeClient _client = new();
    
    public static async Task Main(string[] args)
    {
        if (args.Length == 2 && args[0]?.ToLower() == "video" && !string.IsNullOrEmpty(args[1]))
        {
            var id = VideoId.TryParse(args[1]);
            if (id is null)
            {
                Console.WriteLine("Invalid video ID");
                return;
            }
            await HandleVideo(id.Value);
        }
        else if (args.Length == 2 && args[0]?.ToLower() == "info" && !string.IsNullOrEmpty(args[1]))
        {
            var id = VideoId.TryParse(args[1]);
            if (id is null)
            {
                Console.WriteLine("Invalid video ID");
                return;
            }
            await HandleInfo(id.Value);
        }
    }

    private static async Task HandleVideo(VideoId videoId)
    {
        var video = await _client.Videos.Streams.GetManifestAsync(videoId);
        var streamInfo = video.GetAudioOnlyStreams().GetWithHighestBitrate();
        var stream = await _client.Videos.Streams.GetAsync(streamInfo);
        //write stream to standard output
        await stream.CopyToAsync(Console.OpenStandardOutput());
    }

    private static async Task HandleInfo(VideoId videoId)
    {
        var video = await _client.Videos.GetAsync(videoId);
        Console.WriteLine(video.Title);
        Console.WriteLine(video.Author);
    }
}