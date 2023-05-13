using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ManagedBass;
using ManagedBass.Mix;
using MelodyReactor2;
using File = TagLib.File;

namespace CustomAudioEngine
{
    public class CustomAudioEngine : AudioEngine
    {
        public static bool USE_OLD_GETFFTDATA = false;
        public static bool LOAD_PLUGINS = true;
        public static bool FORCE_44100_DECODING = true;
        public static bool USE_CONFIG_DEVICE_BUFFER = false;
        public static bool USE_FFT_HANN_WINDOW = true;
        public static bool FORCE_PAUSE_EXACT_POSITION = true;

        #region Unknown Fields

        /// <summary>
        /// m_A_int
        /// </summary>
        private const int m_A_int = 100;

        /// <summary>
        /// m_A_Thread
        /// </summary>
        private Thread m_A_Thread;

        /// <summary>
        /// m_a_int
        /// </summary>
        private int _decodingStream;

        /// <summary>
        /// m_B_int
        /// </summary>
        private int _mixedDecodeStream;

        /// <summary>
        /// m_A_bool
        /// </summary>
        private bool _isNotMonoOrNot44100;

        /// <summary>
        /// m_b_int
        /// </summary>
        private int _mixerStream;

        /// <summary>
        /// m_C_int
        /// </summary>
        private int _playbackStream;

        /// <summary>
        /// m_A_long
        /// </summary>
        private long m_A_long;

        /// <summary>
        /// m_A_Stopwatch
        /// </summary>
        private Stopwatch _analyzeStopwatch = new Stopwatch();

        /// <summary>
        /// m_A_nint
        /// </summary>
        private IntPtr _windowHandle = IntPtr.Zero;

        /// <summary>
        /// m_A_string
        /// </summary>
        private string _pluginsPath = "Idk, obfuscated"; //C5C9ABC1-2D8E-4B13-8B77-D687194E8BFD.A();

        /// <summary>
        /// m_A_byteArray
        /// </summary>
        private byte[] m_A_byteArray;

        /// <summary>
        /// m_A_floatArray
        /// </summary>
        private float[] m_A_floatArray;

        /// <summary>
        /// m_c_int
        /// </summary>
        private int m_c_int;

        /// <summary>
        /// m_A_DSPProcedure
        /// </summary>
        private DSPProcedure m_A_DSPProcedure;

        /// <summary>
        /// m_A_StreamProcedure
        /// </summary>
        private StreamProcedure m_A_StreamProcedure;

        /// <summary>
        /// m_A_float
        /// </summary>
        private float m_A_float;

        /// <summary>
        /// m_a_floatArray
        /// </summary>
        private float[] m_a_floatArray;

        /// <summary>
        /// m_B_floatArray
        /// </summary>
        private float[] m_B_floatArray;

        /// <summary>
        /// m_a_long
        /// </summary>
        private long m_a_long;

        /// <summary>
        /// m_D_int
        /// </summary>
        private int m_D_int;

        /// <summary>
        /// m_d_int
        /// </summary>
        private int m_d_int;

        /// <summary>
        /// m_E_int
        /// </summary>
        private readonly int E_int;

        /// <summary>
        /// m_B_long
        /// </summary>
        private long _exactPlaybackPosition;

        /// <summary>
        /// e_int
        /// </summary>
        private int e_int;

        /// <summary>
        /// F_int
        /// </summary>
        private int F_int;

        /// <summary>
        /// m_a_float
        /// </summary>
        private float m_a_float;

        #endregion

