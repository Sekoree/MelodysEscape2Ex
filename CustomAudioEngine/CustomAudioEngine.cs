using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CustomAudioEngine.Entities;
using ManagedBass;
using ManagedBass.Mix;
using MelodyReactor2;
using YoutubeExplode;
using YoutubeExplode.Videos;
using Debug = UnityEngine.Debug;
using File = TagLib.File;

namespace CustomAudioEngine
{
    public class CustomAudioEngine : AudioEngine
    {
        #region Static Flags

        public static bool USE_OLD_GETFFTDATA = false;
        public static bool LOAD_PLUGINS = true;
        public static bool FORCE_44100_DECODING = true;
        public static bool USE_CONFIG_DEVICE_BUFFER = false;
        public static bool USE_FFT_HANN_WINDOW = true;
        public static bool FORCE_PAUSE_EXACT_POSITION = true;

        #endregion

        #region Fields

        private Thread _decodeThread;

        private Stopwatch _analyzeStopwatch = new Stopwatch();

        private DSPProcedure _dspProc;
        private StreamProcedure _streamProc;

        private string _pluginPath;

        /// <summary>
        /// A_long
        /// </summary>
        private long _channelLength;

        #endregion

        #region Unkown Fields

        private readonly int StreamProcChannelCount;
        private int c_int;
        private int D_int;
        private int d_int;
        private long a_long;
        private bool IsNotMonoOrNot44100Frequency;
        private int BassMixChannel;
        private int InitialMonoChannel;
        private float A_float;
        private int e_int;
        private int F_int;
        private int StreamProcChannel;
        private float a_float;
        private int C_int;
        private long B_long;
        private float[] a_floatArray;
        private float[] B_floatArray;
        private byte[] A_byteArray;
        private float[] A_floatArray;

        private IntPtr A_intPtr;

        #endregion

        public bool CurrentIsYouTube { get; set; }

        public CustomAudioEngine(IntPtr window)
        {
            //get MReactor internal static fields with reflection
            var fields = typeof(MReactor).GetFields(BindingFlags.Static | BindingFlags.NonPublic);
            MReactor_AInt = fields.FirstOrDefault(x => x.Name == "A" && x.FieldType == typeof(int));
            MReactor_AByteArray = fields.FirstOrDefault(x => x.Name == "A" && x.FieldType == typeof(byte[]));

            A_intPtr = window;
            _dspProc = DSPProcedure;
            _streamProc = StreamProcedure;
            if (MReactor.FFTQuality < 1024)
            {
                MReactor.FFTQuality = 1024;
            }

            StreamProcChannelCount = MReactor.FFTQuality / 1024;
            //Debug.Log("E_int: " + E_int);
            Console.WriteLine("E_int: " + StreamProcChannelCount);
            
            a_floatArray = new float[MReactor.FFTQuality * StreamProcChannelCount];
            //Debug.Log("a_floatArray.Length: " + a_floatArray.Length);
            Console.WriteLine("a_floatArray.Length: " + a_floatArray.Length);
            
            B_floatArray = new float[MReactor.FFTQuality];
            //Debug.Log("B_floatArray.Length: " + B_floatArray.Length);
            Console.WriteLine("B_floatArray.Length: " + B_floatArray.Length);
            
            A_byteArray = new byte[MReactor.FFTQuality / 2];
            //Debug.Log("A_byteArray.Length: " + A_byteArray.Length);
            Console.WriteLine("A_byteArray.Length: " + A_byteArray.Length);
            
            A_floatArray = new float[MReactor.FFTQuality / 2 * StreamProcChannelCount];
            //Debug.Log("A_floatArray.Length: " + A_floatArray.Length);
            Console.WriteLine("A_floatArray.Length: " + A_floatArray.Length);
            
            c_int = GetBassFlagsForFFTQuality(MReactor.FFTQuality);
            //Debug.Log("c_int: " + c_int);
            Console.WriteLine("c_int: " + c_int);
            if (!USE_FFT_HANN_WINDOW)
            {
                c_int |= 32;
            }
            c_int |= 16;
        }

        #region Helper Methods

        public static int GetBassFlagsForFFTQuality(int fftQuality)
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

