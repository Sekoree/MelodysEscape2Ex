using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using ManagedBass;
using ManagedBass.Mix;
using ManagedBass.Tags;
using MelodyReactor2;
using TagLib;
using Debug = UnityEngine.Debug;
using File = TagLib.File;

namespace CustomAudioEngine
{
    public class ManagedBassAudioEngine : AudioEngine
    {
        public static bool USE_OLD_GETFFTDATA = false;
        public static bool LOAD_PLUGINS = true;
        public static bool FORCE_44100_DECODING = true;
        public static bool USE_CONFIG_DEVICE_BUFFER = false;
        public static bool USE_FFT_HANN_WINDOW = true;
        public static bool FORCE_PAUSE_EXACT_POSITION = true;

        private const int _something = 100;
        private IntPtr _windowHandle = IntPtr.Zero;

        private DSPProcedure _dspProcedure;
        private StreamProcedure _streamProcedure;

        private Thread _initAndAnalyzeThread;

        private int _decodeStream = 0;
        private int _mixStream = 0;
        private int _otherStream = 0;
        
        private Stopwatch _analyzeStopwatch = new Stopwatch();

        private int _weirdChannelValue;

        //float array which is MReactor.FFTQuality * 100 long
        private float[] _fftDataTimes100;

        //float array which is MReactor.FFTQuality long
        private float[] _fftData;

        //byte array which is MReactor.FFTQuality divided by 2 long
        private byte[] _fftDataByte;

        //float array which is MReactor.FFTQuality divided by 2 multiplied by someFactorValue long
        private float[] _fftDataTimesFactor;

        //not sure if actually bassflags
        private int _bassFlags = (int)BassFlags.Default;

        //seems to correlate with USE_FFT_HANN_WINDOW, 16 if true, 32 if false
        private int _someOtherRandomValue;

        //plugin directory
        private string _pluginDirectory;

        private bool _notMonoOrFrequencyMismatch;

        private long _decodeChannelLength;

        //No fucking idea why window handle is needed
        public ManagedBassAudioEngine(IntPtr mainWindowHandle)
        {
            _windowHandle = mainWindowHandle;
            _dspProcedure = new DSPProcedure(_dspProcedure);
            _streamProcedure = new StreamProcedure(_streamProcedure);
            if (MReactor.FFTQuality < 1024) //seems to be 4096 by default
                MReactor.FFTQuality = 1024;
            _weirdChannelValue = MReactor.FFTQuality / 1024;
            _fftDataTimes100 = new float[MReactor.FFTQuality * 100];
            _fftData = new float[MReactor.FFTQuality];
            _fftDataByte = new byte[MReactor.FFTQuality / 2];
            _fftDataTimesFactor = new float[MReactor.FFTQuality / 2 * _weirdChannelValue];

            _bassFlags = -2147483644;
            _someOtherRandomValue |= 16;
        }

        public void FreeChannel(ref int channel)
        {
            if (channel != 0)
            {
                Bass.StreamFree(channel);
            }

            channel = 0;
        }

        public List<string> GetPluginFiles(string directoryPath, string searchPattern, SearchOption searchOption)
        {
            List<string> pluginFiles = new List<string>();
            string str = searchPattern;
            char[] chArray = new char[1] { ';' };
            foreach (string searchPattern1 in str.Split(chArray))
                pluginFiles.AddRange(
                    (IEnumerable<string>)Directory.GetFiles(directoryPath, searchPattern1, searchOption));
            for (int index = pluginFiles.Count - 1; index >= 0; --index)
            {
                string withoutExtension = Path.GetFileNameWithoutExtension(pluginFiles[index]);
                if (!withoutExtension.StartsWith("bass"))
                    pluginFiles.RemoveAt(index);
                else if (withoutExtension == "bass" || withoutExtension == "bass_fx")
                    pluginFiles.RemoveAt(index);
            }

            pluginFiles.Sort();
            return pluginFiles;
        }

        public override void SetPluginsDirectory(string path)
        {
            _pluginDirectory = path;
        }