        public CustomAudioEngine(IntPtr windowHandle)
        {
            Console.WriteLine("CustomAudioEngine constructor called");

            Console.WriteLine("Is MReactor_A_int null? " + (MReactor_A_int == null));
            Console.WriteLine("Is MReactor_A_byteArray null? " + (MReactor_A_byteArray == null));
            
            this._windowHandle = windowHandle;
            this.m_A_DSPProcedure = this.DspProc;
            this.m_A_StreamProcedure = this.StreamProc;
            if (MReactor.FFTQuality < 1024)
            {
                MReactor.FFTQuality = 1024;
            }

            E_int = MReactor.FFTQuality / 1024;
            this.m_a_floatArray = new float[MReactor.FFTQuality * 100];
            this.m_B_floatArray = new float[MReactor.FFTQuality];
            this.m_A_byteArray = new byte[MReactor.FFTQuality / 2];
            this.m_A_floatArray = new float[MReactor.FFTQuality / 2 * E_int];
            this.m_c_int = GetBassFlags(MReactor.FFTQuality);
            if (!USE_FFT_HANN_WINDOW)
            {
                this.m_c_int |= 32;
            }
            this.m_c_int |= 16;
        }

        private int GetBassFlags(int fftQuality)
        {
            switch (fftQuality)
            {
                case 256:
                    return int.MinValue;
                case 512:
                    return -2147483647;
                case 1024:
                    return -2147483646;
                case 2048:
                    return -2147483645;
                case 4096:
                    return -2147483644;
                case 16384:
                    return -2147483642;
                default:
                    return -2147483643;
            }
        }
        
        public void FreeChannel(ref int channel)
        {
            if (channel != 0)
            {
                Bass.StreamFree(channel);
            }
            channel = 0;
        }

        private unsafe int StreamProc(int P_0, IntPtr P_1, int P_2, IntPtr P_3)
        {
            if (P_2 == 0 || P_1 == IntPtr.Zero)
            {
                return 0;
            }

            float* ptr = (float*)(void*)P_1;
            int num = P_2 / 4 / E_int;
            int num2 = num / E_int;
            int num3 = this.m_B_floatArray.Length;
            int num4 = this.m_a_floatArray.Length - num3;
            double num5 = 1024.0;
            int num6 = 2;
            bool flag = false;
            if (this.m_D_int <= 0 || this.m_d_int >= num4)
            {
                _ = this.m_a_long; //wat
                if (this.m_D_int > 0)
                {
                    Array.Copy(this.m_a_floatArray, num4, this.m_B_floatArray, 0, num3);
                    flag = true;
                }

                this.m_d_int = 0;
                int num7 = Bass.ChannelGetData(this._isNotMonoOrNot44100 ? this._mixedDecodeStream : this._decodingStream, this.m_a_floatArray,
                    this.m_a_floatArray.Length * 4);
                this.m_D_int = num7 / 4;
                this.m_a_long += this.m_D_int;
                for (int i = 0; i < this.m_D_int; i++)
                {
                    float num8 = this.m_a_floatArray[i];
                    if (num8 < 0f)
                    {
                        num8 = 0f - num8;
                    }

                    if (num8 > 1f)
                    {
                        num8 = 1f;
                    }

                    this.m_A_float += num8;
                    e_int++;
                    if (e_int < 1024)
                    {
                        continue;
                    }

                    float num9 = (float)((double)this.m_A_float / num5);
                    if (F_int >= num6 && F_int - num6 < MusicData.MusicInfo.TotalSamples)
                    {
                        if (num9 < 0.001f)
                        {
                            num9 = 0f;
                        }

                        if (num9 > 1f)
                        {
                            num9 = 1f;
                        }

                        MusicData.RawPCMLevels[F_int - num6] = (byte)(num9 * 255f);
                    }

                    F_int++;
                    e_int = 0;
                    this.m_A_float = 0f;
                }
            }

            float num10 = 0f;
            int num11 = 0;
            for (int j = 0; j < num; j++)
            {
                int num12 = j * E_int;
                for (int k = 0; k < E_int; k++)
                {
                    int num13 = this.m_d_int + j + num2 * k;
                    if (num13 < this.m_D_int)
                    {
                        num10 = (ptr[num12 + k] = ((!flag)
                            ? this.m_a_floatArray[num13]
                            : ((num13 >= num3) ? this.m_a_floatArray[num13 - num3] : this.m_B_floatArray[num13])));
                        num11++;
                        if (num10 < 0f)
                        {
                            num10 = 0f - num10;
                        }

                        if (num10 > this.m_a_float)
                        {
                            this.m_a_float = num10;
                        }
                    }
                }
            }

            this.m_d_int += num;
            if (flag)
            {
                this.m_d_int = 0;
            }

            return num11 * 4;
        }