        public List<string> GetPluginFiles(string directoryPath, string searchPattern, SearchOption searchOption)
        {
            var list = new List<string>();
            foreach (var searchPattern2 in searchPattern.Split(';'))
            {
                list.AddRange(Directory.GetFiles(directoryPath, searchPattern2, searchOption));
            }

            for (var num = list.Count - 1; num >= 0; num--)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(list[num]);
                if (!fileNameWithoutExtension.StartsWith("bass", false, CultureInfo.InvariantCulture))
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

        private void ReadAudioTags(ChannelInfo channelInfo, bool isNotMonoOrNot44100Hz, bool readFromFile = true)
        {
            _channelLength = Bass.ChannelGetLength(InitialMonoChannel);
            var length = Bass.ChannelBytes2Seconds(InitialMonoChannel, _channelLength);
            MusicData_ProcessChannelParameters(isNotMonoOrNot44100Hz ? 44100 : channelInfo.Frequency,
                channelInfo.Channels, length);
            MusicData.MusicInfo.SampleRatePlayback = channelInfo.Frequency;

            if (!readFromFile)
                return;

            try
            {
                using (var tags = File.Create(MusicFile))
                {
                    MusicData.MusicInfo.TagAlbum = tags.Tag.Album;
                    MusicData.MusicInfo.TagArtist = tags.Tag.FirstPerformer;
                    MusicData.MusicInfo.TagTitle = tags.Tag.Title;
                    MusicData.MusicInfo.TagComment = tags.Tag.Comment;
                    MusicData.MusicInfo.TagBPM = tags.Tag.BeatsPerMinute;
                }
            }
            catch
            {
                //Debug.LogWarning("Unable to read tags from file: " + MusicFile);
                Console.WriteLine("Unable to read tags from file: " + MusicFile);
            }

            if (string.IsNullOrWhiteSpace(MusicData.MusicInfo.TagComment))
                return;

            var list = new List<double>();
            foreach (var commentPart in MusicData.MusicInfo.TagComment.Split(';'))
            {
                if (commentPart == "3/4")
                {
                    MusicData.MusicInfo.Is34TimingTag = true;
                    continue;
                }

                var couldParse = double.TryParse(commentPart, out var result);
                if (couldParse && result > 2.0)
                {
                    list.Add(result);
                }
            }

            if (list.Count > 0)
            {
                MusicData.MusicInfo.AdditionalTagBPMs = list.ToArray();
            }
        }

        #endregion

        #region Procedure Methods

        private void DSPProcedure(int handle, int channel, IntPtr buffer, int length, IntPtr user)
        {
            if (length != 0 && !(buffer == IntPtr.Zero))
            {
                unsafe
                {
                    var num = length / 4;
                    var ptr = (float*)(void*)buffer;
                    for (var i = 0; i < num; i++)
                    {
                        *ptr *= MusicData.MusicInfo.PCMNormalizationFactor;
                        ptr++;
                    }
                }
            }
        }

        private int StreamProcedure(int handle, IntPtr buffer, int length, IntPtr user)
        {
            unsafe
            {
                if (length == 0 || buffer == IntPtr.Zero)
                {
                    return 0;
                }

                var ptr = (float*)(void*)buffer;
                var num = length / 4 / StreamProcChannelCount;
                var num2 = num / StreamProcChannelCount;
                var num3 = this.B_floatArray.Length;
                var num4 = this.a_floatArray.Length - num3;
                var num5 = 1024.0;
                var num6 = 2;
                var flag = false;
                if (D_int <= 0 || d_int >= num4)
                {
                    var a2 = a_long;
                    if (D_int > 0)
                    {
                        Array.Copy(a_floatArray, num4, B_floatArray, 0, num3);
                        flag = true;
                    }

                    d_int = 0;
                    //Debug.Log("Bass.ChannelGetData with length: " + a_floatArray.Length * 4);
                    //Console.WriteLine("Bass.ChannelGetData with length: " + a_floatArray.Length * 4);
                    //tbh i dont think these are bass flags
                    //PrintUsedBassFlags(a_floatArray.Length * 4);
                    //Console.WriteLine("Bass.ChannelGetData with length: " + a_floatArray.Length * 4);
                    var num7 = Bass.ChannelGetData(IsNotMonoOrNot44100Frequency ? BassMixChannel : InitialMonoChannel, a_floatArray,
                        a_floatArray.Length * 4);
                    D_int = num7 / 4;
                    a_long += D_int;
                    for (var i = 0; i < D_int; i++)
                    {
                        var num8 = a_floatArray[i];
                        if (num8 < 0f)
                        {
                            num8 = 0f - num8;
                        }

                        if (num8 > 1f)
                        {
                            num8 = 1f;
                        }

                        A_float += num8;
                        e_int++;
                        if (e_int < 1024)
                        {
                            continue;
                        }

                        var num9 = (float)(A_float / num5);
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
                        A_float = 0f;
                    }
                }

                var num10 = 0f;
                var num11 = 0;
                for (var j = 0; j < num; j++)
                {
                    var num12 = j * StreamProcChannelCount;
                    for (var k = 0; k < StreamProcChannelCount; k++)
                    {
                        var num13 = d_int + j + num2 * k;
                        if (num13 >= D_int)
                            continue;

                        num10 = (ptr[num12 + k] = ((!flag)
                            ? a_floatArray[num13]
                            : ((num13 >= num3) ? a_floatArray[num13 - num3] : B_floatArray[num13])));
                        num11++;
                        if (num10 < 0f)
                        {
                            num10 = 0f - num10;
                        }

                        if (num10 > a_float)
                        {
                            a_float = num10;
                        }
                    }
                }

                d_int += num;
                if (flag)
                {
                    d_int = 0;
                }

                return num11 * 4;
            }
        }

        #endregion

        #region MReactor Internal Fields

        private FieldInfo MReactor_AInt;

        private FieldInfo MReactor_AByteArray;

        #endregion

        #region MusicData Internal Methods

        private readonly MethodInfo _musicData_ProcessChannelParameters = typeof(MusicData).GetMethod("a",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null, new Type[]
            {
                typeof(int),
                typeof(int),
                typeof(double)
            }, null);

        private readonly MethodInfo _musicData_InitializeTrackDefinition = typeof(MusicData).GetMethod("A",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null, new Type[]
            {
                typeof(DifficultyRules)
            }, null);


        private readonly MethodInfo _musicData_ProcessSamplesAndOctaves = typeof(MusicData).GetMethod("f",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null);

        private readonly MethodInfo _musicData_FilterValues = typeof(MusicData).GetMethod("G",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null);

        private readonly MethodInfo _musicData_AnalyzeBPM = typeof(MusicData).GetMethod("c",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null);

        private readonly MethodInfo _musicData_DoVoiceDetection = typeof(MusicData).GetMethod("h",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null);

        private void MusicData_ProcessChannelParameters(int frequency, int channels, double length)
        {
            try
            {
                _musicData_ProcessChannelParameters.Invoke(MusicData, new object[]
                {
                    frequency,
                    channels,
                    length
                });
            }
            catch (Exception e)
            {
                //Debug.LogError(e);
                Console.WriteLine(e);
            }
        }

        private void MusicData_InitializeTrackDefinition(DifficultyRules currentRules)
        {
            try
            {
                _musicData_InitializeTrackDefinition.Invoke(MusicData, new object[]
                {
                    currentRules
                });
            }
            catch (Exception e)
            {
                //Debug.LogError(e);
                Console.WriteLine(e);
            }
        }


        /// <remarks>
        /// f method
        /// </remarks>
        private void MusicData_ProcessSamplesAndOctaves()
        {
            try
            {
                _musicData_ProcessSamplesAndOctaves.Invoke(MusicData, null);
            }
            catch (Exception e)
            {
                //Debug.LogError(e);
                Console.WriteLine(e);
            }
        }

        /// <remarks>
        /// G method
        /// </remarks>
        private void MusicData_FilterValues()
        {
            try
            {
                _musicData_FilterValues.Invoke(MusicData, null);
            }
            catch (Exception e)
            {
                //Debug.LogError(e);
                Console.WriteLine(e);
            }
        }

        /// <remarks>
        /// c method
        /// </remarks>
        private void MusicData_AnalyzeBPM()
        {
            try
            {
                _musicData_AnalyzeBPM.Invoke(MusicData, null);
            }
            catch (Exception e)
            {
                //Debug.LogError(e);
                Console.WriteLine(e);
            }
        }

        /// <remarks>
        /// h method
        /// </remarks>
        private void MusicData_DoVoiceDetection()
        {
            try
            {
                _musicData_DoVoiceDetection.Invoke(MusicData, null);
            }
            catch (Exception e)
            {
                //Debug.LogError(e);
                Console.WriteLine(e);
            }
        }

        #endregion

        public override void SetMusicFile(string fileName)
        {
            Console.WriteLine("SetMusicFile: " + fileName);
            if (fileName.StartsWith("[ME2Ex][YT]_"))
            {
                fileName = $"https://www.youtube.com/watch?v={fileName.Substring(12)}";
                CurrentIsYouTube = true;
            }
            else
            {
                CurrentIsYouTube = false;
            }
            Console.WriteLine("SetMusicFile: " + fileName);
            base.SetMusicFile(fileName);
        }

        #region Setup Methods

        public override void SetPluginsDirectory(string path)
        {
            _pluginPath = path;
        }

        public override void Initialize()
        {
            base.Initialize();
            if (LOAD_PLUGINS && Directory.Exists(_pluginPath))
            {
                var pluginFiles = GetPluginFiles(_pluginPath, "*.dll;*.so;*.dylib;", SearchOption.TopDirectoryOnly);
                foreach (var pluginFile in pluginFiles)
                {
                    //Debug.Log($"Loading plugin: {pluginFile}");
                    Console.WriteLine($"Loading plugin: {pluginFile}");
                    var plugin = Bass.PluginLoad(pluginFile);
                    if (plugin != 0)
                        continue;
                    //Debug.LogError($"Failed to load plugin: {pluginFile}");
                    Console.WriteLine($"Failed to load plugin: {pluginFile}");
                    //Debug.LogError($"Error: {Bass.LastError}");
                    Console.WriteLine($"Error: {Bass.LastError}");
                }
            }

            if (USE_CONFIG_DEVICE_BUFFER)
            {
                Bass.Configure(Configuration.DeviceBufferLength, 80);
                //Debug.Log("Device buffer length set to 80ms");
                Console.WriteLine("Device buffer length set to 80ms");
            }

            Bass.Configure(Configuration.IncludeDefaultDevice, true);

            if (Bass.Init(-1, 44100, DeviceInitFlags.Default, A_intPtr))
            {
                //Debug.Log("Bass initialized");
                Console.WriteLine("Bass initialized");
                Bass.Configure(Configuration.UpdatePeriod, MReactor.UseOwnThread ? 5 : 0);
                //Debug.Log("Bass update period set to " + (MReactor.UseOwnThread ? 5 : 0) + "ms");
                Console.WriteLine("Bass update period set to " + (MReactor.UseOwnThread ? 5 : 0) + "ms");
                Bass.Configure(Configuration.PlaybackBufferLength, 250);
                //Debug.Log("Playback buffer length set to 250ms");
                Console.WriteLine("Playback buffer length set to 250ms");
                
                //Bass.Configure(Configuration.af)
                
                return;
            }

            //Debug.LogError("Failed to initialize bass");
            Console.WriteLine("Failed to initialize bass");
            //Debug.LogError($"Error: {Bass.LastError}");
            Console.WriteLine($"Error: {Bass.LastError}");
            throw new AudioEngineInitializationException("Failed to initialize bass: " + Bass.LastError);
        }

        public override void Unload()
        {
            Bass.Stop();
            Bass.Free();
        }

        #endregion

        #region Decode Stream

        public override bool InitMusicFile(bool ignoreTooShort = false)
        {
            base.InitMusicFile(ignoreTooShort);
            if (string.IsNullOrWhiteSpace(this.MusicFile))
            {
                LastError = "Music file is null or empty";
                return false;
            }

            //remove [ME2Ex][YT]_ from youtube links
            var sanitisedMusicFile = CurrentIsYouTube ?  MusicFile.Substring(12) : MusicFile;
            
            var videoId = VideoId.TryParse(sanitisedMusicFile);
            var isYoutube = videoId != null;
            if (isYoutube)
            {
                MusicData.MusicInfo = new WebMusicInfo()
                {
                    BaseUrl = sanitisedMusicFile,
                    FileFullPath = MusicFile,
                    Filename = MusicFile,
                    FilenameWithoutExt = MusicFile
                };
                GetYouTubeVideoInfo();
            }
            else
            {
                MusicData.MusicInfo = new MusicInfo
                {
                    FileFullPath = MusicFile,
                    Filename = Path.GetFileName(MusicFile),
                    FilenameWithoutExt = Path.GetFileNameWithoutExtension(MusicFile)
                };
            }

            d_int = 0;
            D_int = 0;
            a_long = 0L;
            A_float = 0f;
            if (isYoutube && MusicData.MusicInfo is WebMusicInfo webInfo)
            {
                //Bass.NetPreBuffer = 100;
                //Bass.NetBufferLength = 1000 * 30;
                InitialMonoChannel = Bass.CreateStream(webInfo.DataBytes, 0, webInfo.DataBytes.LongLength, BassFlags.Mono | BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
            }
            else
            {
                InitialMonoChannel = Bass.CreateStream(base.MusicFile, 0L, 0L,
                    BassFlags.Mono | BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
            }

            if (InitialMonoChannel == 0)
            {
                MusicData = null;
                LastError = "Failed to create stream";
                LastErrorCode = Bass.LastError.ToString();
                //Debug.LogError("Failed to create stream: " + Bass.LastError);
                Console.WriteLine("Failed to create stream: " + Bass.LastError);
                return false;
            }

            var channelInfo = Bass.ChannelGetInfo(InitialMonoChannel);
            IsNotMonoOrNot44100Frequency = channelInfo.Channels > 1 || (FORCE_44100_DECODING && channelInfo.Frequency != 44100);
            //A_bool = false;
            ReadAudioTags(channelInfo, IsNotMonoOrNot44100Frequency, !isYoutube);
            if (MusicData.MusicInfo.Duration < 10.0 && !ignoreTooShort)
            {
                MusicData = null;
                LastError = "Music file is too short";
                LastErrorCode = "Too Short: " + Bass.LastError;
                return false;
            }

            if (IsNotMonoOrNot44100Frequency)
            {
                BassMixChannel = BassMix.CreateMixerStream(44100, 1, BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
                if (BassMixChannel == 0)
                {
                    MusicData = null;
                    LastError = "Failed to create mixer stream";
                    LastErrorCode = Bass.LastError.ToString();
                    //Debug.LogError("Failed to create mixer stream: " + Bass.LastError);
                    Console.WriteLine("Failed to create mixer stream: " + Bass.LastError);
                    Bass.StreamFree(InitialMonoChannel);
                    Bass.StreamFree(BassMixChannel);
                    return false;
                }

                if (!BassMix.MixerAddChannel(BassMixChannel, InitialMonoChannel, BassFlags.RecordEchoCancel))
                {
                    MusicData = null;
                    LastError = "Failed to add channel to mixer";
                    LastErrorCode = Bass.LastError.ToString();
                    //Debug.LogError("Failed to add channel to mixer: " + Bass.LastError);
                    Console.WriteLine("Failed to add channel to mixer: " + Bass.LastError);
                    Bass.StreamFree(InitialMonoChannel);
                    Bass.StreamFree(BassMixChannel);
                    return false;
                }
            }

            //Debug.Log("Creating channel with StreamProcedure!");
            Console.WriteLine("Creating channel with StreamProcedure! E_int: " + StreamProcChannelCount);
            StreamProcChannel = Bass.CreateStream(44100, StreamProcChannelCount, BassFlags.Float | BassFlags.Decode, _streamProc, IntPtr.Zero);
            return true;
        }

        private void GetYouTubeVideoInfo()
        {
            //process start yt-dlp.exe with arguments -f bestaudio , -g and base youtubeurl + videoid
            Console.WriteLine("Creating Tasks");
            var t1 = new Task(GetVideoInfoAsync);
            var t2 = new Task(GetAudioBytesAsync);
            Console.WriteLine("Configuring Tasks");
            t1.ConfigureAwait(true);
            t2.ConfigureAwait(true);
            Console.WriteLine("Starting Tasks");
            t1.Start();
            t2.Start();
            Console.WriteLine("Waiting for Tasks");
            Task.WaitAll(t1, t2);
        }

        private void GetAudioBytesAsync()
        {
            var sanitisedMusicFile = CurrentIsYouTube ?  MusicFile.Substring(12) : MusicFile;
            var dataProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Directory.GetCurrentDirectory(), "me2util.exe"),
                    Arguments = $"video \"{sanitisedMusicFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            dataProcess.Start();
            //read the output of the process
            var memStream = new MemoryStream();
            dataProcess.StandardOutput.BaseStream.CopyTo(memStream);
            //wait for the process to exit
            memStream.Position = 0;
            var asWebInfo = MusicData.MusicInfo as WebMusicInfo;
            asWebInfo.DataBytes = memStream.ToArray();
        }
        
        private void GetVideoInfoAsync()
        {
            var sanitisedMusicFile = CurrentIsYouTube ?  MusicFile.Substring(12) : MusicFile;
            var infoProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Directory.GetCurrentDirectory(), "me2util.exe"),
                    Arguments = $"info \"{sanitisedMusicFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            infoProcess.Start();
            var title = infoProcess.StandardOutput.ReadLine();
            var author = infoProcess.StandardOutput.ReadLine();
            infoProcess.WaitForExit();
            infoProcess.Dispose();
            var asWebInfo = MusicData.MusicInfo as WebMusicInfo;
            asWebInfo.TagArtist = author;
            asWebInfo.TagTitle = title;
        }

        public override bool IsReadyToAnalyze()
        {
            //Debug.Log("Is Channel Ready To Analyze?: " + (a_int != 0));
            Console.WriteLine("Is Channel Ready To Analyze?: " + (InitialMonoChannel != 0));
            return InitialMonoChannel != 0;
        }

        public override void InitAndAnalyzeMusicFileAsync()
        {
            //InitAndAnalyzeMusicFileAsyncThread();
            _decodeThread = new Thread(InitAndAnalyzeMusicFileAsyncThread);
            _decodeThread.Start();
        }

        public override void AnalyzeMusicFileAsync()
        {
            //AnalyzeMusicFileAsyncThread();
            _decodeThread = new Thread(AnalyzeMusicFileAsyncThread);
            _decodeThread.Start();
        }

        private void InitAndAnalyzeMusicFileAsyncThread()
        {
            if (MReactor.AudioEngine.InitMusicFile(false))
            {
                MReactor.AudioEngine.AnalyzeMusicFile();
            }
        }

        private void AnalyzeMusicFileAsyncThread()
        {
            MReactor.AudioEngine.AnalyzeMusicFile();
        }

        public override void AnalyzeMusicFile()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(MusicFile))
                {
                    throw new Exception("Music file is null or empty");
                }

                _analyzeStopwatch.Restart();


                //if (MusicData.MusicInfo is WebMusicInfo)
                //{
                //    //wait until the download is finished
                //    //get the length of the stream
                //    var length = Bass.StreamGetFilePosition(InitialMonoChannel, FileStreamPosition.End);
                //    //get the download progress
                //    var progress = Bass.StreamGetFilePosition(InitialMonoChannel, FileStreamPosition.Download);
                //    while (progress < length)
                //    {
                //        //progress as percentage
                //        var percentage = (int) (progress * 100.0 / length);
                //        MusicData.OnLoadingStatusChanged((MusicLoadingStatus)100, percentage);
                //        Console.WriteLine($"Download Progress: {progress} / {length} ({percentage}%)");
                //        //wait a little
                //        Thread.Sleep(100);
                //        //get the download progress
                //        progress = Bass.StreamGetFilePosition(InitialMonoChannel, FileStreamPosition.Download);
                //    }
                //}
                
                ProcessAudioData();

                MusicData_InitializeTrackDefinition(MReactor.CurrentDifficultyRules);
                _analyzeStopwatch.Stop();
                MusicData.MusicInfo.AnalysisTime = _analyzeStopwatch.Elapsed;
                MusicData.OnMusicAnalyzed();
                Console.WriteLine("Music Analyzed in " + _analyzeStopwatch.Elapsed);
            }
            catch (Exception e)
            {
                MusicData.OnMusicAnalysisException(e.Message);
            }
        }

