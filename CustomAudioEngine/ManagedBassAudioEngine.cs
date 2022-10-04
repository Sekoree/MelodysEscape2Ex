using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ManagedBass;
using ManagedBass.Mix;
using MelodyReactor2;
using Un4seen.Bass.AddOn.Tags;
using Debug = UnityEngine.Debug;

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
        private int _liveStreamChannel = 0;

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
        private long _exactPausePosition;

        private int d_someValue;
        private int D_someValue;
        private float A_someValue;
        private float a_someValue;
        private long a_someLongValue;
        private int e_someValue;
        private int F_someValue;

        //No fucking idea why window handle is needed
        public ManagedBassAudioEngine(IntPtr mainWindowHandle)
        {
            Debug.Log("Ctor ManagedBassAudioEngine");
            _windowHandle = mainWindowHandle;
            Debug.Log("About to create DSPProcedure");
            //_dspProcedure = new DSPProcedure(_dspProcedure);
            Debug.Log("About to create StreamProcedure");
            //_streamProcedure = new StreamProcedure(_streamProcedure);
            Debug.Log("About to check FFTQuality");
            if (MReactor.FFTQuality < 1024) //seems to be 4096 by default
                MReactor.FFTQuality = 1024;
            Debug.Log("About to get weirdChannelValue");
            _weirdChannelValue = 2;//MReactor.FFTQuality / 1024;
            Debug.Log("About to get fftDataTimes100");
            _fftDataTimes100 = new float[MReactor.FFTQuality * 100];
            Debug.Log("About to get fftData");
            _fftData = new float[MReactor.FFTQuality];
            Debug.Log("About to get fftDataByte");
            _fftDataByte = new byte[MReactor.FFTQuality / 2];
            Debug.Log("About to get fftDataTimesFactor");
            _fftDataTimesFactor = new float[MReactor.FFTQuality / 2 * _weirdChannelValue];

            Debug.Log("Ctor before flags and weird value");
            
            _bassFlags = -2147483644;
            _someOtherRandomValue |= 16;
            Debug.Log("Ctor ManagedBassAudioEngine done");
        }

        public void FreeChannel(ref int channel)
        {
            Debug.Log("FreeChannel with channel " + channel);
            if (channel != 0)
            {
                Bass.StreamFree(channel);
            }

            channel = 0;
        }

        public List<string> GetPluginFiles(string directoryPath, string searchPattern, SearchOption searchOption)
        {
            Debug.Log("GetPluginFiles with directoryPath " + directoryPath + " searchPattern " + searchPattern + " searchOption " + searchOption);
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
            Debug.Log("GetPluginFiles done");
            return pluginFiles;
        }

        public override void SetPluginsDirectory(string path)
        {
            _pluginDirectory = path;
        }

        public override void Initialize()
        {
            Debug.Log("Initialize");
            base.Initialize();
            if (LOAD_PLUGINS)
            {
                if (Directory.Exists(_pluginDirectory))
                {
                    Debug.Log("Loading plugins from " + _pluginDirectory);
                    List<string> pluginFiles = GetPluginFiles(_pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
                    foreach (string pluginFile in pluginFiles)
                    {
                        Debug.Log("Loading plugin " + pluginFile);
                        Bass.PluginLoad(pluginFile);
                    }
                    Debug.Log("Loading plugins done");
                }
            }

            SupportedFilesExtensions = Bass.SupportedFormats;
            int num = 0;
            bool couldInitBass = false;
            Errors bassError = Errors.OK;
            //Some buffer stuff here that is disabled

            Debug.Log("Initializing bass");
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

            if (!couldInitBass)
            {
                throw new AudioEngineInitializationException("Bass committed cringe and could not be initialized: " +
                                                             bassError);
            }

            Debug.Log("Initializing bass done");
            Bass.Configure(Configuration.UpdatePeriod, MReactor.UseOwnThread ? 5 : 0);
            Bass.Configure(Configuration.PlaybackBufferLength, 250);
            Debug.Log("Initialize done");
            //USE_CONFIG_DEVICE_BUFFER is always false
        }

        public override void CleanRessources()
        {
            base.CleanRessources();
        }

        public override bool IsReadyToAnalyze()
        {
            Debug.Log("IsReadyToAnalyze");
            return _decodeStream != 0;
        }

        public override void InitAndAnalyzeMusicFileAsync()
        {
            Console.WriteLine("InitAndAnalyzeMusicFileAsync about to start Thread");
            _initAndAnalyzeThread = new Thread(new ThreadStart(InitAndAnalyzeMusicFileAsyncThread));
            _initAndAnalyzeThread.Start();
        }

        public override void AnalyzeMusicFileAsync()
        {
            Console.WriteLine("AnalyzeMusicFileAsync about to start Thread");
            _initAndAnalyzeThread = new Thread(new ThreadStart(AnalyzeMusicFileAsyncThread));
            _initAndAnalyzeThread.Start();
        }

        public void InitAndAnalyzeMusicFileAsyncThread()
        {
            Console.WriteLine("InitAndAnalyzeMusicFileAsyncThread");
            if (!MReactor.AudioEngine.InitMusicFile())
                return;
            Debug.Log("InitAndAnalyzeMusicFileAsyncThread about to call AnalyzeMusicFileAsyncThread");
            MReactor.AudioEngine.AnalyzeMusicFile();
        }

        public void AnalyzeMusicFileAsyncThread()
        {
            Debug.Log("AnalyzeMusicFileAsyncThread: Doing Thread Stuff");
            MReactor.AudioEngine.AnalyzeMusicFile();
        }

        public override bool InitMusicFile()
        {
            Debug.Log("InitMusicFile");
            base.InitMusicFile();
            Debug.Log("InitMusicFile base done");
            if (string.IsNullOrEmpty(MusicFile))
            {
                LastError = "No music file specified";
                Debug.Log("InitMusicFile No music file specified");
                return false;
            }

            Debug.Log("InitMusicFile about to get file info");
            MusicData.MusicInfo = new MusicInfo();
            MusicData.MusicInfo.FileFullPath = MusicFile;
            MusicData.MusicInfo.Filename = Path.GetFileName(MusicFile);
            MusicData.MusicInfo.FilenameWithoutExt = Path.GetFileNameWithoutExtension(MusicFile);

            d_someValue = 0;
            D_someValue = 0;
            a_someLongValue = 0L;
            A_someValue = 0.0f;
            Debug.Log("Creating decode stream");
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
            Debug.Log("Getting TagInfo");
            //man IDK, too lazy to replicate this monster of a function
            TAG_INFO fromFile = Un4seen.Bass.AddOn.Tags.BassTags.BASS_TAG_GetFromFile(this.MusicFile);
            var channelInfo = new ChannelInfo();
            Debug.Log("Getting channel info");
            Bass.ChannelGetInfo(_decodeStream, out channelInfo);
            _notMonoOrFrequencyMismatch = channelInfo.Channels > 1 ||
                                          BassAudioEngine.FORCE_44100_DECODING && channelInfo.Frequency != 44100;
            Debug.Log("Getting length");
            _decodeChannelLength = Bass.ChannelGetLength(_decodeStream);
            var asSeconds = Bass.ChannelBytes2Seconds(_decodeStream, _decodeChannelLength);
            Debug.Log("Seconds: " + asSeconds);
            try
            {
                //get internal method "a" from MusicData with reflection
                var methods = typeof(MusicData).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

                Debug.Log("Getting MusicData.a");
                //find method that has 3 arguments being int, int and double
                var aMethod = methods.Where(x => x.Name == "a")
                    .First(m =>
                        m.GetParameters().Length == 3 &&
                        m.GetParameters()[0].ParameterType == typeof(int) &&
                        m.GetParameters()[1].ParameterType == typeof(int) &&
                        m.GetParameters()[2].ParameterType == typeof(double));

                Debug.Log("Invoking MusicData.a");
                //Idk if this will work cause this is obfuscation pain
                aMethod.Invoke(MusicData,
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

            Debug.Log("Setting MusicData.MusicInfo");
            MusicData.MusicInfo.SampleRate = channelInfo.Frequency;
            if (fromFile != null)
            {
                Debug.Log("Setting MusicData.MusicInfo tags");
                MusicData.MusicInfo.TagAlbum = fromFile.album;
                MusicData.MusicInfo.TagArtist = fromFile.artist;
                MusicData.MusicInfo.TagTitle = fromFile.title;
                MusicData.MusicInfo.TagComment = fromFile.comment;
                try
                {
                    MusicData.MusicInfo.BPM = Convert.ToDouble(fromFile.bpm, CultureInfo.InvariantCulture);
                }
                finally
                {
                    MusicData.MusicInfo.BPM = 0.0;
                }

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
            Debug.Log("Mixing if not mono or frequency mismatch: " + _notMonoOrFrequencyMismatch);
            if (_notMonoOrFrequencyMismatch)
            {
                Debug.Log("Mixing");
                _mixStream =
                    BassMix.CreateMixerStream(44100, 1, BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
                if (_mixStream == 0)
                {
                    MusicData = null;
                    Debug.Log("Could not create mixer stream");
                    LastError = "Could not create mixer stream";
                    LastErrorCode = Bass.LastError.ToString();
                    FreeChannel(ref _decodeStream);
                    FreeChannel(ref _mixStream);
                    return false;
                }

                Debug.Log("Adding decode stream to mixer");
                if (!BassMix.MixerAddChannel(_mixStream, _decodeStream, BassFlags.RecordEchoCancel))
                {
                    MusicData = null;
                    Debug.Log("Could not add channel to mixer stream");
                    LastError = "Could not add channel to mixer stream";
                    LastErrorCode = Bass.LastError.ToString();
                    FreeChannel(ref _decodeStream);
                    FreeChannel(ref _mixStream);
                    return false;
                }
                //getting channelinfo here but not used anywhere
            }

            Debug.Log("Creating other stream");
            _otherStream = Bass.CreateStream(44100, _weirdChannelValue, BassFlags.Float | BassFlags.Decode,
                _streamProcedure, IntPtr.Zero);
            return true;
        }

        public override void AnalyzeMusicFile()
        {
            Debug.Log("AnalyzeMusicFile: Arrived here");
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
                Debug.Log("About to GetFFTData");
                GetFFTData();
                Debug.Log("About to FreeMusicFile");
                FreeMusicFile();
                //TODO: Get methods from reflection properly, needs arg checking

                //get internal methods f, G, c, h from MusicData with reflection
                var methods = typeof(MusicData).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

                try
                {
                    //get method f with no args
                    var fMethod = methods.Where(x => x.Name == "f").First(m => m.GetParameters().Length == 0);
                    //get method G with no args
                    var gMethod = methods.Where(x => x.Name == "G").First(m => m.GetParameters().Length == 0);
                    //get method c with no args
                    var cMethod = methods.Where(x => x.Name == "c").First(m => m.GetParameters().Length == 0);

                    //invoke methods
                    Debug.Log("Invoking MusicData.f");
                    fMethod.Invoke(MusicData, null);
                    Debug.Log("Invoking MusicData.G");
                    gMethod.Invoke(MusicData, null);
                    Debug.Log("Invoking MusicData.c");
                    cMethod.Invoke(MusicData, null);
                }
                catch (Exception e)
                {
                    Debug.Log("Error while invoking MusicData.f, MusicData.G or MusicData.c: " + e);
                    throw new Exception("Error while invoking MusicData.f, MusicData.G or MusicData.c: " + e);
                }

                if (MReactor.EnableHeldNoteDetection)
                {
                    try
                    {
                        var hMethod = methods.Where(x => x.Name == "h").First(m => m.GetParameters().Length == 0);
                        Debug.Log("Invoking MusicData.h");
                        hMethod.Invoke(MusicData, null);

                        //MusicData.h();
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Error while invoking MusicData.h: " + e);
                        throw new Exception("Error while invoking MusicData.h: " + e);
                    }
                }

                try
                {
                    //get "A" method from MusicData with reflection with 1 arg of type DifficultyRules
                    var aMethod = methods.Where(x => x.Name == "A")
                        .First(m => m.GetParameters().Length == 1 &&
                                    m.GetParameters()[0].ParameterType == typeof(DifficultyRules));

                    //invoke method with DifficultyRules
                    Debug.Log("Invoking MusicData.A");
                    aMethod.Invoke(MusicData, new object[] { MReactor.CurrentDifficultyRules });
                    //MusicData.A(MReactor.CurrentDifficultyRules);
                }
                catch (Exception e)
                {
                    Debug.Log("Error while invoking MusicData.A: " + e);
                    throw new Exception("Error while invoking MusicData.A: " + e);
                }

                _analyzeStopwatch.Stop();
                MusicData.MusicInfo.AnalysisTime = _analyzeStopwatch.Elapsed;
                Debug.Log("AnalyzeMusicFile: Finished, calling OnMusicAnalyzed");
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
            //some other channel instead of maybe the mix channel
        }

        public override void GetFFTData()
        {
            Debug.Log("GetFFTData start, setting position");
            Bass.ChannelSetPosition(_notMonoOrFrequencyMismatch ? _mixStream : _decodeStream, 0L);
            d_someValue = 0;
            a_someValue = 0.0f;
            e_someValue = 0;
            F_someValue = 0;
            Debug.Log("Triggering OnLoadingStatusChanged");
            MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT);
            var num = 0;
            PointF decibelsRange1 = MReactor.DECIBELS_RANGE;
            PointF decibelsRange2 = MReactor.DECIBELS_RANGE;

            //get internal static int field A from MReactor with reflection
            Debug.Log("Getting MReactor.As");
            var fields = typeof(MReactor).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
            
            Debug.Log("Getting MReactor.A int");
            var bigAField = fields.First(x => x.Name == "A" && x.FieldType == typeof(int));

            //get internal static byte[] field A from MReactor with reflection
            Debug.Log("Getting MReactor.A byte[]");
            var bigAByteArrayField = fields.First(x => x.Name == "A" && x.FieldType == typeof(byte[]));
            var bigAByteArray = (byte[])bigAByteArrayField.GetValue(null);

            var bigAValue = (int)bigAField.GetValue(null);
            int num3 = 0;

            _fftDataByte[0] = 0;
            _fftDataByte[1] = 0;

            //this.A is the byte array!!
            while (Bass.ChannelGetData(_otherStream, _fftData, _bassFlags) > 0)
            {
                //Debug.Log("Bass.ChannelGetData returned > 0");
                for (int i = 0; i < _weirdChannelValue; i++)
                {
                    //Debug.Log("Looping through weird channel value");
                    byte b = 0;
                    for (int j = 0; j < bigAValue; j++)
                    {
                        //Debug.Log("Looping through bigAValue");
                        float num4 = _fftData[j * _weirdChannelValue + i];
                        //Debug.Log("num4 = " + num4);
                        if (num4 < MReactor.DECIBEL_MIN_VALUE)
                        {
                            _fftDataByte[j] = 0;
                            continue;
                        }

                        //Debug.Log("num4 = " + num4);
                        if (num4 > 0.9999f)
                        {
                            _fftDataByte[j] = 254;
                            b = 254;
                            continue;
                        }

                        //Debug.Log("num4 = " + num4);
                        byte b2 = bigAByteArray[(int)(num4 * 1000000f)];
                        _fftDataByte[j] = b2;
                        if (b2 > b)
                        {
                            b = b2;
                        }
                    }

                    //Debug.Log("About to copy array");
                    Array.Copy(_fftDataByte, MusicData.Samples[num3].FFT, MReactor.FFTArrayLimitedSize);
                    MusicData.Samples[num3].MaxFFTBandValue = b;
                    num3++;
                    if (num3 >= MusicData.MusicInfo.TotalSamples)
                    {
                        break;
                    }

                    int num5 = (int)Math.Round((double)(num3 + 1) / (double)MusicData.MusicInfo.TotalSamples * 100.0);
                    if (num5 != num)
                    {
                        num = num5;
                        MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT, num5);
                    }
                }

                if (num3 >= MusicData.MusicInfo.TotalSamples)
                {
                    break;
                }
            }

            bigAByteArrayField.SetValue(null, bigAByteArray);
            MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT, 100);
            B_ObfuscatedMethod();
        }

        private void B_ObfuscatedMethod()
        {
            if (a_someValue <= 0.005f)
            {
                a_someValue = 1f;
            }
            else if (a_someValue <= 0.05f)
            {
                a_someValue = 0.05f;
            }
            else if (a_someValue > 0.99f)
            {
                a_someValue = 1f;
            }

            MusicData.MusicInfo.PCMNormalizationFactor = 1f / a_someValue;
            if (MusicData.MusicInfo.PCMNormalizationFactor > 5f)
            {
                MusicData.MusicInfo.PCMNormalizationFactor = 5f;
            }

            if (MusicData.MusicInfo.PCMNormalizationFactor < 1.1f)
            {
                MusicData.MusicInfo.PCMNormalizationFactor = 1f;
            }
        }

        public override bool LoadMusicFile()
        {
            if (string.IsNullOrWhiteSpace(MusicFile))
            {
                LastError = "No music file specified";
                return false;
            }

            if (_liveStreamChannel != 0)
            {
                Bass.ChannelStop(_liveStreamChannel);
                FreeChannel(ref _liveStreamChannel);
            }

            var usedFlags = BassFlags.Float | BassFlags.Prescan;
            _liveStreamChannel = Bass.CreateStream(MusicFile, 0L, 0L, usedFlags);
            if (_liveStreamChannel == 0)
            {
                LastError = "Failed to create stream";
                LastErrorCode = Bass.LastError.ToString();
                return false;
            }

            if (MReactor.EnablePlaybackNormalization && MusicData.MusicInfo.PCMNormalizationFactor > 1f)
            {
                Bass.ChannelSetDSP(_liveStreamChannel, _dspProcedure, IntPtr.Zero, 0);
            }

            D_ObfuscatedMethod();
            return true;
        }

        public void MuteLiveSong()
        {
            if (_liveStreamChannel != 0)
            {
                var currentVolume = Bass.ChannelGetAttribute(_liveStreamChannel, ChannelAttribute.Volume);
                Bass.ChannelSetAttribute(_liveStreamChannel, ChannelAttribute.Volume, (currentVolume != 0f) ? 0f : 1f);
            }
        }

        public override bool PlayMusicFile()
        {
            if (_liveStreamChannel == 0)
            {
                LastError = "No stream to play";
                D_ObfuscatedMethod();
                return false;
            }

            if (Bass.ChannelPlay(_liveStreamChannel, true))
            {
                D_ObfuscatedMethod();
                return true;
            }

            LastErrorCode = Bass.LastError.ToString();
            //TODO: additional check here if buffer was lost in OG method
            LastError = "Failed to play stream";
            return false;
        }

        public override void PauseLiveStream()
        {
            if (_liveStreamChannel == 0)
            {
                return;
            }

            D_ObfuscatedMethod();
            if (MusicData.MusicInfo.State == SongState.Playing)
            {
                if (FORCE_PAUSE_EXACT_POSITION)
                {
                    _exactPausePosition = Bass.ChannelGetPosition(_liveStreamChannel);
                }

                Bass.ChannelPause(_liveStreamChannel);
                if (FORCE_PAUSE_EXACT_POSITION)
                {
                    Bass.ChannelSetPosition(_liveStreamChannel, _exactPausePosition);
                }

                MusicData.MusicInfo.State = SongState.Paused;
            }
        }

        public override void ResumeLiveStream()
        {
            if (_liveStreamChannel == 0)
            {
                return;
            }

            D_ObfuscatedMethod();
            if (MusicData.MusicInfo.State == SongState.Paused)
            {
                Bass.ChannelPlay(_liveStreamChannel, false);
                MusicData.MusicInfo.State = SongState.Playing;
            }
        }

        public override void StopLiveStream()
        {
            if (_liveStreamChannel == 0)
            {
                return;
            }

            if (MusicData != null && MusicData.MusicInfo.State != SongState.Stopped)
            {
                Bass.ChannelStop(_liveStreamChannel);
                FreeChannel(ref _liveStreamChannel);
                MusicData.MusicInfo.State = SongState.Stopped;
                MReactor.ClearRealTimeData();
            }
        }

        public override void UnloadLiveStream()
        {
            Bass.ChannelStop(_liveStreamChannel);
            FreeChannel(ref _liveStreamChannel);
            MusicData.MusicInfo.State = SongState.Stopped;
            MReactor.ClearRealTimeData();
        }

        public override void ResetLiveStream()
        {
            if (_liveStreamChannel == 0)
                return;

            Bass.ChannelPlay(_liveStreamChannel, true);
            D_ObfuscatedMethod();
        }

        public override void Update(float elapsedSeconds)
        {
            if (MusicData == null || MusicData.MusicInfo == null)
                return;

            var num = (int)(elapsedSeconds * 2000f);
            if (num < 10)
            {
                num = 10;
            }

            if (!MReactor.UseOwnThread)
            {
                Bass.Update(num);
            }

            D_ObfuscatedMethod();
            if (MReactor.EnableRealTimeFrequencyData && MusicData.MusicInfo.State == SongState.Playing)
            {
                UpdateRealTimeFrequencyData();
            }
        }

        public override void UpdateRealTimeFrequencyData()
        {
            if (MusicData.MusicInfo.State != 0 && MusicData.MusicInfo.State != SongState.Paused)
            {
                return;
            }

            //get internal static int field A from MReactor with reflection
            var fields = typeof(MReactor).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
            var bigAField = fields.First(x => x.Name == "A" && x.FieldType == typeof(int));

            //get internal static byte[] field A from MReactor with reflection
            var bigAByteArrayField = fields.First(x => x.Name == "A" && x.FieldType == typeof(byte[]));
            var bigAByteArray = (byte[])bigAByteArrayField.GetValue(null);
            var bigAValue = (int)bigAField.GetValue(null);

            Bass.ChannelGetData(_liveStreamChannel, MReactor.RealTimeFFTData, -2147483644);
            for (int i = 2; i < bigAValue; i++)
            {
                float num = MReactor.RealTimeFFTData[i];
                if (num < MReactor.DECIBEL_MIN_VALUE)
                {
                    MReactor.RealTimeByteFFTData[i] = 0;
                }
                else if (num > 0.9999f)
                {
                    MReactor.RealTimeByteFFTData[i] = 254;
                }
                else
                {
                    MReactor.RealTimeByteFFTData[i] = bigAByteArray[(int)(num * 1000000f)];
                }
            }

            bigAByteArrayField.SetValue(null, bigAByteArray);
            for (int j = 0; j < 7; j++)
            {
                int x = MReactor.OctaveBinsRanges[j].X;
                int y = MReactor.OctaveBinsRanges[j].Y;
                float num2 = 0f;
                int num3 = y - x + 1;
                for (int k = x; k <= y; k++)
                {
                    float num4 = MReactor.ByteToFloatCache[MReactor.RealTimeByteFFTData[k]];
                    num2 += num4;
                }

                float num5 = num2 / (float)num3;
                MReactor.RealTimeOctaveFFTDataAll[j] = num5;
            }

            float[] octavesBinsLevelFactor = MusicSample.OctavesBinsLevelFactor;
            float num6 = Math.Max(MReactor.RealTimeOctaveFFTDataAll[0] * octavesBinsLevelFactor[0],
                MReactor.RealTimeOctaveFFTDataAll[1] * octavesBinsLevelFactor[1]);
            float num7 = MReactor.RealTimeOctaveFFTDataAll[2] * octavesBinsLevelFactor[2];
            float num8 = Math.Max(MReactor.RealTimeOctaveFFTDataAll[3] * octavesBinsLevelFactor[3],
                MReactor.RealTimeOctaveFFTDataAll[4] * octavesBinsLevelFactor[4]);
            float num9 = Math.Max(MReactor.RealTimeOctaveFFTDataAll[5] * octavesBinsLevelFactor[5],
                MReactor.RealTimeOctaveFFTDataAll[6] * octavesBinsLevelFactor[6]);
            MReactor.RealTimeOctaveFFTData[0] = num6;
            MReactor.RealTimeOctaveFFTData[1] = num7;
            MReactor.RealTimeOctaveFFTData[2] = num8;
            MReactor.RealTimeOctaveFFTData[3] = num9;
            float num10 = 0f;
            for (int l = 0; l < 4; l++)
            {
                float num11 = MReactor.RealTimeOctaveFFTData[l] * MusicSample.MergedOctavesBinsLevelFactor[l];
                num10 += num11;
            }

            num10 /= MusicSample.MergedOctavesBinsLevelFactorSum;
            MReactor.RealTimeLevel = num10;
        }

        public override void Unload()
        {
            Bass.Stop();
            Bass.Free();
        }

        public override void SetPosition(double seconds)
        {
            Bass.ChannelSetPosition(_liveStreamChannel, Bass.ChannelSeconds2Bytes(_liveStreamChannel, seconds),
                PositionFlags.Bytes);
            D_ObfuscatedMethod();
        }

        public override void ResetPosition()
        {
            Bass.ChannelSetPosition(_liveStreamChannel, 0L);
            D_ObfuscatedMethod();
        }

        public override void SetVolume(float volume)
        {
            Bass.Configure(Configuration.GlobalStreamVolume, (int)(volume * 10000f));
        }

        public override float GetVolume()
        {
            return (float)Bass.GetConfig(Configuration.GlobalStreamVolume) / 10000f;
        }

        public override void SetPlaybackSpeed(float speedFactor)
        {
            Bass.ChannelSetAttribute(_liveStreamChannel, ChannelAttribute.Frequency,
                (float)MusicData.MusicInfo.SampleRatePlayback * speedFactor);
        }

        /// <summary>
        /// Probably "UpdateSongState"
        /// </summary>
        private void D_ObfuscatedMethod()
        {
            var state = GetLiveStreamState();
            if (MusicData != null)
            {
                MusicData.MusicInfo.State = state;
                d_ObfuscatedMethod();
            }
        }

        /// <summary>
        /// Probably "UpdateSamplePosition"
        /// </summary>
        private void d_ObfuscatedMethod()
        {
            var position = Bass.ChannelGetPosition(_liveStreamChannel);
            MusicData.MusicInfo.Position = Bass.ChannelBytes2Seconds(_liveStreamChannel, position);
            if (MusicData.MusicInfo.Position < 0.0)
            {
                MusicData.MusicInfo.Position = 0.0;
            }

            var num = MusicData.MusicInfo.Position * MusicData.MusicInfo.SamplesPerSeconds;
            var num2 = (float)(num - Math.Truncate(num));
            if (num2 < 0f)
            {
                num2 = 0f;
            }

            if (num2 > 0.999f)
            {
                num2 = 0.999f;
            }

            MusicData.MusicInfo.CurrentSampleID = MusicData.SecondsToSamples(MusicData.MusicInfo.Position, false);
            if (MusicData.MusicInfo.CurrentSampleID < 0)
            {
                MusicData.MusicInfo.CurrentSampleID = 0;
                num = 0.0;
                num2 = 0f;
            }

            if (MusicData.MusicInfo.CurrentSampleID >= MusicData.MusicInfo.TotalSamples)
            {
                MusicData.MusicInfo.CurrentSampleID = MusicData.MusicInfo.TotalSamples - 1;
                num = MusicData.MusicInfo.CurrentSampleID;
                num2 = 0f;
            }

            MusicData.MusicInfo.CurrentSamplePosition = num;
            MusicData.MusicInfo.CurrentInSampleProgress = num2;
        }

        public override SongState GetLiveStreamState()
        {
            if (_liveStreamChannel == 0)
            {
                return SongState.Stopped;
            }

            switch (Bass.ChannelIsActive(_liveStreamChannel))
            {
                case PlaybackState.Paused:
                    return SongState.Paused;
                case PlaybackState.Playing:
                case PlaybackState.Stalled:
                    return SongState.Playing;
                default:
                    return SongState.Stopped;
            }
        }
    }
}