// See https://aka.ms/new-console-template for more information

using MelodyReactor2;
using CustomAudioEngine = CustomAudioEngine.CustomAudioEngine;

Console.WriteLine("Hello, World!");

DifficultyRules.InitializeDefaultDifficulties();
MReactor.CurrentDifficultyRules = DifficultyRules.DefaultDifficulties[2];

//MReactor.FFTQuality = 4096;

MReactor.CacheMusicSamples = false;
MReactor.CacheOnsetsAreas = false;
MReactor.EnableHeldNoteDetection = true;
MReactor.UseOwnThread = true;

var engine = new global::CustomAudioEngine.CustomAudioEngine(IntPtr.Zero);
//engine.StatusChanged += (sender, args) => Console.WriteLine("Idk what happened but the status changed");
//engine.MusicAnalyzed += (sender, args) => Console.WriteLine("Music analyzed");
//engine.MusicAnalysisException += (sender, args) => Console.WriteLine("Music analysis exception");
engine.SetPluginsDirectory(Directory.GetCurrentDirectory());
MReactor.Initialize(engine);
engine.SetVolume(0.3f);

//engine.SetMusicFile("song.flac");
engine.SetMusicFile("https://www.youtube.com/watch?v=gKXDTpnJKLk");
//engine.SetMusicFile("song.flac");
engine.InitMusicFile();
engine.AnalyzeMusicFile();

engine.MusicData.TrackDefinition.DisplayBPM = (int)Math.Round(engine.MusicData.MusicInfo.BPM);
var trackData = engine.MusicData.GetTrackCacheData(engine.MusicData.TrackDefinition, "1.03");
File.WriteAllText("trackData.txt", trackData);
Console.WriteLine("Track data saved to trackData.txt");