        private void ProcessAudioData()
        {
            GetFFTData();
            FreeMusicFile();
            MusicData_ProcessSamplesAndOctaves();
            MusicData_FilterValues();
            MusicData_AnalyzeBPM();
            if (MReactor.EnableHeldNoteDetection)
            {
                MusicData_DoVoiceDetection();
            }
        }

        public override void GetFFTData()
        {
            if (USE_OLD_GETFFTDATA)
            {
                //shouldn't happen
                //Debug.LogError("GetFFTData() called with USE_OLD_GETFFTDATA set to true!");
                Console.WriteLine("GetFFTData() called with USE_OLD_GETFFTDATA set to true!");
            }
            else
            {
                GetFFTData_Internal();
            }
            
            //In the OG source there is an if check here if CacheMusicSamples is true and a flag that is always true, a !false
            //One can only happened here, and a path get filename thing into nothingness
        }

        private void GetFFTData_Internal()
        {
            //Console.WriteLine("GetFFTData_Internal() called!");
            var couldSetPos = Bass.ChannelSetPosition(IsNotMonoOrNot44100Frequency ? BassMixChannel : InitialMonoChannel, 0L);
            Console.WriteLine("Could set position: " + couldSetPos);
            d_int = 0;
            a_float = 0f;
            e_int = 0;
            F_int = 0;
            MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT);
            var num = 0;
            //PointF dECIBELS_RANGE = MReactor.DECIBELS_RANGE;
            //PointF dECIBELS_RANGE2 = MReactor.DECIBELS_RANGE;
            var num2 = MReactor_AInt.GetValue(null) as int? ?? throw new Exception("MReactor_AInt is null");
            var num3 = 0;
            A_byteArray[0] = 0;
            A_byteArray[1] = 0;
            var tempArray = MReactor_AByteArray.GetValue(null) as byte[];
            if (tempArray == null)
            {
                throw new Exception("MReactor_AByteArray is null");
            }