        private unsafe void DspProc(int P_0, int P_1, IntPtr P_2, int P_3, IntPtr P_4)
        {
            if (P_3 == 0 || P_2 == IntPtr.Zero)
                return;
            var num = P_3 / 4;
            var ptr = (float*)(void*)P_2;
            for (var i = 0; i < num; i++)
            {
                *ptr *= MusicData.MusicInfo.PCMNormalizationFactor;
                ptr++;
            }
        }

        public override void SetMusicFile(string fileName)
        {
            base.SetMusicFile(fileName);
        }

        public override void SetPluginsDirectory(string path)
        {
            this._pluginsPath = path;
        }

        public override void Initialize()
        {
            base.Initialize();
            if (LOAD_PLUGINS)
            {
                if (Directory.Exists(this._pluginsPath))
                {
                    var files = Directory.GetFiles(this._pluginsPath, "*.dll", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        var plugin = Bass.PluginLoad(file);
                        if (plugin != 0)
                        {
                            Console.WriteLine("Loaded plugin: " + file);
                            continue;
                        }

                        var error = Bass.LastError;
                        Console.WriteLine("Error loading plugin: " + file + " - " + error);
                    }
                }
                else
                {
                    Console.WriteLine("Plugins directory not found: " + this._pluginsPath);
                }
            }

            SupportedFilesExtensions =
                "*.mp3;*.ogg;*.wav;*.mp2;*.mp1;*.aiff;*.m2a;*.mpa;*.m1a;*.mpg;*.mpeg;*.aif;*.mp3pro;*.bwf;*.mus"; //From Bass.Net ILSpy
            var i = 0;
            var flag = false;
            var lastError = Errors.OK;
            if (USE_CONFIG_DEVICE_BUFFER)
            {
                Bass.Configure(Configuration.DeviceBufferLength, 80);
                Console.WriteLine("Bass device buffer length: " + Bass.GetConfig(Configuration.DeviceBufferLength));
            }

            Bass.Configure(Configuration.IncludeDefaultDevice, true);
            for (; i < 5; i++)
            {
                if (Bass.Init(Win: this._windowHandle))
                {
                    if (LOAD_PLUGINS)
                    {
                        Console.WriteLine(Bass.Info);
                    }

                    flag = true;
                    break;
                }

                lastError = Bass.LastError;
                if (lastError != Errors.Already)
                {
                    Bass.Free();
                }

                Thread.Sleep(1000);
            }

            if (!flag)
            {
                throw new AudioEngineInitializationException("Bass initialization failed: " + lastError);
            }

            Bass.Configure(Configuration.UpdatePeriod, MReactor.UseOwnThread ? 5 : 0);
            Bass.Configure(Configuration.PlaybackBufferLength, 250);
            if (USE_CONFIG_DEVICE_BUFFER)
            {
                Console.WriteLine("Bass device buffer length: " + Bass.GetConfig(Configuration.DeviceBufferLength));
            }
        }

        public override void CleanRessources()
        {
            base.CleanRessources();
        }

        public override bool IsReadyToAnalyze()
        {
            return this._decodingStream != 0;
        }

        public override void InitAndAnalyzeMusicFileAsync()
        {
            this.m_A_Thread = new Thread(InitAndAnalyzeMusicFileAsyncThread);
            this.m_A_Thread.Start();
        }

        public void InitAndAnalyzeMusicFileAsyncThread()
        {
            if (MReactor.AudioEngine.InitMusicFile(ignoreShortDurationError: false))
            {
                MReactor.AudioEngine.AnalyzeMusicFile();
            }
        }

