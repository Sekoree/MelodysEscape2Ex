using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using CustomAudioEngine.Entities;
using ManagedBass;
using ManagedBass.Mix;
using MelodyReactor2;
using TagLib;
using Debug = UnityEngine.Debug;
using File = TagLib.File;

namespace CustomAudioEngine
{
    public class TestAudioEngine : AudioEngine
    {
        public static bool USE_OLD_GETFFTDATA = false;
        public static bool LOAD_PLUGINS = true;
        public static bool FORCE_44100_DECODING = true;
        public static bool USE_CONFIG_DEVICE_BUFFER = false;
        public static bool USE_FFT_HANN_WINDOW = true;
        public static bool FORCE_PAUSE_EXACT_POSITION = true;

        private const int A_int = 100;
        private Thread musicAnalyzeThread;
        private int baseAnalyzeStream;
        private int unmixedChannel;
        private bool notMonoAndIsNot44100;
        private int mixChannel;
        private int livestreamChannel;
        private long baseAnalyzeStreamPosition;
        private Stopwatch musicAnalyzeTimer = new Stopwatch();
        private IntPtr windowHandle = IntPtr.Zero;
        private string pluginPath = string.Empty;
        private byte[] A_byteArray;
        private float[] A_floatArray;
        private int channelDataReadLength;
        private DSPProcedure musicAnalyzeDSP;
        private StreamProcedure musicAnalyzeStreamProc;
        private float A_float;
        private float[] a_floatArray;
        private float[] B_floatArray;
        private long a_long;
        private int D_int;
        private int d_int;
        private readonly int E_int;
        private long livestreamChannelPausePosition;
        private int e_int;
        private int F_int;
        private float a_float;