            //FFT 8192?
            while (Bass.ChannelGetData(StreamProcChannel, A_floatArray, c_int) > 0)
            {
                //Console.WriteLine("ChannelGetData: " + Bass.LastError);
                for (var i = 0; i < StreamProcChannelCount; i++)
                {
                    byte b = 0;
                    for (var j = 2; j < num2; j++)
                    {
                        var num4 = A_floatArray[j * StreamProcChannelCount + i];
                        if (num4 < MReactor.DECIBEL_MIN_VALUE)
                        {
                            A_byteArray[j] = 0;
                            continue;
                        }

                        if (num4 > 0.9999f)
                        {
                            A_byteArray[j] = 254;
                            b = 254;
                            continue;
                        }

                        var b2 = tempArray[(int)(num4 * 1000000f)];
                        A_byteArray[j] = b2;
                        if (b2 > b)
                        {
                            b = b2;
                        }
                    }

                    Array.Copy(A_byteArray, MusicData.Samples[num3].FFT, MReactor.FFTArrayLimitedSize);
                    MusicData.Samples[num3].MaxFFTBandValue = b;
                    num3++;
                    if (num3 >= MusicData.MusicInfo.TotalSamples)
                    {
                        break;
                    }

                    var num5 = (int)Math.Round((double)(num3 + 1) / (double)MusicData.MusicInfo.TotalSamples * 100.0);
                    if (num5 == num)
                        continue;
                    num = num5;
                    MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT, num5);
                }

