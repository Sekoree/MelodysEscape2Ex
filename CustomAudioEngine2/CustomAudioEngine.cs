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

        private const int m_A_int = 100;

        private Thread m_A_Thread;

        private int m_a_int;

        private int m_B_int;

        private bool m_A_bool;

        private int m_b_int;

        private int m_C_int;

        private long m_A_long;

        private Stopwatch m_A_Stopwatch = new Stopwatch();

        private IntPtr m_A_IntPtr = IntPtr.Zero;

        private string m_A_string = string.Empty;

        private byte[] m_A_byteArray;

        private float[] m_A_floatArray;

        private int m_c_int;

        private DSPProcedure m_A_DSPProcedure;

        private StreamProcedure m_A_StreamProcedure;

        private float m_A_float;

        private float[] m_a_floatArray;

        private float[] m_B_floatArray;

        private long m_a_long;

        private int m_D_int;

        private int m_d_int;

        private readonly int E_int;

        private long m_B_long;

        private int e_int;

        private int F_int;

        private float m_a_float;

        #endregion

        public CustomAudioEngine(IntPtr windowHandle)
        {
            this.m_A_IntPtr = windowHandle;
            this.m_A_DSPProcedure = TheDSPProc;
            this.m_A_StreamProcedure = TheStreamProc;
            if (MReactor.FFTQuality < 1024)
            {
                MReactor.FFTQuality = 1024;
            }
            this.E_int = MReactor.FFTQuality / 1024;
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

        private static int GetBassFlags(int fftQuality) => fftQuality switch
        {
            256 => int.MinValue,
            512 => -2147483647,
            1024 => -2147483646,
            2048 => -2147483645,
            4096 => -2147483644,
            16384 => -2147483642,
            _ => -2147483643,
        };

        private unsafe void TheDSPProc(int Handle, int Channel, IntPtr Buffer, int Length, IntPtr User)
        {
            if (Length != 0 && !(Buffer == IntPtr.Zero))
            {
                int num = Length / 4;
                float* ptr = (float*)(void*)Buffer;
                for (int i = 0; i < num; i++)
                {
                    *ptr *= MusicData.MusicInfo.PCMNormalizationFactor;
                    ptr++;
                }
            }
        }

        private unsafe int TheStreamProc(int Handle, IntPtr Buffer, int Length, IntPtr User)
        {
            if (Length == 0 || Buffer == IntPtr.Zero)
            {
                return 0;
            }
            float* ptr = (float*)(void*)Buffer;
            int num = Length / 4 / E_int;
            int num2 = num / E_int;
            int num3 = this.m_B_floatArray.Length;
            int num4 = this.m_a_floatArray.Length - num3;
            double num5 = 1024.0;
            int num6 = 2;
            bool flag = false;
            if (this.m_D_int <= 0 || this.m_d_int >= num4)
            {
                if (this.m_D_int > 0)
                {
                    Array.Copy(this.m_a_floatArray, num4, this.m_B_floatArray, 0, num3);
                    flag = true;
                }
                this.m_d_int = 0;
                int num7 = Bass.ChannelGetData(this.m_A_bool ? this.m_b_int : this.m_a_int, this.m_a_floatArray, this.m_a_floatArray.Length * 4);
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
                    this.e_int++;
                    if (this.e_int < 1024)
                    {
                        continue;
                    }
                    float num9 = (float)((double)this.m_A_float / num5);
                    if (this.F_int >= num6 && this.F_int - num6 < MusicData.MusicInfo.TotalSamples)
                    {
                        if (num9 < 0.001f)
                        {
                            num9 = 0f;
                        }
                        if (num9 > 1f)
                        {
                            num9 = 1f;
                        }
                        MusicData.RawPCMLevels[this.F_int - num6] = (byte)(num9 * 255f);
                    }
                    this.F_int++;
                    this.e_int = 0;
                    this.m_A_float = 0f;
                }
            }
            float num10 = 0f;
            int num11 = 0;
            for (int j = 0; j < num; j++)
            {
                int num12 = j * this.E_int;
                for (int k = 0; k < this.E_int; k++)
                {
                    int num13 = this.m_d_int + j + num2 * k;
                    if (num13 < this.m_D_int)
                    {
                        num10 = (ptr[num12 + k] = ((!flag) ? this.m_a_floatArray[num13] : ((num13 >= num3) ? this.m_a_floatArray[num13 - num3] : this.m_B_floatArray[num13])));
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

        public void FreeChannel(ref int channel)
        {
            if (channel != 0)
                Bass.StreamFree(channel);
            channel = 0;
        }

        public List<string> GetPluginFiles(string directoryPath, string searchPattern, SearchOption searchOption)
        {
            List<string> list = new List<string>();
            string[] array = searchPattern.Split(';');
            foreach (string searchPattern2 in array)
            {
                list.AddRange(Directory.GetFiles(directoryPath, searchPattern2, searchOption));
            }
            for (int num = list.Count - 1; num >= 0; num--)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(list[num]);
                if (!fileNameWithoutExtension.StartsWith("bass", ignoreCase: false, CultureInfo.InvariantCulture))
                {
                    list.RemoveAt(num);
                }
                else if (fileNameWithoutExtension == "bass" || fileNameWithoutExtension == "bassmix")
                {
                    list.RemoveAt(num);
                }
            }
            list.Sort();
            return list;
        }

        public override void SetPluginsDirectory(string path)
        {
            this.m_A_string = path;
        }

        private void LoadBassPlugin(string pluginPath)
        {
            string fileName = Path.GetFileName(pluginPath);
            Console.WriteLine("Trying to load BASS plugin \"" + fileName + "\"...");
            try
            {
                if (Bass.PluginLoad(pluginPath) == 0)
                {
                    Errors bASSError = Bass.LastError;
                    string text = bASSError switch
                    {
                        Errors.FileOpen => "file could not be opened",
                        Errors.FileFormat => "file is not a plugin (or incorrect architecture, check for 32/64 bits))",
                        Errors.Already => "file is already plugged in",
                        _ => bASSError.ToString(),
                    };
                    Console.WriteLine("Unable to load BASS plugin \"" + fileName + "\": [" + text + "]");
                }
                else
                {
                    Console.WriteLine("Successfully loaded BASS plugin \"" + fileName + "\".");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to load BASS plugin. Exception! [" + ex.Message + "]");
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            if (LOAD_PLUGINS)
            {
                if (Directory.Exists(this.m_A_string))
                {
                    List<string> pluginFiles = GetPluginFiles(this.m_A_string, "*.dll;*.dylib;*.so;", SearchOption.TopDirectoryOnly);
                    Console.WriteLine(string.Format("Found [{0}] plugin files in \"{1}\"", pluginFiles.Count, this.m_A_string));
                    foreach (string item in pluginFiles)
                    {
                        LoadBassPlugin(item);
                    }
                }
                else
                {
                    Console.WriteLine("BASS Plugins subfolder not found: \"" + this.m_A_string + "\", unable to load plugins");
                }
            }
            this.SupportedFilesExtensions = Bass.SupportedFormats;
            Console.WriteLine("File support: " + this.SupportedFilesExtensions);
            var i = 0;
            var flag = false;
            var bassError = Bass.LastError;
            if (USE_CONFIG_DEVICE_BUFFER)
            {
                Bass.DeviceBufferLength = 80;
                Console.WriteLine("BASS_CONFIG_DEV_BUFFER set to " + 80);
            }
            Bass.Configure(Configuration.IncludeDefaultDevice, true);
            for (; i < 5; i++)
            {
                if (Bass.Init(-1, 44100, DeviceInitFlags.Default, this.m_A_IntPtr))
                {
                    if (LOAD_PLUGINS)
                    {
                        Console.WriteLine("[BASS INFO] " + Bass.Info);
                    }
                    flag = true;
                    break;
                }
                bassError = Bass.LastError;
                if (bassError == Errors.Already)
                {
                    Bass.Free();
                }
                Thread.Sleep(1000);
            }
            if (!flag)
            {
                throw new AudioEngineInitializationException("Error while initalizing BASS Audio Engine: " + bassError);
            }
            Bass.UpdatePeriod = MReactor.UseOwnThread ? 5 : 0;
            Bass.PlaybackBufferLength = 250;
            if (USE_CONFIG_DEVICE_BUFFER)
            {
                Console.WriteLine("BASS_CONFIG_DEV_BUFFER read config value: " + Bass.PlaybackBufferLength);
            }
        }

        public override void CleanRessources()
        {
            base.CleanRessources();
        }

        public override bool IsReadyToAnalyze()
        {
            return this.m_a_int != 0;
        }

        public override void InitAndAnalyzeMusicFileAsync()
        {
            this.m_A_Thread = new Thread(InitAndAnalyzeMusicFileAsyncThread);
            this.m_A_Thread.Start();
        }

        public override void AnalyzeMusicFileAsync()
        {
            this.m_A_Thread = new Thread(AnalyzeMusicFileAsyncThread);
            this.m_A_Thread.Start();
        }

        public void InitAndAnalyzeMusicFileAsyncThread()
        {
            if (MReactor.AudioEngine.InitMusicFile(ignoreShortDurationError: false))
            {
                MReactor.AudioEngine.AnalyzeMusicFile();
            }
        }

        public void AnalyzeMusicFileAsyncThread()
        {
            MReactor.AudioEngine.AnalyzeMusicFile();
        }

        private MethodInfo MusicData_a = typeof(MusicData).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .First(x => x.Name == "a"
            && x.GetParameters().ElementAtOrDefault(0)?.ParameterType == typeof(int)
            && x.GetParameters().ElementAtOrDefault(1)?.ParameterType == typeof(int)
            && x.GetParameters().ElementAtOrDefault(2)?.ParameterType == typeof(double));


        private void ReadAudioTags(ChannelInfo channelInfo, int channel)
        {
            int freq = channelInfo.Frequency;
            int chans = channelInfo.Channels;
            this.m_A_long = Bass.ChannelGetLength(this.m_a_int);
            double num = Bass.ChannelBytes2Seconds(this.m_a_int, this.m_A_long);
            try
            {
                this.MusicData_a.Invoke(this.MusicData, new object[] { freq, chans, num });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to Execute 'a' in ReadAudioTags:");
                Console.WriteLine(ex);
            }
            MusicData.MusicInfo.SampleRatePlayback = freq;

            var fileTags = TagLib.File.Create(this.MusicFile);
            if (fileTags != null)
            {
                MusicData.MusicInfo.TagAlbum = fileTags.Tag.Album;
                MusicData.MusicInfo.TagArtist = fileTags.Tag.JoinedAlbumArtists;
                MusicData.MusicInfo.TagTitle = fileTags.Tag.Title;
                MusicData.MusicInfo.TagComment = fileTags.Tag.Comment;
                MusicData.MusicInfo.TagBPM = fileTags.Tag.BeatsPerMinute;
            }
        }

        public override bool InitMusicFile(bool ignoreShortDurationError)
        {
            base.InitMusicFile(ignoreShortDurationError);
            if (base.MusicFile != null && !string.IsNullOrEmpty(base.MusicFile))
            {
                string text = base.MusicFile;
                if (MReactor.AddSpecialPrefixToLongPathNames && text.Length > 260)
                {
                    text = "\\\\?\\" + text;
                }
                MusicData.MusicInfo = new MusicInfo();
                MusicData.MusicInfo.FileFullPath = base.MusicFile;
                MusicData.MusicInfo.FileFullPathLongSafe = text;
                MusicData.MusicInfo.Filename = Path.GetFileName(base.MusicFile);
                MusicData.MusicInfo.FilenameWithoutExt = Path.GetFileNameWithoutExtension(base.MusicFile);
                this.m_d_int = 0;
                this.m_D_int = 0;
                this.m_a_long = 0L;
                this.m_A_float = 0f;
                this.m_a_int = Bass.CreateStream(text, 0L, 0L, BassFlags.Mono | BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
                if (this.m_a_int != 0)
                {
                    //BassTags.ReadPictureTAGs = false;
                    //TAG_INFO tAG_INFO = BassTags.BASS_TAG_GetFromFile(text);
                    var channelInfo = Bass.ChannelGetInfo(this.m_a_int);
                    this.m_A_bool = channelInfo.Channels > 1 || (FORCE_44100_DECODING && channelInfo.Frequency != 44100);
                    ReadAudioTags(channelInfo, this.m_a_int);
                    //A(bASS_CHANNELINFO, this.m_A, tAG_INFO);
                    if (MusicData.MusicInfo.Duration < 10.0 && !ignoreShortDurationError)
                    {
                        MusicData = null;
                        base.LastError = "Audio file too short for analysis";
                        base.LastErrorCode = "MREACTOR_DURATION_TOO_SHORT";
                        return false;
                    }
                    if (this.m_A_bool)
                    {
                        this.m_b_int = BassMix.CreateMixerStream(44100, 1, BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
                        if (this.m_b_int == 0)
                        {
                            MusicData = null;
                            base.LastErrorCode = Bass.LastError.ToString();
                            base.LastError = "Can't CREATE MixChannel: " + base.LastErrorCode;
                            FreeChannel(ref this.m_a_int);
                            FreeChannel(ref this.m_b_int);
                            return false;
                        }
                        if (!BassMix.MixerAddChannel(this.m_b_int, this.m_a_int, BassFlags.RecordEchoCancel))
                        {
                            MusicData = null;
                            base.LastErrorCode = Bass.LastError.ToString();
                            base.LastError = "Can't ADD MixChannel: " + base.LastErrorCode;
                            FreeChannel(ref this.m_a_int);
                            FreeChannel(ref this.m_b_int);
                            return false;
                        }
                    }
                    this.m_B_int = Bass.CreateStream(44100, E_int, BassFlags.Float | BassFlags.Decode, this.m_A_StreamProcedure);
                    return true;
                }
                MusicData = null;
                base.LastErrorCode = Bass.LastError.ToString();
                base.LastError = "Can't create Decoding Channel: " + base.LastErrorCode;
                if (base.LastErrorCode == "BASS_ERROR_FILEFORM")
                {
                    base.LastError = "Unsupported audio file format";
                }

                else if (base.LastErrorCode == "BASS_ERROR_FILEOPEN" && base.MusicFile.Length > 260)
                {
                    base.LastErrorCode = "BASS_ERROR_PATH_TOO_LONG";
                }
                return false;
            }
            base.LastError = "Empty music filename";
            return false;
        }

        public override void AnalyzeMusicFile()
        {
            try
            {
                if (base.MusicFile == string.Empty)
                {
                    throw new Exception("No selection music file");
                }
                this.m_A_Stopwatch.Restart();
                //A();
                //MusicData.A(MReactor.CurrentDifficultyRules);
                this.m_A_Stopwatch.Stop();
                MusicData.MusicInfo.AnalysisTime = this.m_A_Stopwatch.Elapsed;
                MusicData.OnMusicAnalyzed();
            }
            catch (Exception ex)
            {
                this.MusicData.OnMusicAnalysisException(ex.Message);
            }
        }
    }
}