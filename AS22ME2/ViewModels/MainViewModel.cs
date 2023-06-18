using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using METrackEditor;
using MiniAS2Renderer;

namespace AS22ME2.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region AS2 Parameters

    [ObservableProperty] private float _minSpeed = 0.01f;
    [ObservableProperty] private float _maxSpeed = 6.1f;
    [ObservableProperty] private float _minJumpTime = 2.15f;
    [ObservableProperty] private bool _downhillOnly = false;
    [ObservableProperty] private float _uphillScaler = 5.5f;
    [ObservableProperty] private float _downhillScaler = 5.5f;
    [ObservableProperty] private bool _useAveragedFlatSlopes = false;
    [ObservableProperty] private float _uphillSmoother = 0.03f;
    [ObservableProperty] private float _downhillSmoother = 0.03f;
    [ObservableProperty] private float _gravity = 0.45f;

    #endregion

    #region ME2 Track Data

    [ObservableProperty] private Track? _me2Track;
    [ObservableProperty] private string? _cacheFile;
    [ObservableProperty] private string? _audioFile;

    #endregion

    #region Output

    [ObservableProperty] private Track? _newME2Track;

    #endregion

    #region Conversion Parameters

    [ObservableProperty] private decimal _solidObstacleChance = 30; //decimal cause NumericUpDown
    [ObservableProperty] private decimal _minimumObstacleDistance = 9; //decimal cause NumericUpDown
    [ObservableProperty] private bool _replaceCacheFile = false; //decimal cause NumericUpDown

    #endregion

    #region Tips

    public string WhereFilesTip =>
        "Output files get saved where the .exe of this tool is! (Unless you check the Replace Cache File option)";

    public string ControlOrDensityTypesTip =>
        "The number at the end of ME2 indicated the Density (Low, Medium, Extreme) or Control Type (Mono, Colors, Colors+Directions) => 0, 1, 2";

    public string DensityTip =>
        "The minimum distance between obstacles is set at 9 here, this is quite difficult, but still generates possible tracks (most of the time), set it higher or maybe even lower if you want to experiment";

    public string As2IsDifferentTip =>
        "Please keep in mind that Audiosurf 2 is a very 3 dimensional game with no predefined speed levels (like ME2's walking, jogging, running, flying), slow parts in AS2 generally have less blocks/obstacles, but the speed is still the same, so you might want to experiment with the speed settings to get a good result";

    #endregion

    //Cache file Directory is always "AppData\LocalLow\Icetesy\Melody's Escape 2\Tracks Cache"
    private readonly string _cacheFileDirectory =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow"),
            "Icetesy", "Melody's Escape 2", "Tracks Cache");

    [RelayCommand]
    public async Task OpenAndLoadMe2Track()
    {
        //main window
        var window = ((ClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow;
        var cacheFolder = await window!.StorageProvider.TryGetFolderFromPathAsync(_cacheFileDirectory);
        var opts = new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("ME2 Chache File")
                {
                    MimeTypes = new[]
                    {
                        "text/plain"
                    },
                    Patterns = new[]
                    {
                        "*.txt"
                    }
                }
            },
            Title = "Select ME2 Cache File",
            SuggestedStartLocation = cacheFolder
        };
        var files = await window!.StorageProvider.OpenFilePickerAsync(opts);
        if (files.Count == 0)
            return;

        var file = files[0].TryGetLocalPath();
        if (file == null || !File.Exists(file))
            return;

        var trackData = await File.ReadAllLinesAsync(file);
        CacheFile = file;
        Me2Track = Track.LoadFromString(trackData);
    }

    [RelayCommand]
    public void OpenCacheFolder()
    {
        Process.Start(new ProcessStartInfo()
        {
            UseShellExecute = true,
            FileName = _cacheFileDirectory,
            Verb = "open"
        });
    }

    [RelayCommand]
    public async Task GetAudioFileLocation()
    {
        var window = ((ClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow;
        var opts = new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Audio File")
                {
                    MimeTypes = new[]
                    {
                        "audio/*"
                    },
                    Patterns = new[]
                    {
                        "*.mp3", "*.wav", "*.ogg", "*.flac", "*.m4a" //TODO: Add more, bit I'm lazy rn
                    }
                }
            },
            Title = "Select Audio File"
        };
        var files = await window!.StorageProvider.OpenFilePickerAsync(opts);
        if (files.Count == 0)
            return;

        var file = files[0].TryGetLocalPath();
        if (file == null || !File.Exists(file))
            return;

        AudioFile = file;
    }

    [RelayCommand]
    public async Task GenerateNewObstacles()
    {
        if (Me2Track == null || !File.Exists(AudioFile) || !File.Exists(CacheFile))
            return;

        var copyOfTrack =
            Track.LoadFromString(Me2Track.ToString()
                .Split(Environment.NewLine,
                    StringSplitOptions.RemoveEmptyEntries)); //not 100% about line breaks, but should work

        copyOfTrack.Obstacles.Clear();

        var sumsAndSeconds = MiniRenderer.GetAllFloatSamples(AudioFile);
        var nodes = MiniRenderer.GetAllTrackNodes(
            sumsAndSeconds.sums,
            sumsAndSeconds.seconds,
            MinSpeed,
            MaxSpeed,
            MinJumpTime,
            DownhillOnly,
            UphillScaler,
            DownhillScaler,
            UseAveragedFlatSlopes,
            UphillSmoother,
            DownhillSmoother,
            Gravity);

        //skip nodes before 0 seconds
        nodes = nodes.SkipWhile(n => n.Seconds < 0).ToArray();

        //skip nodes after the end of the track
        nodes = nodes.TakeWhile(n => n.Seconds < copyOfTrack.SongDuration).ToArray();

        //if (nodes.Length < sumsAndSeconds.sums.Length) //funny error
        //    throw new Exception(string.Format("Not enough nodes? Less nodes than samples {0} Nodes, {1} Samples",
        //        nodes.Length, sumsAndSeconds.sums.Length));


        var indexedNodes = new Dictionary<int, Node>(); //index, node
        var sumsPerSecond = sumsAndSeconds.sums.Length / sumsAndSeconds.seconds;
        //index nodes since Nodes run on Seconds, but the track on Samples
        foreach (var node in nodes)
        {
            var index = (int)(node.Seconds * sumsPerSecond);
            if (indexedNodes.ContainsKey(index))
            {
                var existingNode = indexedNodes[index];
                var averagedNode = new Node()
                {
                    HasBlock = node.HasBlock || existingNode.HasBlock,
                    Seconds = (existingNode.Seconds + node.Seconds) / 2,
                    JumpSeconds = (existingNode.JumpSeconds + node.JumpSeconds) / 2,
                    Intensity = (existingNode.Intensity + node.Intensity) / 2,
                    Pos = new Vector3()
                    {
                        X = (existingNode.Pos.X + node.Pos.X) / 2,
                        Y = (existingNode.Pos.Y + node.Pos.Y) / 2,
                        Z = (existingNode.Pos.Z + node.Pos.Z) / 2
                    },
                    RotVector = new Vector3()
                    {
                        X = (existingNode.RotVector.X + node.RotVector.X) / 2,
                        Y = (existingNode.RotVector.Y + node.RotVector.Y) / 2,
                        Z = (existingNode.RotVector.Z + node.RotVector.Z) / 2
                    },
                    MaxAir = (existingNode.MaxAir + node.MaxAir) / 2,
                    TrafficStrength = (existingNode.TrafficStrength + node.TrafficStrength) / 2,
                    TrafficChainStart =
                        (existingNode.TrafficChainStart > nodes.Length || existingNode.TrafficChainStart < 0)
                            ? node.TrafficChainStart
                            : existingNode.TrafficChainStart,
                    TrafficChainEnd =
                        (existingNode.TrafficChainStart > nodes.Length || existingNode.TrafficChainStart < 0)
                            ? node.TrafficChainEnd
                            : existingNode.TrafficChainEnd,
                };
                indexedNodes[index] = averagedNode;
            }
            else
            {
                indexedNodes[index] = node;
            }
        }

        var indexedNodesWithBlocks = indexedNodes.Where(x => x.Value.HasBlock)
            .OrderBy(x => x.Key)
            .ToArray();

        var rng = new Random(indexedNodesWithBlocks.Length);
        foreach (var indexedNode in indexedNodesWithBlocks)
        {
            var lastObstacle = copyOfTrack.Obstacles.LastOrDefault();
            if (lastObstacle != null && (indexedNode.Key - lastObstacle.EndSampleID) < (int)MinimumObstacleDistance)
                continue;

            var indexToUse = indexedNode.Key;
            //if (indexedNode.Value.TrafficChainStart != indexedNode.Value.TrafficChainEnd)
            //{
            //    var endIndexSeconds = rawNodes[indexedNode.Value.TrafficChainEnd].Seconds;
            //    var endIndex = (int)(endIndexSeconds * sumsPerSecond);
            //    indexToUse = (indexToUse + endIndex) / 2;
            //}
            //AS2 Chains are a mess

            var obst = rng.Next(4, 8);
            var wallChance = rng.Next(0, 100);
            if (wallChance > (100 - (int)SolidObstacleChance))
                obst -= 4;

            var obstacle = new Obstacle()
            {
                SampleID = indexToUse,
                EndSampleID = indexToUse + 1,
                ForceType = (ObstacleInputType)(obst),
                IsHeld = false,
                IsSolid = false
            };
            copyOfTrack.Obstacles.Add(obstacle);
        }

        NewME2Track = copyOfTrack;
        if (!ReplaceCacheFile)
            await File.WriteAllTextAsync(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    Path.GetFileName(CacheFile)),
                NewME2Track.ToString());
        else
            await File.WriteAllTextAsync(CacheFile,
                NewME2Track.ToString());
    }
}