                if (num3 >= MusicData.MusicInfo.TotalSamples)
                {
                    break;
                }
            }

            MReactor_AByteArray.SetValue(null, tempArray);
            MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT, 100);
            ProcessPCMNormalizationFactor();
        }

        private void ProcessPCMNormalizationFactor()
        {
            if (a_float <= 0.005f)
            {
                a_float = 1f;
            }
            else if (a_float <= 0.05f)
            {
                a_float = 0.05f;
            }
            else if (a_float > 0.99f)
            {
                a_float = 1f;
            }

            MusicData.MusicInfo.PCMNormalizationFactor = 1f / a_float;
            if (MusicData.MusicInfo.PCMNormalizationFactor > 5f)
            {
                MusicData.MusicInfo.PCMNormalizationFactor = 5f;
            }

            if (MusicData.MusicInfo.PCMNormalizationFactor < 1.1f)
            {
                MusicData.MusicInfo.PCMNormalizationFactor = 1f;
            }
        }

        public override void FreeMusicFile()
        {
            Bass.StreamFree(InitialMonoChannel);
            Bass.StreamFree(BassMixChannel);
            Bass.StreamFree(StreamProcChannel);
        }

        #endregion

        #region Playback Stream

        public override bool LoadMusicFile(bool loop)
        {
            if (string.IsNullOrWhiteSpace(MusicFile))
            {
                LastError = "Music file is null or empty";
                return false;
            }

            if (C_int != 0)
            {
                Bass.ChannelStop(C_int);
                Bass.StreamFree(C_int);
            }

            var flags = BassFlags.Prescan | BassFlags.Float;
            if (loop)
            {
                flags |= BassFlags.Loop;
            }
            if (MReactor.UseSoftwareSampling)
            {
                flags |= BassFlags.SoftwareMixing;
            }

            var isYoutube = MusicData.MusicInfo is WebMusicInfo;
            var webInfo = MusicData.MusicInfo as WebMusicInfo;

            if (isYoutube)
            {
                C_int = Bass.CreateStream(webInfo.DataBytes, 0, webInfo.DataBytes.LongLength, flags);
            }
            else
            {
                C_int = Bass.CreateStream(MusicFile, 0L, 0L, flags);
            }

            if (C_int == 0)
            {
                LastError = "Failed to load music file";
                LastErrorCode = Bass.LastError.ToString();
                return false;
            }

            if (MReactor.EnablePlaybackNormalization && MusicData.MusicInfo.PCMNormalizationFactor > 1f)
            {
                Bass.ChannelSetDSP(C_int, _dspProc, IntPtr.Zero, 0);
            }

            UpdateMusicInfoState();
            return true;
        }

        public override bool PlayMusicFile()
        {
            if (C_int == 0)
            {
                LastError = "Music file is not loaded";
                UpdateMusicInfoState();
                return false;
            }

            if (Bass.ChannelPlay(C_int, true))
            {
                UpdateMusicInfoState();
                return true;
            }

            LastError = "Failed to play music file";
            LastErrorCode = Bass.LastError.ToString();
            return false;
        }

        public override SongState GetLiveStreamState()
        {
            if (C_int == 0)
            {
                return SongState.Stopped;
            }

            switch (Bass.ChannelIsActive(C_int))
            {
                case PlaybackState.Stalled:
                case PlaybackState.Playing:
                    return SongState.Playing;
                case PlaybackState.Paused:
                    return SongState.Paused;
                case PlaybackState.Stopped:
                default:
                    return SongState.Stopped;
            }
        }

        public override void PauseLiveStream()
        {
            if (C_int == 0)
            {
                return;
            }

            UpdateMusicInfoState();
            if (MusicData.MusicInfo.State != SongState.Playing)
                return;
            if (FORCE_PAUSE_EXACT_POSITION)
            {
                B_long = Bass.ChannelGetPosition(C_int);
            }

            Bass.ChannelPause(C_int);
            if (FORCE_PAUSE_EXACT_POSITION)
            {
                Bass.ChannelSetPosition(C_int, B_long);
            }

            MusicData.MusicInfo.State = SongState.Paused;
        }

        public override void ResumeLiveStream()
        {
            if (C_int == 0)
                return;
            UpdateMusicInfoState();
            if (MusicData.MusicInfo.State != SongState.Paused)
                return;

            Bass.ChannelPlay(C_int, false);
            MusicData.MusicInfo.State = SongState.Playing;
        }

        public override void StopLiveStream()
        {
            if (C_int == 0)
                return;

            UpdateMusicInfoState();
            if (MusicData == null || MusicData.MusicInfo.State == SongState.Stopped)
                return;
            Bass.ChannelStop(C_int);
            Bass.StreamFree(C_int);
            MusicData.MusicInfo.State = SongState.Stopped;
            MReactor.ClearRealTimeData();
        }

        public override void UnloadLiveStream()
        {
            Bass.ChannelStop(C_int);
            Bass.StreamFree(C_int);
            MusicData.MusicInfo.State = SongState.Stopped;
            MReactor.ClearRealTimeData();
        }

        public override void ResetLiveStream()
        {
            if (C_int == 0)
                return;
            Bass.ChannelPlay(C_int, true);
            UpdateMusicInfoState();
        }

        public override void Update(float elapsedSeconds)
        {
            if (MusicData?.MusicInfo == null)
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

            UpdateMusicInfoState();
            if (MReactor.EnableRealTimeFrequencyData && MusicData.MusicInfo.State == SongState.Playing)
            {
                UpdateRealTimeFrequencyData();
            }
        }

        public override void SetPosition(double seconds)
        {
            Bass.ChannelSetPosition(C_int, Bass.ChannelSeconds2Bytes(C_int, seconds));
            UpdateMusicInfoState();
        }

        public override void ResetPosition()
        {
            Bass.ChannelSetPosition(C_int, 0L);
            UpdateMusicInfoState();
        }

        public override void SetVolume(float volume)
        {
            Bass.Configure(Configuration.GlobalStreamVolume, (int)(volume * 10000f));
        }

        public override float GetVolume()
        {
            return Bass.GetConfig(Configuration.GlobalStreamVolume);
        }

        public override void SetPlaybackSpeed(float speedFactor)
        {
            Bass.ChannelSetAttribute(C_int, ChannelAttribute.Frequency,
                (float)MusicData.MusicInfo.SampleRatePlayback * speedFactor);
        }

        public override void UpdateRealTimeFrequencyData()
        {
            if (MusicData.MusicInfo.State != 0 && MusicData.MusicInfo.State != SongState.Paused)
            {
                return;
            }

            //Default | Loop | Unicode
            //Debug.Log("UpdateRealTimeFrequencyData");
            //PrintUsedBassFlags(-2147483644);
            var tempArray = MReactor_AByteArray.GetValue(null) as byte[];
            if (tempArray == null)
            {
                throw new NullReferenceException("MReactor_AByteArray is null");
            }

            Bass.ChannelGetData(C_int, MReactor.RealTimeFFTData, -2147483644);
            for (var i = 2; i < (MReactor_AInt.GetValue(null) as int? ?? 0); i++)
            {
                var num = MReactor.RealTimeFFTData[i];
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
                    MReactor.RealTimeByteFFTData[i] = tempArray[(int)(num * 1000000f)];
                }
            }

            MReactor_AByteArray.SetValue(null, tempArray);
            for (var j = 0; j < 7; j++)
            {
                var x = MReactor.OctaveBinsRanges[j].X;
                var y = MReactor.OctaveBinsRanges[j].Y;
                var num2 = 0f;
                var num3 = y - x + 1;
                for (var k = x; k <= y; k++)
                {
                    var num4 = MReactor.ByteToFloatCache[MReactor.RealTimeByteFFTData[k]];
                    num2 += num4;
                }

                var num5 = num2 / (float)num3;
                MReactor.RealTimeOctaveFFTDataAll[j] = num5;
            }

            var octavesBinsLevelFactor = MusicSample.OctavesBinsLevelFactor;
            var num6 = Math.Max(MReactor.RealTimeOctaveFFTDataAll[0] * octavesBinsLevelFactor[0],
                MReactor.RealTimeOctaveFFTDataAll[1] * octavesBinsLevelFactor[1]);
            var num7 = MReactor.RealTimeOctaveFFTDataAll[2] * octavesBinsLevelFactor[2];
            var num8 = Math.Max(MReactor.RealTimeOctaveFFTDataAll[3] * octavesBinsLevelFactor[3],
                MReactor.RealTimeOctaveFFTDataAll[4] * octavesBinsLevelFactor[4]);
            var num9 = Math.Max(MReactor.RealTimeOctaveFFTDataAll[5] * octavesBinsLevelFactor[5],
                MReactor.RealTimeOctaveFFTDataAll[6] * octavesBinsLevelFactor[6]);
            MReactor.RealTimeOctaveFFTData[0] = num6;
            MReactor.RealTimeOctaveFFTData[1] = num7;
            MReactor.RealTimeOctaveFFTData[2] = num8;
            MReactor.RealTimeOctaveFFTData[3] = num9;
            var num10 = 0f;
            for (var l = 0; l < 4; l++)
            {
                var num11 = MReactor.RealTimeOctaveFFTData[l] * MusicSample.MergedOctavesBinsLevelFactor[l];
                num10 += num11;
            }

            num10 /= MusicSample.MergedOctavesBinsLevelFactorSum;
            MReactor.RealTimeLevel = num10;
        }

        private void UpdateMusicInfoState()
        {
            if (MusicData == null)
                return;
            MusicData.MusicInfo.State = GetLiveStreamState();
            ProcessMusicStateInfo();
        }

        private void ProcessMusicStateInfo()
        {
            var musicInfo = MusicData.MusicInfo;
            var pos = Bass.ChannelGetPosition(C_int);
            musicInfo.PositionWithoutLatency = Bass.ChannelBytes2Seconds(C_int, pos);
            musicInfo.Position = musicInfo.PositionWithoutLatency - LatencyCompensationInSeconds;
            var num = MusicData.MusicInfo.Position * MusicData.MusicInfo.SamplesPerSeconds;
            var sampleIDFromPosition = MusicData.GetSampleIDFromPosition(num, out var sampleProgress);
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

        #endregion
    }
}