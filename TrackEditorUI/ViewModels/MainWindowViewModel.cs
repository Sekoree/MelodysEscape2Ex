using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using METrackEditor;

namespace TrackEditorUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackVersion))]
    [NotifyPropertyChangedFor(nameof(SampleCount))]
    [NotifyPropertyChangedFor(nameof(TransitionsCount))]
    [NotifyPropertyChangedFor(nameof(ObstaclesCount))]
    private Track? _track;

    public Version TrackVersion => Track?.CacheFileVersion ?? new Version(0, 0, 0, 0);
    
    public int SampleCount => Track?.SampleCount ?? 0;
    
    public int TransitionsCount => Track?.Transitions.Count ?? 0;
    
    public int ObstaclesCount => Track?.Obstacles.Count ?? 0;

    public MainWindowViewModel()
    {
        var trackString = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Circus-P - Better Off Worse.flac_2.txt"));
        Track = Track.LoadFromString(trackString);
    }
}