        public override void Initialize()
        {
            base.Initialize();
            if (LOAD_PLUGINS)
            {
                if (Directory.Exists(_pluginDirectory))
                {
                    List<string> pluginFiles = GetPluginFiles(_pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
                    foreach (string pluginFile in pluginFiles)
                    {
                        Bass.PluginLoad(pluginFile);
                    }
                }
            }

            SupportedFilesExtensions = Bass.SupportedFormats;
            int num = 0;
            bool couldInitBass = false;
            Errors bassError = Errors.OK;
            //Some buffer stuff here that is disabled

            Bass.Configure(Configuration.IncludeDefaultDevice, true);

            //try to init bass 5 times
            for (; num < 5; num++)
            {
                if (Bass.Init(Win: _windowHandle))
                {
                    if (LOAD_PLUGINS)
                    {
                        BassInfo bassInfo = new BassInfo();
                        Bass.GetInfo(out bassInfo);
                        Debug.Log("Bass info" + bassInfo);
                    }

                    couldInitBass = true;
                    break;
                }

                bassError = Bass.LastError;
                if (bassError == Errors.Already)
                {
                    Bass.Free();
                }

                Thread.Sleep(100);
            }

            if (couldInitBass)
            {
                throw new AudioEngineInitializationException("Bass committed cringe and could not be initialized: " +
                                                             bassError);
            }

            Bass.Configure(Configuration.UpdatePeriod, MReactor.UseOwnThread ? 5 : 0);
            Bass.Configure(Configuration.PlaybackBufferLength, 250);
            //USE_CONFIG_DEVICE_BUFFER is always false
        }

        public override void CleanRessources()
        {
            base.CleanRessources();
        }

        public override bool IsReadyToAnalyze()
        {
            return _decodeStream != 0;
        }

        public override void InitAndAnalyzeMusicFileAsync()
        {
            _initAndAnalyzeThread = new Thread(new ThreadStart(InitAndAnalyzeMusicFileAsyncThread));
            _initAndAnalyzeThread.Start();
        }

        public override void AnalyzeMusicFileAsync()
        {
            _initAndAnalyzeThread = new Thread(new ThreadStart(AnalyzeMusicFileAsyncThread));
            _initAndAnalyzeThread.Start();
        }

        public void InitAndAnalyzeMusicFileAsyncThread()
        {
            if (!MReactor.AudioEngine.InitMusicFile())
                return;
            MReactor.AudioEngine.AnalyzeMusicFile();
        }

        public void AnalyzeMusicFileAsyncThread()
        {
            MReactor.AudioEngine.AnalyzeMusicFile();
        }

        public override bool InitMusicFile()
        {
            base.InitMusicFile();
            if (string.IsNullOrEmpty(MusicFile))
            {
                LastError = "No music file specified";
                return false;
            }

            MusicData.MusicInfo = new MusicInfo();
            MusicData.MusicInfo.FileFullPath = MusicFile;
            MusicData.MusicInfo.Filename = Path.GetFileName(MusicFile);
            MusicData.MusicInfo.FilenameWithoutExt = Path.GetFileNameWithoutExtension(MusicFile);
            var smallD = 0;
            var bigD = 0;
            var smallA = 0L;
            var bigA = 0.0f;
            _decodeStream = Bass.CreateStream(MusicFile, 0L, 0L,
                BassFlags.Mono | BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
            if (_decodeStream == 0)
            {
                this.MusicData = null;
                LastError = "Could not create stream";
                LastErrorCode = Bass.LastError.ToString();
                //some check here for a specific code but idk cause obfuscation
                return false;
            }

            //no picture tags
            var tags = GetTagsFromFile(MusicFile);
            var channelInfo = new ChannelInfo();
            Bass.ChannelGetInfo(_decodeStream, out channelInfo);
            _notMonoOrFrequencyMismatch = channelInfo.Channels > 1 ||
                                          BassAudioEngine.FORCE_44100_DECODING && channelInfo.Frequency != 44100;
            _decodeChannelLength = Bass.ChannelGetLength(_decodeStream);
            var asSeconds = Bass.ChannelBytes2Seconds(_decodeStream, _decodeChannelLength);

            try
            {
                //TODO: Needs argument checking too
                //get internal method "a" from MusicData with reflection
                var a = typeof(MusicData).GetMethod("a", BindingFlags.NonPublic | BindingFlags.Instance);
                //Idk if this will work cause this is obfuscation pain
                a.Invoke(MusicData,
                    new object[]
                    {
                        _notMonoOrFrequencyMismatch ? 44100 : channelInfo.Frequency, channelInfo.Channels, asSeconds
                    });
            }
            catch (Exception e)
            {
                Debug.Log("Error while invoking MusicData.a: " + e);
                throw new Exception("Error while invoking MusicData.a: " + e);
            }
            
            MusicData.MusicInfo.SampleRate = channelInfo.Frequency;
            if (tags != null)
            {
                MusicData.MusicInfo.TagAlbum = tags.Album;
                MusicData.MusicInfo.TagArtist = tags.FirstAlbumArtist;
                MusicData.MusicInfo.TagTitle = tags.Title;
                MusicData.MusicInfo.TagComment = tags.Comment;
                MusicData.MusicInfo.BPM = Convert.ToDouble(tags.BeatsPerMinute);
                
                //Some additional BPM tag stuff here  including trying to find a Is34TimingTag
                //Cant figure out what its looking for cause obfuscation
            }

            if (MusicData.MusicInfo.Duration < 10.0)
            {
                MusicData = null;
                LastError = "Music file is too short";
                LastErrorCode = "ERR_TOO_SHORT";
                return false;
            }
            
            //Now mixing if not mono or frequency mismatch
            if (_notMonoOrFrequencyMismatch)
            {
                _mixStream = BassMix.CreateMixerStream(44100, 1, BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
                if (_mixStream == 0)
                {
                    MusicData = null;
                    LastError = "Could not create mixer stream";
                    LastErrorCode = Bass.LastError.ToString();
                    FreeChannel(ref _decodeStream);
                    FreeChannel(ref _mixStream);
                    return false;
                }

                if (!BassMix.MixerAddChannel(_mixStream, _decodeStream, BassFlags.RecordEchoCancel))
                {
                    MusicData = null;
                    LastError = "Could not add channel to mixer stream";
                    LastErrorCode = Bass.LastError.ToString();
                    FreeChannel(ref _decodeStream);
                    FreeChannel(ref _mixStream);
                    return false;
                }
                //getting channelinfo here but not used anywhere
            }

            _otherStream = Bass.CreateStream(44100, _weirdChannelValue, BassFlags.Float | BassFlags.Decode,
                _streamProcedure, IntPtr.Zero);
            return true;
        }

        private Tag GetTagsFromFile(string file)
        {
            try
            {
                var tags = File.Create(file);
                return tags.Tag;
            }
            catch (Exception e)
            {
                Debug.Log("Error while getting tags from file: " + e);
                return null;
            }
        }

        public override void AnalyzeMusicFile()
        {
            try
            {
                if (string.IsNullOrEmpty(MusicFile))
                {
                    throw new Exception("No music file specified");
                }
                 
                _analyzeStopwatch.Stop();
                _analyzeStopwatch.Reset();
                _analyzeStopwatch.Start();
                
                var elapsedMilliseconds = _analyzeStopwatch.ElapsedMilliseconds;
                GetFFTData();
                FreeMusicFile();
                //TODO: Get methods from reflection properly, needs arg checking
                //MusicData.f();
                //MusicData.G();
                //MusicData.c();
                if (MReactor.EnableHeldNoteDetection)
                {
                    //MusicData.h();
                }
                
                //MusicData.A(MReactor.CurrentDifficultyRules);
                _analyzeStopwatch.Stop();
                MusicData.MusicInfo.AnalysisTime = _analyzeStopwatch.Elapsed;
                MusicData.OnMusicAnalyzed();
                
            }
            catch (Exception e)
            {
                MusicData.OnMusicAnalysisException(e.Message);
            }
        }

        public override void FreeMusicFile()
        {
            FreeChannel(ref _decodeStream);
            FreeChannel(ref _mixStream); //idk
            FreeChannel(ref _otherStream); //idk
            //some other channel instead of maybe themix channel
        }

        public override void GetFFTData()
        {
            //b();
        }
    }
}