        public TestAudioEngine(IntPtr handle)
        {
            windowHandle = handle;
            musicAnalyzeDSP = A_DSPPROC_Obfuscated;
            musicAnalyzeStreamProc = A_STREAMPROC_Obfuscated;
            if (MReactor.FFTQuality < 1024)
            {
                MReactor.FFTQuality = 1024;
            }

            E_int = MReactor.FFTQuality / 1024;
            a_floatArray = new float[MReactor.FFTQuality * A_int];
            B_floatArray = new float[MReactor.FFTQuality];
            A_byteArray = new byte[MReactor.FFTQuality / 2];
            A_floatArray = new float[MReactor.FFTQuality / 2 * E_int];
            channelDataReadLength = A_Obfuscated(MReactor.FFTQuality);
            if (!USE_FFT_HANN_WINDOW)
            {
                channelDataReadLength |= 32;
            }

            channelDataReadLength |= 16;
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
            var list = new List<string>();
            foreach (var searchPattern2 in searchPattern.Split(';'))
            {
                list.AddRange(Directory.GetFiles(directoryPath, searchPattern2, searchOption));
            }

            for (int num = list.Count - 1; num >= 0; num--)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(list[num]);
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

        private unsafe void A_DSPPROC_Obfuscated(int param0, int param1, IntPtr param2, int param3, IntPtr param4)
        {
            if (param3 != 0 && !(param2 == IntPtr.Zero))
            {
                var num = param3 / 4;
                float* ptr = (float*)(void*)param2;
                for (int i = 0; i < num; i++)
                {
                    *ptr *= MusicData.MusicInfo.PCMNormalizationFactor;
                    ptr++;
                }
            }
        }

        public override void SetPluginsDirectory(string path)
        {
            pluginPath = path;
        }

        /// <summary>
        /// Safely loads Bass plugins
        /// </summary>
        private void a_Obfuscated(string param0)
        {
            var fileName = Path.GetFileName(param0);
            Debug.Log("Loading plugin: " + fileName);
            try
            {
                if (Bass.PluginLoad(param0) != 0)
                {
                    Debug.Log("Loaded plugin: " + fileName);
                }
                else
                {
                    Debug.Log("Failed to load plugin: " + fileName);
                    Debug.Log("Error: " + Bass.LastError);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error loading plugin: " + fileName + " - " + e.Message);
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            if (LOAD_PLUGINS)
            {
                var pluginFiles = GetPluginFiles(pluginPath, "*.dll;*.so", SearchOption.AllDirectories);
                foreach (var pluginFile in pluginFiles)
                {
                    a_Obfuscated(pluginFile);
                }
            }

            SupportedFilesExtensions = Bass.SupportedFormats;
            var flag = false;
            if (USE_CONFIG_DEVICE_BUFFER)
            {
                Bass.Configure(Configuration.DeviceBufferLength, 80);
            }

            Bass.Configure(Configuration.IncludeDefaultDevice, true);
            for (int i = 0; i < 5; i++)
            {
                if (Bass.Init(Flags: DeviceInitFlags.Default, Win: windowHandle))
                {
                    if (LOAD_PLUGINS)
                    {
                        Bass.GetInfo(out var info);
                        Debug.Log("BASS Latency: " + info.Latency);
                    }

                    flag = true;
                    break;
                }

                if (Bass.LastError == Errors.Already)
                {
                    Bass.Free();
                }

                Thread.Sleep(1000);
            }

            if (!flag)
            {
                throw new AudioEngineInitializationException("BASS failed to initialize: " + Bass.LastError);
            }

            Bass.Configure(Configuration.UpdatePeriod, MReactor.UseOwnThread ? 5 : 0);
            Bass.Configure(Configuration.PlaybackBufferLength, 250);
            if (USE_CONFIG_DEVICE_BUFFER)
            {
                Debug.Log("BASS Device Buffer Length: " + Bass.GetConfig(Configuration.PlaybackBufferLength));
            }
        }

        public override void CleanRessources()
        {
            base.CleanRessources();
        }

        public override bool IsReadyToAnalyze()
        {
            return baseAnalyzeStream != 0;
        }

        public override void InitAndAnalyzeMusicFileAsync()
        {
            musicAnalyzeThread = new Thread(InitAndAnalyzeMusicFileAsyncThread);
            musicAnalyzeThread.Start();
        }

        public override void AnalyzeMusicFileAsync()
        {
            musicAnalyzeThread = new Thread(AnalyzeMusicFileAsyncThread);
            musicAnalyzeThread.Start();
        }

        public void InitAndAnalyzeMusicFileAsyncThread()
        {
            if (MReactor.AudioEngine.InitMusicFile())
            {
                MReactor.AudioEngine.AnalyzeMusicFile();
            }
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
                LastError = "Music file is null or empty";
                return false;
            }

            Debug.Log("Loading music file: " + MusicFile);
            if (MusicFile.StartsWith("https://"))
            {
                Debug.Log("Loading music file from URL");
                MusicData.MusicInfo = new WebMusicInfo();
                MusicData.MusicInfo.FileFullPath = MusicFile;
                MusicData.MusicInfo.Filename = MusicFile;
                MusicData.MusicInfo.FilenameWithoutExt = MusicFile;
            }
            else
            {
                Debug.Log("Loading music file from disk");
                MusicData.MusicInfo = new MusicInfo();
                MusicData.MusicInfo.FileFullPath = MusicFile;
                MusicData.MusicInfo.Filename = Path.GetFileName(MusicFile);
                MusicData.MusicInfo.FilenameWithoutExt = Path.GetFileNameWithoutExtension(MusicFile);
            }

            d_int = 0;
            D_int = 0;
            a_long = 0L;
            A_float = 0f;
            if (MusicData.MusicInfo is WebMusicInfo webInfo)
            {
                baseAnalyzeStream = Bass.CreateStream(webInfo.TempPath, 0, 0L,
                    BassFlags.Mono | BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
            }
            else
            {
                baseAnalyzeStream = Bass.CreateStream(MusicFile, 0L, 0L,
                    BassFlags.Mono | BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
            }

            if (baseAnalyzeStream == 0)
            {
                LastError = "BASS failed to create stream: " + Bass.LastError;
                LastErrorCode = Bass.LastError.ToString();
                return false;
            }

            Tag tagInfo = null;
            if (MusicData.MusicInfo is WebMusicInfo info)
            {
                tagInfo = GetTagsFromFile(info.TempPath);
            }
            else
            {
                tagInfo = GetTagsFromFile(MusicFile);
            }

            var channelInfo = Bass.ChannelGetInfo(baseAnalyzeStream);
            notMonoAndIsNot44100 = channelInfo.Channels > 1 || (FORCE_44100_DECODING && channelInfo.Frequency != 44100);
            A_Obfuscated(channelInfo, notMonoAndIsNot44100, tagInfo);
            if (MusicData.MusicInfo.Duration < 10)
            {
                MusicData = null;
                LastError = "Music file is too short";
                LastErrorCode = "TOO_SHORT";
                return false;
            }

            Debug.Log("notMonoAndIsNot44100: " + notMonoAndIsNot44100);
            if (notMonoAndIsNot44100)
            {
                Debug.Log("creating mix streams");
                mixChannel =
                    BassMix.CreateMixerStream(44100, 1, BassFlags.Float | BassFlags.Prescan | BassFlags.Decode);
                if (mixChannel == 0)
                {
                    LastError = "BASS failed to create mixer stream: " + Bass.LastError;
                    LastErrorCode = Bass.LastError.ToString();
                    FreeChannel(ref baseAnalyzeStream);
                    FreeChannel(ref mixChannel);
                    return false;
                }

                if (!BassMix.MixerAddChannel(mixChannel, baseAnalyzeStream, BassFlags.RecordEchoCancel))
                {
                    LastError = "BASS failed to add channel to mixer: " + Bass.LastError;
                    LastErrorCode = Bass.LastError.ToString();
                    FreeChannel(ref baseAnalyzeStream);
                    FreeChannel(ref mixChannel);
                    return false;
                }
            }

            Debug.Log("Music file loaded, creating unmixed stream");
            unmixedChannel = Bass.CreateStream(44100, E_int, BassFlags.Float | BassFlags.Decode, musicAnalyzeStreamProc,
                IntPtr.Zero);
            return true;
        }

        public override void AnalyzeMusicFile()
        {
            try
            {
                if (string.IsNullOrEmpty(MusicFile))
                {
                    throw new Exception("Music file is null or empty");
                }

                musicAnalyzeTimer.Stop();
                musicAnalyzeTimer.Reset();
                musicAnalyzeTimer.Start();

                //weird if here that always calls the same method
                A_Obfuscated();

                //get all internal methods from MusicData
                var methods = MusicData.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);

                //get method "A" with DifficultyRules as parameter
                var method = methods.FirstOrDefault(m => m.Name == "A"
                                                         && m.GetParameters().Length == 1
                                                         && m.GetParameters()[0].ParameterType ==
                                                         typeof(DifficultyRules));

                //call method "A" with DifficultyRules as parameter
                Debug.Log("Calling method A with DifficultyRules as parameter");
                method.Invoke(MusicData, new object[] { MReactor.CurrentDifficultyRules });

                musicAnalyzeTimer.Stop();
                MusicData.MusicInfo.AnalysisTime = musicAnalyzeTimer.Elapsed;
                MusicData.OnMusicAnalyzed();
            }
            catch (Exception e)
            {
                MusicData.OnMusicAnalysisException(e.Message);
            }
        }

        public override void FreeMusicFile()
        {
            FreeChannel(ref baseAnalyzeStream);
            FreeChannel(ref mixChannel);
            FreeChannel(ref unmixedChannel);
        }

        public override void GetFFTData()
        {
            Debug.Log("GetFFTData called");
            if (USE_OLD_GETFFTDATA)
            {
                //Not used I think
                Debug.Log("Old GetFFTData apparently happening");
            }
            else
            {
                Debug.Log("Expected GetFFTData happening");
                b_Obfuscated();
            }
        }

        public override bool LoadMusicFile()
        {
            if (string.IsNullOrEmpty(MusicFile))
            {
                LastError = "Music file is null or empty";
                return false;
            }

            if (livestreamChannel != 0)
            {
                Bass.ChannelStop(livestreamChannel);
                FreeChannel(ref livestreamChannel);
            }

            var flags = BassFlags.Float | BassFlags.Prescan;
            if (MReactor.UseSoftwareSampling)
            {
                flags |= BassFlags.SoftwareMixing;
            }

            if (MusicData.MusicInfo is WebMusicInfo webInfo)
            {
                livestreamChannel = Bass.CreateStream(webInfo.TempPath, 0, 0L, flags);
            }
            else
            {
                livestreamChannel = Bass.CreateStream(MusicFile, 0L, 0L, flags);
            }

            if (livestreamChannel == 0)
            {
                LastError = "BASS failed to create stream: " + Bass.LastError;
                LastErrorCode = Bass.LastError.ToString();
                return false;
            }

            if (MReactor.EnablePlaybackNormalization && MusicData.MusicInfo.PCMNormalizationFactor > 1f)
            {
                Bass.ChannelSetDSP(livestreamChannel, musicAnalyzeDSP, IntPtr.Zero, 0);
            }

            D_Obfuscated();
            return true;
        }

        public void MuteLiveSong()
        {
            if (livestreamChannel == 0)
                return;

            var vol = Bass.ChannelGetAttribute(livestreamChannel, ChannelAttribute.Volume);
            Bass.ChannelSetAttribute(livestreamChannel, ChannelAttribute.Volume, (vol != 0f) ? 0f : 1f);
        }

        public override bool PlayMusicFile()
        {
            if (livestreamChannel == 0)
            {
                LastError = "Music file is not loaded";
                D_Obfuscated();
                return false;
            }

            if (Bass.ChannelPlay(livestreamChannel, true))
            {
                D_Obfuscated();
                return true;
            }

            LastError = "BASS failed to play stream: " + Bass.LastError;
            LastErrorCode = Bass.LastError.ToString();
            return false;
        }

        public override SongState GetLiveStreamState()
        {
            return c_Obfuscated();
        }

        public override void PauseLiveStream()
        {
            if (livestreamChannel == 0)
            {
                return;
            }

            D_Obfuscated();
            if (MusicData.MusicInfo.State != SongState.Playing)
            {
                return;
            }

            if (FORCE_PAUSE_EXACT_POSITION)
            {
                livestreamChannelPausePosition = Bass.ChannelGetPosition(livestreamChannel);
            }

            Bass.ChannelPause(livestreamChannel);
            if (FORCE_PAUSE_EXACT_POSITION)
            {
                Bass.ChannelSetPosition(livestreamChannel, livestreamChannelPausePosition);
            }

            MusicData.MusicInfo.State = SongState.Paused;
        }

        public override void ResumeLiveStream()
        {
            if (livestreamChannel == 0)
            {
                return;
            }

            D_Obfuscated();
            if (MusicData.MusicInfo.State == SongState.Paused)
            {
                Bass.ChannelPlay(livestreamChannel, false);
                MusicData.MusicInfo.State = SongState.Playing;
            }
        }

        public override void StopLiveStream()
        {
            if (livestreamChannel == 0)
            {
                return;
            }

            D_Obfuscated();
            if (MusicData != null && MusicData.MusicInfo.State != SongState.Stopped)
            {
                Bass.ChannelStop(livestreamChannel);
                FreeChannel(ref livestreamChannel);
                MusicData.MusicInfo.State = SongState.Stopped;
                MReactor.ClearRealTimeData();
            }
        }

        public override void UnloadLiveStream()
        {
            Bass.ChannelStop(livestreamChannel);
            FreeChannel(ref livestreamChannel);
            MusicData.MusicInfo.State = SongState.Stopped;
            MReactor.ClearRealTimeData();
        }

        public override void ResetLiveStream()
        {
            if (livestreamChannel == 0)
                return;

            Bass.ChannelPlay(livestreamChannel, true);
            D_Obfuscated();
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

            D_Obfuscated();
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

            Bass.ChannelGetData(livestreamChannel, MReactor.RealTimeFFTData, -2147483644);

            //get all internal static fields from MReactor
            var fields = typeof(MReactor).GetFields(BindingFlags.Static | BindingFlags.NonPublic);

            //get field "A" with type int
            var aIntField = fields.FirstOrDefault(f => f.FieldType == typeof(int) && f.Name == "A");

            //get field "A" with type byte[]
            var aByteArrayField = fields.FirstOrDefault(f => f.FieldType == typeof(byte[]) && f.Name == "A");
            var tempAInt = (int?)aIntField?.GetValue(null);

            var tempAByteArray = (byte[])aByteArrayField?.GetValue(null);
            if (tempAByteArray == null)
                throw new Exception("Could not get A byte array from MReactor");

            for (int i = 2; i < tempAInt; i++)
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
                    MReactor.RealTimeByteFFTData[i] = tempAByteArray[(int)(num * 1000000f)];
                }
            }

            aByteArrayField.SetValue(null, tempAByteArray);
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

        private void D_Obfuscated()
        {
            if (MusicData != null)
            {
                MusicData.MusicInfo.State = c_Obfuscated();
                d_Obfuscated();
            }
        }

        public override void SetPosition(double seconds)
        {
            Bass.ChannelSetPosition(livestreamChannel, Bass.ChannelSeconds2Bytes(livestreamChannel, seconds));
            D_Obfuscated();
        }

        public override void ResetPosition()
        {
            Bass.ChannelSetPosition(livestreamChannel, 0L);
            D_Obfuscated();
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
            Bass.ChannelSetAttribute(livestreamChannel, ChannelAttribute.Frequency,
                (float)MusicData.MusicInfo.SampleRatePlayback * speedFactor);
        }

        private void d_Obfuscated()
        {
            var pos = Bass.ChannelGetPosition(livestreamChannel);
            MusicData.MusicInfo.Position = Bass.ChannelBytes2Seconds(livestreamChannel, pos);
            if (MusicData.MusicInfo.Position < 0.0)
            {
                MusicData.MusicInfo.Position = 0.0;
            }

            double num = MusicData.MusicInfo.Position * MusicData.MusicInfo.SamplesPerSeconds;
            float num2 = (float)(num - Math.Truncate(num));
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

        private SongState c_Obfuscated()
        {
            if (livestreamChannel == 0)
            {
                return SongState.Stopped;
            }

            switch (Bass.ChannelIsActive(livestreamChannel))
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

        private void b_Obfuscated()
        {
            Debug.Log("b_Obfuscated called");
            Bass.ChannelSetPosition(notMonoAndIsNot44100 ? mixChannel : baseAnalyzeStream, 0L);
            d_int = 0;
            a_float = 0f;
            e_int = 0;
            F_int = 0;
            MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT);
            Debug.Log("FFT status set");
            var num = 0;
            PointF dECIBELS_RANGE = MReactor.DECIBELS_RANGE;
            PointF dECIBELS_RANGE2 = MReactor.DECIBELS_RANGE;

            //get all internal static fields from MReactor
            var fields = typeof(MReactor).GetFields(BindingFlags.Static | BindingFlags.NonPublic);

            //get field "A" with type int
            var aIntField = fields.FirstOrDefault(f => f.FieldType == typeof(int) && f.Name == "A");

            //get field "A" with type byte[]
            var aByteArrayField = fields.FirstOrDefault(f => f.FieldType == typeof(byte[]) && f.Name == "A");

            var num2 = (int?)aIntField?.GetValue(null);
            var num3 = 0;
            A_byteArray[0] = 0;
            A_byteArray[1] = 0;

            var tempArray = (byte[])aByteArrayField?.GetValue(null);
            if (tempArray == null)
                throw new Exception("Could not get byte array from MReactor");

            Debug.Log("FFT loop started");
            Debug.Log("baseChannel length: " + Bass.ChannelGetLength(baseAnalyzeStream));
            Debug.Log("mixChannel length: " + Bass.ChannelGetLength(mixChannel));
            Debug.Log("channelDataRead length: " + channelDataReadLength);
            while (Bass.ChannelGetData(unmixedChannel, A_floatArray, channelDataReadLength) > 0)
            {
                //Debug.Log("FFT loop iteration: " + num);
                for (int i = 0; i < E_int; i++)
                {
                    byte b = 0;
                    for (int j = 2; j < num2; j++)
                    {
                        float num4 = A_floatArray[j * E_int + i];
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

                        byte b2 = tempArray[(int)(num4 * 1000000f)];
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


            Debug.Log("FFT loop ended");
            aByteArrayField?.SetValue(null, tempArray);
            Debug.Log("Set MReactor.A to updated array");
            MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT, 100);
            B_Obfuscated();
        }

        private void B_Obfuscated()
        {
            Debug.Log("B_Obfuscated called, just some number stuff");
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

            Debug.Log("B_Obfuscated ended");
        }

        private void A_Obfuscated()
        {
            GetFFTData();
            FreeMusicFile();

            //get all internal methods of MusicData with reflection
            var methods = typeof(MusicData).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);

            //get methods f with no parameters, G with no parameters and c with no parameters
            var f = methods.FirstOrDefault(m => m.Name == "f" && m.GetParameters().Length == 0);
            var G = methods.FirstOrDefault(m => m.Name == "G" && m.GetParameters().Length == 0);
            var c = methods.FirstOrDefault(m => m.Name == "c" && m.GetParameters().Length == 0);

            //invoke methods
            Debug.Log("Invoking MusicData.f");
            f.Invoke(MusicData, null);
            Debug.Log("Invoking MusicData.G");
            G.Invoke(MusicData, null);
            Debug.Log("Invoking MusicData.c");
            c.Invoke(MusicData, null);

            if (!MReactor.EnableHeldNoteDetection)
                return;

            //get method h with no parameters
            var h = methods.FirstOrDefault(m => m.Name == "h" && m.GetParameters().Length == 0);
            //invoke method
            Debug.Log("Invoking MusicData.h");
            h.Invoke(MusicData, null);
        }

        private void A_Obfuscated(ChannelInfo channelInfo, bool param1, Tag tags)
        {
            baseAnalyzeStreamPosition = Bass.ChannelGetLength(baseAnalyzeStream);
            var asSeconds = Bass.ChannelBytes2Seconds(baseAnalyzeStream, baseAnalyzeStreamPosition);

            //get internal method "a" from MusicData with params (int, int, double) with reflection
            var methods = typeof(MusicData).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            var method = methods.FirstOrDefault(m => m.Name == "a" && m.GetParameters().Length == 3
                                                                   && m.GetParameters()[0].ParameterType == typeof(int)
                                                                   && m.GetParameters()[1].ParameterType == typeof(int)
                                                                   && m.GetParameters()[2].ParameterType ==
                                                                   typeof(double));
            method?.Invoke(MusicData,
                new object[] { param1 ? 44100 : channelInfo.Frequency, channelInfo.Channels, asSeconds });

            MusicData.MusicInfo.SampleRatePlayback = channelInfo.Frequency;
            if (tags == null)
            {
                return;
            }

            MusicData.MusicInfo.TagAlbum = tags.Album;
            MusicData.MusicInfo.TagArtist = tags.FirstPerformer;
            MusicData.MusicInfo.TagTitle = tags.Title;
            MusicData.MusicInfo.TagComment = tags.Comment;
            MusicData.MusicInfo.BPM = tags.BeatsPerMinute;
            if (string.IsNullOrWhiteSpace(tags.Comment))
            {
                return;
            }

            var moreBPMs = new List<double>();
            var comments = tags.Comment.Split(';');
            for (int i = 0; i < comments.Length; i++)
            {
                if (comments[i] == "3/4")
                {
                    MusicData.MusicInfo.Is34TimingTag = true;
                    continue;
                }

                var tryNumber = double.TryParse(comments[i], out var bpm);
                if (tryNumber && bpm > 2.0)
                {
                    moreBPMs.Add(bpm);
                }
            }

            if (moreBPMs.Count > 0)
            {
                MusicData.MusicInfo.AdditionalTagBPMs = moreBPMs.ToArray();
            }
        }

        private Tag GetTagsFromFile(string filepath)
        {
            try
            {
                using (var tagFile = TagLib.File.Create(filepath))
                {
                    return tagFile.Tag;
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error getting tags from file: " + filepath + " - " + e.Message);
                return null;
            }
        }

        private unsafe int A_STREAMPROC_Obfuscated(int param0, IntPtr param1, int param2, IntPtr param3)
        {
            if (param2 == 0 || param1 == IntPtr.Zero)
            {
                return 0;
            }

            float* ptr = (float*)(void*)param1;
            int num = param2 / 4 / E_int;
            int num2 = num / E_int;
            int num3 = B_floatArray.Length;
            int num4 = a_floatArray.Length - num3;
            double num5 = 1024.0;
            int num6 = 2;
            bool flag = false;
            if (D_int <= 0 || d_int >= num4)
            {
                long a2 = a_long;
                if (D_int > 0)
                {
                    Array.Copy(a_floatArray, num4, B_floatArray, 0, num3);
                    flag = true;
                }

                d_int = 0;
                int num7 = Bass.ChannelGetData(notMonoAndIsNot44100 ? mixChannel : baseAnalyzeStream, a_floatArray,
                    a_floatArray.Length * 4);
                D_int = num7 / 4;
                a_long += D_int;
                for (int i = 0; i < d_int; i++)
                {
                    float num8 = a_floatArray[i];
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

                    float num9 = (float)(A_float / num5);
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

            float num10 = 0f;
            int num11 = 0;
            for (int j = 0; j < num; j++)
            {
                int num12 = j * E_int;
                for (int k = 0; k < E_int; k++)
                {
                    int num13 = d_int + j + num2 * k;
                    if (num13 < D_int)
                    {
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
            }

            d_int += num;
            if (flag)
            {
                d_int = 0;
            }

            return num11 * 4;
        }

        /// <summary>
        /// Probably decides the BassFlags
        /// </summary>
        private static int A_Obfuscated(int fftQuality)
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
    }
}