        public override void AnalyzeMusicFileAsync()
        {
            this.m_A_Thread = new Thread(AnalyzeMusicFileAsyncThread);
            this.m_A_Thread.Start();
        }

        private void AnalyzeMusicFileAsyncThread()
        {
            MReactor.AudioEngine.AnalyzeMusicFile();
        }

        public override bool InitMusicFile(bool ignoreShortDurationError)
        {
            base.InitMusicFile(ignoreShortDurationError);
            if (this.MusicFile != null && string.IsNullOrEmpty(this.MusicFile))
            {
                this.LastError = "Music file is null or empty";
                return false;
            }

            var filePath = this.MusicFile;
            if (MReactor.AddSpecialPrefixToLongPathNames && filePath.Length > 260)
            {
                filePath = @"\\?\" + filePath; // I think thats correct for UNC stuff?
            }

            this.MusicData.MusicInfo = new MusicInfo()
            {
                FileFullPath = this.MusicFile,
                FileFullPathLongSafe = filePath,
                Filename = Path.GetFileName(this.MusicFile),
                FilenameWithoutExt = Path.GetFileNameWithoutExtension(this.MusicFile)
            };
            this.m_d_int = 0;
            this.m_D_int = 0;
            this.m_a_long = 0L;
            this.m_A_float = 0f;
            this._decodingStream = Bass.CreateStream(this.MusicFile, 0, 0,
                BassFlags.Mono | BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
            if (this._decodingStream == 0)
            {
                this.LastError = "Bass.CreateStream failed: " + Bass.LastError;
                return false;
            }

            var tags = TagLib.File.Create(filePath);
            var channelInfo = Bass.ChannelGetInfo(this._decodingStream);
            this._isNotMonoOrNot44100 = channelInfo.Channels > 1 || (FORCE_44100_DECODING && channelInfo.Frequency != 44100);
            //Tag reading method here
            ReadChannelTags(tags, this._isNotMonoOrNot44100, channelInfo);
            if (this.MusicData.MusicInfo.Duration < 10.0 && !ignoreShortDurationError)
            {
                this.LastError = "Music duration is too short: " + this.MusicData.MusicInfo.Duration;
                this.LastErrorCode = "SHORT_DURATION";
                return false;
            }

            if (this._isNotMonoOrNot44100)
            {
                this._mixerStream =
                    BassMix.CreateMixerStream(44100, 1, BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
                if (this._mixerStream == 0)
                {
                    this.MusicData = null;
                    this.LastError = "BassMix.CreateMixerStream failed: " + Bass.LastError;
                    this.LastErrorCode = "BASSMIX_CREATEMIXERSTREAM_FAILED: " + Bass.LastError;
                    FreeChannel(ref this._decodingStream);
                    FreeChannel(ref this._mixerStream);
                    return false;
                }

                if (!BassMix.MixerAddChannel(this._mixerStream, this._decodingStream, BassFlags.RecordEchoCancel))
                {
                    this.MusicData = null;
                    this.LastError = "BassMix.MixerAddChannel failed: " + Bass.LastError;
                    this.LastErrorCode = "BASSMIX_MIXERADDCHANNEL_FAILED: " + Bass.LastError;
                    FreeChannel(ref this._decodingStream);
                    FreeChannel(ref this._mixerStream);
                    return false;
                }
            }

            this._mixedDecodeStream = Bass.CreateStream(44100, E_int,
                BassFlags.Float | BassFlags.Prescan | BassFlags.Decode,
                this.m_A_StreamProcedure);
            return true;
        }

        //Use Reflection to get internal method "a" with signature "a(int, int, double)" of class MusicData
        private static readonly MethodInfo MusicData_a = typeof(MusicData).GetMethod("a",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(int), typeof(int), typeof(double) },
            null);


        private void ReadChannelTags(File tags, bool isNotMono, ChannelInfo channelInfo)
        {
            var frequency = channelInfo.Frequency;
            var channels = channelInfo.Channels;
            this.m_A_long = Bass.ChannelGetLength(this._decodingStream);
            var duration = Bass.ChannelBytes2Seconds(this._decodingStream, this.m_A_long);
            //invoke MusicData.a(int, int, double) with (isNotMono ? 44100 : frequency, channels, duration)
            try
            {
                MusicData_a.Invoke(this.MusicData, new object[] { isNotMono ? 44100 : frequency, channels, duration });
            }
            catch (Exception e)
            {
                Console.WriteLine("Error invoking MusicData.a(int, int, double):");
                Console.WriteLine(e);
            }
            this.MusicData.MusicInfo.SampleRatePlayback = frequency;
            if (tags == null) 
                return;
            this.MusicData.MusicInfo.TagAlbum = tags.Tag.Album;
            this.MusicData.MusicInfo.TagArtist = tags.Tag.FirstPerformer;
            this.MusicData.MusicInfo.TagTitle = tags.Tag.Title;
            this.MusicData.MusicInfo.TagComment = tags.Tag.Comment;
            this.MusicData.MusicInfo.TagBPM = tags.Tag.BeatsPerMinute;
        }

        //Use Reflection to get internal method "A" with signature "A(MelodyReactor2.DifficultyRules)" of class MusicData
        private static readonly MethodInfo MusicData_A = typeof(MusicData).GetMethod("A",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(MelodyReactor2.DifficultyRules) },
            null);
        
