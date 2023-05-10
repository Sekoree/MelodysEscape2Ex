// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace ME2StreamingUtil;

public class Exports
{
    //callback for getting video info, takes a VideoInfo struct
    public delegate void GetVideoInfoCallback(IntPtr videoInfoPtr);
    public delegate void GetVideoInfoErrorCallback(IntPtr errorStringPtr);

    public static GetVideoInfoCallback? GetVideoInfoCallbackInstance;
    public static GetVideoInfoErrorCallback? GetVideoInfoErrorCallbackInstance;
    
    
    //callback for getting audio data, takes an AudioData struct
    public delegate void GetAudioDataCallback(IntPtr audioDataPtr);
    public delegate void GetAudioDataErrorCallback(IntPtr errorStringPtr);
    
    public static GetAudioDataCallback? GetAudioDataCallbackInstance;
    public static GetAudioDataErrorCallback? GetAudioDataErrorCallbackInstance;

    private static readonly YoutubeClient YoutubeClient = new();
    private static readonly HttpClient HttpClient = new();

    [UnmanagedCallersOnly(EntryPoint = "SetGetVideoInfoCallback")]
    public static void SetGetVideoInfoCallback(IntPtr callbackPtr, IntPtr errorCallbackPtr)
    {
        var callback = Marshal.GetDelegateForFunctionPointer<GetVideoInfoCallback>(callbackPtr);
        var errorCallback = Marshal.GetDelegateForFunctionPointer<GetVideoInfoErrorCallback>(errorCallbackPtr);
        GetVideoInfoCallbackInstance = callback;
        GetVideoInfoErrorCallbackInstance = errorCallback;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "SetGetAudioDataCallback")]
    public static void SetGetAudioDataCallback(IntPtr callbackPtr, IntPtr errorCallbackPtr)
    {
        var callback = Marshal.GetDelegateForFunctionPointer<GetAudioDataCallback>(callbackPtr);
        var errorCallback = Marshal.GetDelegateForFunctionPointer<GetAudioDataErrorCallback>(errorCallbackPtr);
        GetAudioDataCallbackInstance = callback;
        GetAudioDataErrorCallbackInstance = errorCallback;
    }

    [UnmanagedCallersOnly(EntryPoint = "StartGetVideoInfo")]
    public static void StartGetVideoInfo(IntPtr videoIdPtr)
    {
        var videoIdString = Marshal.PtrToStringUni(videoIdPtr);
        if (videoIdString is null || VideoId.TryParse(videoIdString) is null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var video = await YoutubeClient.Videos.GetAsync(videoIdString);
                var thumbnail = video.Thumbnails.GetWithHighestResolution();
                var thumbnailData = await HttpClient.GetByteArrayAsync(thumbnail.Url);
                var videoInfo = new VideoInfo
                {
                    Title = Marshal.StringToHGlobalUni(video.Title),
                    Author = Marshal.StringToHGlobalUni(video.Author.ChannelTitle),
                    ThumbnailDataLength = thumbnailData.Length,
                    ThumbnailData = Marshal.AllocHGlobal(thumbnailData.Length)
                };
                Marshal.Copy(thumbnailData, 0, videoInfo.ThumbnailData, thumbnailData.Length);
                GetVideoInfoCallbackInstance?.Invoke(Marshal.AllocHGlobal(Marshal.SizeOf(videoInfo)));
            }
            catch (Exception e)
            {
                GetVideoInfoErrorCallbackInstance?.Invoke(Marshal.StringToHGlobalUni(e.Message));
            }
        });
    }
    
    [UnmanagedCallersOnly(EntryPoint = "StartGetAudioData")]
    public static void StartGetAudioData(IntPtr videoIdPtr)
    {
        var videoIdString = Marshal.PtrToStringUni(videoIdPtr);
        if (videoIdString is null || VideoId.TryParse(videoIdString) is null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var streamManifest = await YoutubeClient.Videos.Streams.GetManifestAsync(videoIdString);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                var ms = new MemoryStream();
                await YoutubeClient.Videos.Streams.CopyToAsync(streamInfo, ms, new Progress<double>(x =>
                {
                    Console.WriteLine($"Progress for {videoIdString}: {x}");
                }));
                var audioData = new AudioData
                {
                    Length = ms.Length,
                    Data = Marshal.AllocHGlobal((int) ms.Length)
                };
                Marshal.Copy(ms.GetBuffer(), 0, audioData.Data, (int) ms.Length);
                GetAudioDataCallbackInstance?.Invoke(Marshal.AllocHGlobal(Marshal.SizeOf(audioData)));
            }
            catch (Exception e)
            {
                GetAudioDataErrorCallbackInstance?.Invoke(Marshal.StringToHGlobalUni(e.Message));
            }
        });
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct VideoInfo
{
    public IntPtr Title { get; set; }
    public IntPtr Author { get; set; }
    public long ThumbnailDataLength { get; set; }
    public IntPtr ThumbnailData { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct AudioData
{
    public long Length { get; set; }
    public IntPtr Data { get; set; }
}