        public override void AnalyzeMusicFile()
        {
            try
            {
                if (string.IsNullOrEmpty(this.MusicFile))
                    throw new Exception("Music file is null or empty");
                
                this._analyzeStopwatch = Stopwatch.StartNew();
                AnalyzeInternal();
                MusicData_A.Invoke(this.MusicData, new object[] { MReactor.CurrentDifficultyRules });
                this._analyzeStopwatch.Stop();
                this.MusicData.MusicInfo.AnalysisTime = this._analyzeStopwatch.Elapsed;
                this.MusicData.OnMusicAnalyzed();
            }
            catch (Exception ex)
            {
                this.MusicData.OnMusicAnalysisException(ex.Message);
                Console.WriteLine("Error in AnalyzeMusicFile():");
                Console.WriteLine(ex);
            }
        }
        
        //Use Reflection to get internal method "f" with signature "f()" of class MusicData
        private static readonly MethodInfo MusicData_f = typeof(MusicData).GetMethod("f",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { }, null);

        //Use Reflection to get internal method "G" with signature "G()" of class MusicData
        private static readonly MethodInfo MusicData_G = typeof(MusicData).GetMethod("G",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { }, null);
        
        //Use Reflection to get internal method "c" with signature "c()" of class MusicData
        private static readonly MethodInfo MusicData_c = typeof(MusicData).GetMethod("c",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { }, null);
        
        //Use Reflection to get internal method "h" with signature "h()" of class MusicData
        private static readonly MethodInfo MusicData_h = typeof(MusicData).GetMethod("h",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { }, null);
        
        private void AnalyzeInternal()
        {
            GetFFTData();
            FreeMusicFile();
            try
            {
                MusicData_f.Invoke(this.MusicData, new object[] { });
                MusicData_G.Invoke(this.MusicData, new object[] { });
                MusicData_c.Invoke(this.MusicData, new object[] { });
                if (MReactor.EnableHeldNoteDetection)
                {
                    MusicData_h.Invoke(this.MusicData, new object[] { });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in AnalyzeInternal():");
                Console.WriteLine(ex);
            }
        }

        public override void FreeMusicFile()
        {
            //I think those are all???
            FreeChannel(ref this._decodingStream);
            FreeChannel(ref this._mixerStream);
            FreeChannel(ref this._mixedDecodeStream);
        }

        public override void GetFFTData()
        {
            var flag = false;
            if (USE_OLD_GETFFTDATA)
            {
                //false but idk
            }
            else
            {
                GetFFTDataInternal();
            }
        }
        
        //Use Reflection to get internal static int field "A" of class MReactor
        private static readonly FieldInfo MReactor_A_int = typeof(MReactor)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
            .First(f => f.Name == "A" && f.FieldType == typeof(int));
        
        //Use Reflection to get internal static field "A" of class MReactor as byte[]
        private static readonly FieldInfo MReactor_A_byteArray = typeof(MReactor)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .First(f => f.Name == "A" && f.FieldType == typeof(byte[]));

        private void GetFFTDataInternal()
        {
            Bass.ChannelSetPosition(this._isNotMonoOrNot44100? this._mixerStream : this._decodingStream, 0L);
            this.m_d_int = 0;
            this.m_a_float = 0f;
            e_int = 0;
            F_int = 0;
            MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT);
            int num = 0;
            _ = MReactor.DECIBELS_RANGE;
            _ = MReactor.DECIBELS_RANGE;
            int num2 = Convert.ToInt32(MReactor_A_int.GetValue(null));
            int num3 = 0;
            this.m_A_byteArray[0] = 0;
            this.m_A_byteArray[1] = 0;
            var theArray = MReactor_A_byteArray.GetValue(null) as byte[];
            if (theArray == null)
                throw new Exception("theArray is null");
            while (Bass.ChannelGetData(this._mixedDecodeStream, this.m_A_floatArray, this.m_c_int) > 0)
            {
                for (int i = 0; i < E_int; i++)
                {
                    byte b = 0;
                    for (int j = 2; j < num2; j++)
                    {
                        float num4 = this.m_A_floatArray[j * E_int + i];
                        if (num4 < MReactor.DECIBEL_MIN_VALUE)
                        {
                            this.m_A_byteArray[j] = 0;
                            continue;
                        }
                        if (num4 > 0.9999f)
                        {
                            this.m_A_byteArray[j] = 254;
                            b = 254;
                            continue;
                        }
                        byte b2 = theArray[(int)(num4 * 1000000f)];
                        this.m_A_byteArray[j] = b2;
                        if (b2 > b)
                        {
                            b = b2;
                        }
                    }
                    Array.Copy(this.m_A_byteArray, MusicData.Samples[num3].FFT, MReactor.FFTArrayLimitedSize);
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
            MReactor_A_byteArray.SetValue(null, theArray);
            MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT, 100);
            CalculateNormalizationFactorInternal();
        }

        private void CalculateNormalizationFactorInternal()
        {
            if (this.m_a_float <= 0.005f)
            {
                this.m_a_float = 1f;
            }
            else if (this.m_a_float <= 0.05f)
            {
                this.m_a_float = 0.05f;
            }
            else if (this.m_a_float > 0.99f)
            {
                this.m_a_float = 1f;
            }
            MusicData.MusicInfo.PCMNormalizationFactor = 1f / this.m_a_float;
            if (MusicData.MusicInfo.PCMNormalizationFactor > 5f)
            {
                MusicData.MusicInfo.PCMNormalizationFactor = 5f;
            }
            if (MusicData.MusicInfo.PCMNormalizationFactor < 1.1f)
            {
                MusicData.MusicInfo.PCMNormalizationFactor = 1f;
            }
        }

        public override bool LoadMusicFile(bool loop)
        {
            if (string.IsNullOrEmpty(this.MusicFile))
            {
                this.LastError = "Music file is null or empty";
                return false;
            }

            if (this._playbackStream != 0)
            {
                Bass.ChannelStop(this._playbackStream);
                FreeChannel(ref this._playbackStream);
            }
            var playbackFlags = BassFlags.Float | BassFlags.Prescan;
            if (loop)
                playbackFlags |= BassFlags.Loop;
            if (MReactor.UseSoftwareSampling)
                playbackFlags |= BassFlags.SoftwareMixing;
            
            var filePath = this.MusicFile;
            if (MReactor.AddSpecialPrefixToLongPathNames && filePath.Length > 260)
                filePath = @"\\?\" + filePath; //Maybe correct???
            
            this._playbackStream = Bass.CreateStream(filePath, 0L, 0L, playbackFlags);
            if (this._playbackStream == 0)
            {
                this.LastError = "Bass.CreateStream failed with error code " + Bass.LastError;
                this.LastErrorCode = Bass.LastError.ToString();
                return false;
            }

            if (MReactor.EnablePlaybackNormalization && this.MusicData.MusicInfo.PCMNormalizationFactor > 1f)
                Bass.ChannelSetDSP(this._playbackStream, this.m_A_DSPProcedure, IntPtr.Zero, 0);

            SetSongStateInternal();
            return true;
        }

        private void SetSongStateInternal()
        {
            SongState state = GetSongstateFromChannelInternal();
            if (MusicData == null) 
                return;
            
            MusicData.MusicInfo.State = state;
            SetMoreSongStateInfoInternal();
        }

        private void SetMoreSongStateInfoInternal()
        {
            MusicInfo musicInfo = MusicData.MusicInfo;
            long pos = Bass.ChannelGetPosition(this._playbackStream);
            musicInfo.PositionWithoutLatency = Bass.ChannelBytes2Seconds(this._playbackStream, pos);
            musicInfo.Position = musicInfo.PositionWithoutLatency - LatencyCompensationInSeconds;
            double num = MusicData.MusicInfo.Position * MusicData.MusicInfo.SamplesPerSeconds;
            float sampleProgress;
            int sampleIDFromPosition = MusicData.GetSampleIDFromPosition(num, out sampleProgress);
            musicInfo.CurrentSampleID = sampleIDFromPosition;
            if (musicInfo.CurrentSampleID >= musicInfo.TotalSamples)
            {
                musicInfo.CurrentSampleID = musicInfo.TotalSamples - 1;
                num = MusicData.MusicInfo.CurrentSampleID;
                sampleProgress = 0f;
            }
            musicInfo.CurrentSamplePosition = num;
            musicInfo.CurrentInSampleProgress = sampleProgress;
        }

        private SongState GetSongstateFromChannelInternal()
        {
            SongState result = SongState.Stopped;
            if (this._playbackStream == 0) 
                return result;
            switch (Bass.ChannelIsActive(this._playbackStream))
            {
                case PlaybackState.Paused:
                    result = SongState.Paused;
                    break;
                case PlaybackState.Playing:
                case PlaybackState.Stalled:
                    result = SongState.Playing;
                    break;
                case PlaybackState.Stopped:
                    result = SongState.Stopped;
                    break;
            }
            return result;
        }

        public override bool PlayMusicFile()
        {
            if (this._playbackStream == 0)
            {
                this.LastError = "Music file is not loaded";
                SetSongStateInternal();
                return false;
            }

            if (!Bass.ChannelPlay(this._playbackStream, true))
            {
                var lastError = Bass.LastError;
                this.LastError = "Bass.ChannelPlay failed with error code " + lastError;
                this.LastErrorCode = lastError.ToString();
                return false;
            }
            
            SetSongStateInternal();
            return true;
        }

        public override SongState GetLiveStreamState()
        {
            return GetSongstateFromChannelInternal();
        }

        public override void PauseLiveStream()
        {
            if (this._playbackStream == 0) 
                return;
            SetSongStateInternal();
            if (MusicData.MusicInfo.State != SongState.Playing) 
                return;
            
            if (FORCE_PAUSE_EXACT_POSITION) 
                this._exactPlaybackPosition = Bass.ChannelGetPosition(this._playbackStream);
            Bass.ChannelPause(this._playbackStream);
            if (FORCE_PAUSE_EXACT_POSITION)
                Bass.ChannelSetPosition(this._playbackStream, this._exactPlaybackPosition);
            this.MusicData.MusicInfo.State = SongState.Paused;
        }

        public override void ResumeLiveStream()
        {
            if (this._playbackStream == 0) 
                return;
            
            SetSongStateInternal();
            if (this.MusicData.MusicInfo.State != SongState.Paused) 
                return;
            
            Bass.ChannelPlay(this._playbackStream, false);
            this.MusicData.MusicInfo.State = SongState.Playing;
        }

        public override void StopLiveStream()
        {
            if (this._playbackStream == 0) 
                return;
            
            SetSongStateInternal();
            if (this.MusicData == null || this.MusicData.MusicInfo.State == SongState.Stopped) 
                return;
            
            Bass.ChannelStop(this._playbackStream);
            FreeChannel(ref this._playbackStream);
            this.MusicData.MusicInfo.State = SongState.Stopped;
            MReactor.ClearRealTimeData();
        }

        public override void UnloadLiveStream()
        {
            Bass.ChannelStop(this._playbackStream);
            FreeChannel(ref this._playbackStream);
            this.MusicData.MusicInfo.State = SongState.Stopped;
            MReactor.ClearRealTimeData();
        }

        public override void ResetLiveStream()
        {
            if (this._playbackStream == 0) 
                return;
            
            Bass.ChannelPlay(this._playbackStream, true);
            SetSongStateInternal();
        }

        public override void Update(float elapsedSeconds)
        {
            if (MusicData == null || MusicData.MusicInfo == null) 
                return;
            int num = (int)(elapsedSeconds * 2000f);
            if (num < 10)
            {
                num = 10;
            }
            if (!MReactor.UseOwnThread)
            {
                Bass.Update(num);
            }
            SetSongStateInternal();
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
            Bass.ChannelGetData(this._playbackStream, MReactor.RealTimeFFTData, -2147483644);
            var theArray = MReactor_A_byteArray.GetValue(null) as byte[];
            if (theArray == null)
            {
                throw new Exception("MReactor_A_byteArray is null");
            }
            
            for (int i = 2; i < (MReactor_A_int.GetValue(null) as int?); i++)
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
                    MReactor.RealTimeByteFFTData[i] = theArray[(int)(num * 1000000f)];
                }
            }
            MReactor_A_byteArray.SetValue(null, theArray);
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
            float num6 = Math.Max(MReactor.RealTimeOctaveFFTDataAll[0] * octavesBinsLevelFactor[0], MReactor.RealTimeOctaveFFTDataAll[1] * octavesBinsLevelFactor[1]);
            float num7 = MReactor.RealTimeOctaveFFTDataAll[2] * octavesBinsLevelFactor[2];
            float num8 = Math.Max(MReactor.RealTimeOctaveFFTDataAll[3] * octavesBinsLevelFactor[3], MReactor.RealTimeOctaveFFTDataAll[4] * octavesBinsLevelFactor[4]);
            float num9 = Math.Max(MReactor.RealTimeOctaveFFTDataAll[5] * octavesBinsLevelFactor[5], MReactor.RealTimeOctaveFFTDataAll[6] * octavesBinsLevelFactor[6]);
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
            Bass.ChannelSetPosition(this._playbackStream, Bass.ChannelSeconds2Bytes(this._playbackStream, seconds));
            SetSongStateInternal();
        }

        public override void ResetPosition()
        {
            Bass.ChannelSetPosition(this._playbackStream, 0L);
            SetSongStateInternal();
        }

        public override void SetVolume(float volume)
        {
            Bass.ChannelSetAttribute(this._playbackStream, ChannelAttribute.Volume, volume);
        }

        public override float GetVolume()
        {
            return (float)Bass.ChannelGetAttribute(this._playbackStream, ChannelAttribute.Volume);
        }

        public override void SetPlaybackSpeed(float speedFactor)
        {
            Bass.ChannelSetAttribute(this._playbackStream, ChannelAttribute.Frequency, (float)MusicData.MusicInfo.SampleRatePlayback * speedFactor);
        }
    }
}