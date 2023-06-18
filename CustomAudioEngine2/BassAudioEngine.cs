// MelodyReactor2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MelodyReactor2.BassAudioEngine
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MelodyReactor2;

public class BassAudioEngine : AudioEngine
{
	public static bool USE_OLD_GETFFTDATA = false;

	public static bool LOAD_PLUGINS = true;

	public static bool FORCE_44100_DECODING = true;

	public static bool USE_CONFIG_DEVICE_BUFFER = false;

	public static bool USE_FFT_HANN_WINDOW = true;

	public static bool FORCE_PAUSE_EXACT_POSITION = true;

	private const int m_A = 100;

	private Thread m_A;

	private int m_a;

	private int m_B;

	private bool m_A;

	private int m_b;

	private int m_C;

	private long m_A;

	private Stopwatch m_A = new Stopwatch();

	private IntPtr m_A = IntPtr.Zero;

	private string m_A = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.A();

	private byte[] m_A;

	private float[] m_A;

	private int m_c;

	private DSPPROC m_A;

	private STREAMPROC m_A;

	private float m_A;

	private float[] m_a;

	private float[] m_B;

	private long m_a;

	private int m_D;

	private int m_d;

	private readonly int E;

	private long m_B;

	private int e;

	private int F;

	private float m_a;

	public BassAudioEngine(IntPtr handle)
	{
		this.m_A = handle;
		this.m_A = A;
		this.m_A = A;
		if (MReactor.FFTQuality < 1024)
		{
			MReactor.FFTQuality = 1024;
		}
		E = MReactor.FFTQuality / 1024;
		this.m_a = new float[MReactor.FFTQuality * 100];
		this.m_B = new float[MReactor.FFTQuality];
		this.m_A = new byte[MReactor.FFTQuality / 2];
		this.m_A = new float[MReactor.FFTQuality / 2 * E];
		this.m_c = A(MReactor.FFTQuality);
		if (!USE_FFT_HANN_WINDOW)
		{
			this.m_c |= 32;
		}
		this.m_c |= 16;
	}

	private static int A(int P_0)
	{
		return P_0 switch
		{
			256 => int.MinValue, 
			512 => -2147483647, 
			1024 => -2147483646, 
			2048 => -2147483645, 
			4096 => -2147483644, 
			16384 => -2147483642, 
			_ => -2147483643, 
		};
	}

	public void FreeChannel(ref int channel)
	{
		if (channel != 0)
		{
			Bass.BASS_StreamFree(channel);
		}
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
			if (!fileNameWithoutExtension.StartsWith(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.at(), ignoreCase: false, CultureInfo.InvariantCulture))
			{
				list.RemoveAt(num);
			}
			else if (fileNameWithoutExtension == C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.at() || fileNameWithoutExtension == C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.aU())
			{
				list.RemoveAt(num);
			}
		}
		list.Sort();
		return list;
	}

	private string A(string P_0)
	{
		byte[] bytes = Convert.FromBase64String(P_0);
		return Encoding.UTF8.GetString(bytes);
	}

	public override void SetPluginsDirectory(string path)
	{
		this.m_A = path;
	}

	private void a(string P_0)
	{
		string fileName = Path.GetFileName(P_0);
		Console.WriteLine(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.au() + fileName + C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.aV());
		try
		{
			if (Bass.BASS_PluginLoad(P_0) == 0)
			{
				BASSError bASSError = Bass.BASS_ErrorGetCode();
				string text = bASSError switch
				{
					BASSError.BASS_ERROR_FILEOPEN => C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.av(), 
					BASSError.BASS_ERROR_FILEFORM => C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.aW(), 
					BASSError.BASS_ERROR_ALREADY => C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.aw(), 
					_ => bASSError.ToString(), 
				};
				Console.WriteLine(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.aX() + fileName + C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.ax() + text + C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.ah());
			}
			else
			{
				Console.WriteLine(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.aY() + fileName + C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.ay());
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.aZ() + ex.Message + C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.ah());
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		BassNet.Registration(A(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.az()) + A(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BA()) + A(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Ba()), A(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BB()));
		if (LOAD_PLUGINS)
		{
			if (Directory.Exists(this.m_A))
			{
				List<string> pluginFiles = GetPluginFiles(this.m_A, C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bb(), SearchOption.TopDirectoryOnly);
				Console.WriteLine(string.Format(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BC(), pluginFiles.Count, this.m_A));
				foreach (string item in pluginFiles)
				{
					a(item);
				}
			}
			else
			{
				Console.WriteLine(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bc() + this.m_A + C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BD());
			}
		}
		SupportedFilesExtensions = Utils.BASSAddOnGetSupportedFileExtensions(null);
		Console.WriteLine(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bd() + SupportedFilesExtensions);
		int i = 0;
		bool flag = false;
		BASSError bASSError = BASSError.BASS_OK;
		if (USE_CONFIG_DEVICE_BUFFER)
		{
			int num = 80;
			Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_DEV_BUFFER, num);
			Console.WriteLine(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BE() + num);
		}
		Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_DEV_DEFAULT, newvalue: true);
		for (; i < 5; i++)
		{
			if (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.m_A))
			{
				if (LOAD_PLUGINS)
				{
					BASS_INFO bASS_INFO = new BASS_INFO();
					Bass.BASS_GetInfo(bASS_INFO);
					Console.WriteLine(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Be() + bASS_INFO.ToString());
				}
				flag = true;
				break;
			}
			bASSError = Bass.BASS_ErrorGetCode();
			if (bASSError == BASSError.BASS_ERROR_ALREADY)
			{
				Bass.BASS_Free();
			}
			Thread.Sleep(1000);
		}
		if (!flag)
		{
			throw new AudioEngineInitializationException(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BF() + bASSError);
		}
		Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, MReactor.UseOwnThread ? 5 : 0);
		Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 250);
		Bass.BASS_GetInfo(new BASS_INFO());
		if (USE_CONFIG_DEVICE_BUFFER)
		{
			Console.WriteLine(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bf() + Bass.BASS_GetConfig(BASSConfig.BASS_CONFIG_DEV_BUFFER));
		}
	}

	public override void CleanRessources()
	{
		base.CleanRessources();
	}

	public override bool IsReadyToAnalyze()
	{
		return this.m_a != 0;
	}

	public override void InitAndAnalyzeMusicFileAsync()
	{
		this.m_A = new Thread(InitAndAnalyzeMusicFileAsyncThread);
		this.m_A.Start();
	}

	public override void AnalyzeMusicFileAsync()
	{
		this.m_A = new Thread(AnalyzeMusicFileAsyncThread);
		this.m_A.Start();
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

	public override bool InitMusicFile(bool ignoreShortDurationError)
	{
		base.InitMusicFile(ignoreShortDurationError);
		if (base.MusicFile != null && !string.IsNullOrEmpty(base.MusicFile))
		{
			string text = base.MusicFile;
			if (MReactor.AddSpecialPrefixToLongPathNames && text.Length > 260)
			{
				text = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BG() + text;
			}
			MusicData.MusicInfo = new MusicInfo();
			MusicData.MusicInfo.FileFullPath = base.MusicFile;
			MusicData.MusicInfo.FileFullPathLongSafe = text;
			MusicData.MusicInfo.Filename = Path.GetFileName(base.MusicFile);
			MusicData.MusicInfo.FilenameWithoutExt = Path.GetFileNameWithoutExtension(base.MusicFile);
			this.m_d = 0;
			this.m_D = 0;
			this.m_a = 0L;
			this.m_A = 0f;
			this.m_a = Bass.BASS_StreamCreateFile(text, 0L, 0L, BASSFlag.BASS_SAMPLE_MONO | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN | BASSFlag.BASS_STREAM_DECODE);
			if (this.m_a != 0)
			{
				BassTags.ReadPictureTAGs = false;
				TAG_INFO tAG_INFO = BassTags.BASS_TAG_GetFromFile(text);
				BASS_CHANNELINFO bASS_CHANNELINFO = new BASS_CHANNELINFO();
				Bass.BASS_ChannelGetInfo(this.m_a, bASS_CHANNELINFO);
				this.m_A = bASS_CHANNELINFO.chans > 1 || (FORCE_44100_DECODING && bASS_CHANNELINFO.freq != 44100);
				A(bASS_CHANNELINFO, this.m_A, tAG_INFO);
				if (MusicData.MusicInfo.Duration < 10.0 && !ignoreShortDurationError)
				{
					MusicData = null;
					base.LastError = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bg();
					base.LastErrorCode = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BH();
					return false;
				}
				if (this.m_A)
				{
					this.m_b = BassMix.BASS_Mixer_StreamCreate(44100, 1, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN | BASSFlag.BASS_STREAM_DECODE);
					if (this.m_b == 0)
					{
						MusicData = null;
						base.LastErrorCode = Bass.BASS_ErrorGetCode().ToString();
						base.LastError = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bh() + base.LastErrorCode;
						FreeChannel(ref this.m_a);
						FreeChannel(ref this.m_b);
						return false;
					}
					if (!BassMix.BASS_Mixer_StreamAddChannel(this.m_b, this.m_a, BASSFlag.BASS_RECORD_ECHOCANCEL))
					{
						MusicData = null;
						base.LastErrorCode = Bass.BASS_ErrorGetCode().ToString();
						base.LastError = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BI() + base.LastErrorCode;
						FreeChannel(ref this.m_a);
						FreeChannel(ref this.m_b);
						return false;
					}
					BASS_CHANNELINFO info = new BASS_CHANNELINFO();
					Bass.BASS_ChannelGetInfo(this.m_b, info);
				}
				this.m_B = Bass.BASS_StreamCreate(44100, E, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE, this.m_A, IntPtr.Zero);
				return true;
			}
			MusicData = null;
			base.LastErrorCode = Bass.BASS_ErrorGetCode().ToString();
			base.LastError = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bi() + base.LastErrorCode;
			if (base.LastErrorCode == C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BJ())
			{
				base.LastError = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bj();
			}
			else if (base.LastErrorCode == C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BK() && base.MusicFile.Length > 260)
			{
				base.LastErrorCode = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bk();
			}
			return false;
		}
		base.LastError = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BL();
		return false;
	}

	public override void AnalyzeMusicFile()
	{
		try
		{
			if (base.MusicFile == string.Empty)
			{
				throw new Exception(C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bl());
			}
			this.m_A.Stop();
			this.m_A.Reset();
			this.m_A.Start();
			if (!MReactor.CacheOnsetsAreas)
			{
				A();
			}
			else if (false)
			{
				a();
			}
			else
			{
				A();
			}
			MusicData.A(MReactor.CurrentDifficultyRules);
			this.m_A.Stop();
			MusicData.MusicInfo.AnalysisTime = this.m_A.Elapsed;
			MusicData.OnMusicAnalyzed();
		}
		catch (Exception ex)
		{
			MusicData.OnMusicAnalysisException(ex.Message);
		}
	}

	private void A()
	{
		_ = this.m_A.ElapsedMilliseconds;
		GetFFTData();
		FreeMusicFile();
		MusicData.f();
		MusicData.G();
		MusicData.c();
		if (MReactor.EnableHeldNoteDetection)
		{
			MusicData.h();
		}
	}

	private string B(string P_0)
	{
		SHA1CryptoServiceProvider sHA1CryptoServiceProvider = new SHA1CryptoServiceProvider();
		byte[] bytes = Encoding.Unicode.GetBytes(P_0);
		return BitConverter.ToString(sHA1CryptoServiceProvider.ComputeHash(bytes));
	}

	private void a()
	{
		FreeMusicFile();
		MusicData.g();
		MusicData.c();
	}

	public override void FreeMusicFile()
	{
		FreeChannel(ref this.m_a);
		FreeChannel(ref this.m_b);
		FreeChannel(ref this.m_B);
	}

	private void A(BASS_CHANNELINFO P_0, bool P_1, TAG_INFO P_2)
	{
		int freq = P_0.freq;
		int chans = P_0.chans;
		this.m_A = Bass.BASS_ChannelGetLength(this.m_a);
		double num = Bass.BASS_ChannelBytes2Seconds(this.m_a, this.m_A);
		MusicData.a(P_1 ? 44100 : freq, chans, num);
		MusicData.MusicInfo.SampleRatePlayback = freq;
		if (P_2 != null)
		{
			MusicData.MusicInfo.TagAlbum = P_2.album;
			MusicData.MusicInfo.TagArtist = P_2.artist;
			MusicData.MusicInfo.TagTitle = P_2.title;
			MusicData.MusicInfo.TagComment = P_2.comment;
			MusicData.MusicInfo.TagBPM = b(P_2.bpm);
		}
	}

	private double b(string P_0)
	{
		try
		{
			if (P_0 == string.Empty)
			{
				return 0.0;
			}
			return double.Parse(P_0.Trim(), CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			return 0.0;
		}
	}

	private unsafe void A(int P_0, int P_1, IntPtr P_2, int P_3, IntPtr P_4)
	{
		if (P_3 != 0 && !(P_2 == IntPtr.Zero))
		{
			int num = P_3 / 4;
			float* ptr = (float*)(void*)P_2;
			for (int i = 0; i < num; i++)
			{
				*ptr *= MusicData.MusicInfo.PCMNormalizationFactor;
				ptr++;
			}
		}
	}

	private void B()
	{
		if (this.m_a <= 0.005f)
		{
			this.m_a = 1f;
		}
		else if (this.m_a <= 0.05f)
		{
			this.m_a = 0.05f;
		}
		else if (this.m_a > 0.99f)
		{
			this.m_a = 1f;
		}
		MusicData.MusicInfo.PCMNormalizationFactor = 1f / this.m_a;
		if (MusicData.MusicInfo.PCMNormalizationFactor > 5f)
		{
			MusicData.MusicInfo.PCMNormalizationFactor = 5f;
		}
		if (MusicData.MusicInfo.PCMNormalizationFactor < 1.1f)
		{
			MusicData.MusicInfo.PCMNormalizationFactor = 1f;
		}
	}

	private unsafe int A(int P_0, IntPtr P_1, int P_2, IntPtr P_3)
	{
		if (P_2 == 0 || P_1 == IntPtr.Zero)
		{
			return 0;
		}
		float* ptr = (float*)(void*)P_1;
		int num = P_2 / 4 / E;
		int num2 = num / E;
		int num3 = this.m_B.Length;
		int num4 = this.m_a.Length - num3;
		double num5 = 1024.0;
		int num6 = 2;
		bool flag = false;
		if (this.m_D <= 0 || this.m_d >= num4)
		{
			_ = this.m_a;
			if (this.m_D > 0)
			{
				Array.Copy(this.m_a, num4, this.m_B, 0, num3);
				flag = true;
			}
			this.m_d = 0;
			int num7 = Bass.BASS_ChannelGetData(this.m_A ? this.m_b : this.m_a, this.m_a, this.m_a.Length * 4);
			this.m_D = num7 / 4;
			this.m_a += this.m_D;
			for (int i = 0; i < this.m_D; i++)
			{
				float num8 = this.m_a[i];
				if (num8 < 0f)
				{
					num8 = 0f - num8;
				}
				if (num8 > 1f)
				{
					num8 = 1f;
				}
				this.m_A += num8;
				e++;
				if (e < 1024)
				{
					continue;
				}
				float num9 = (float)((double)this.m_A / num5);
				if (F >= num6 && F - num6 < MusicData.MusicInfo.TotalSamples)
				{
					if (num9 < 0.001f)
					{
						num9 = 0f;
					}
					if (num9 > 1f)
					{
						num9 = 1f;
					}
					MusicData.RawPCMLevels[F - num6] = (byte)(num9 * 255f);
				}
				F++;
				e = 0;
				this.m_A = 0f;
			}
		}
		float num10 = 0f;
		int num11 = 0;
		for (int j = 0; j < num; j++)
		{
			int num12 = j * E;
			for (int k = 0; k < E; k++)
			{
				int num13 = this.m_d + j + num2 * k;
				if (num13 < this.m_D)
				{
					num10 = (ptr[num12 + k] = ((!flag) ? this.m_a[num13] : ((num13 >= num3) ? this.m_a[num13 - num3] : this.m_B[num13])));
					num11++;
					if (num10 < 0f)
					{
						num10 = 0f - num10;
					}
					if (num10 > this.m_a)
					{
						this.m_a = num10;
					}
				}
			}
		}
		this.m_d += num;
		if (flag)
		{
			this.m_d = 0;
		}
		return num11 * 4;
	}

	public override void GetFFTData()
	{
		bool flag = false;
		if (USE_OLD_GETFFTDATA)
		{
			C();
		}
		else
		{
			b();
		}
		if (MReactor.CacheMusicSamples && !flag)
		{
			Path.GetFileNameWithoutExtension(base.MusicFile);
		}
	}

	internal void b()
	{
		Bass.BASS_ChannelSetPosition(this.m_A ? this.m_b : this.m_a, 0L);
		this.m_d = 0;
		this.m_a = 0f;
		e = 0;
		F = 0;
		MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT);
		int num = 0;
		_ = MReactor.DECIBELS_RANGE;
		_ = MReactor.DECIBELS_RANGE;
		int num2 = MReactor.A;
		int num3 = 0;
		this.m_A[0] = 0;
		this.m_A[1] = 0;
		while (Bass.BASS_ChannelGetData(this.m_B, this.m_A, this.m_c) > 0)
		{
			for (int i = 0; i < E; i++)
			{
				byte b = 0;
				for (int j = 2; j < num2; j++)
				{
					float num4 = this.m_A[j * E + i];
					if (num4 < MReactor.DECIBEL_MIN_VALUE)
					{
						this.m_A[j] = 0;
						continue;
					}
					if (num4 > 0.9999f)
					{
						this.m_A[j] = 254;
						b = 254;
						continue;
					}
					byte b2 = MReactor.A[(int)(num4 * 1000000f)];
					this.m_A[j] = b2;
					if (b2 > b)
					{
						b = b2;
					}
				}
				Array.Copy(this.m_A, MusicData.Samples[num3].FFT, MReactor.FFTArrayLimitedSize);
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
		MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT, 100);
		B();
	}

	private void C()
	{
		if (MReactor.FFTQuality < 1024)
		{
			MReactor.FFTQuality = 1024;
		}
		int num = 0;
		if (MusicData.MusicInfo.Duration > 0.0)
		{
			num = (int)Math.Ceiling((double)MusicData.MusicInfo.SampleRate / (double)MReactor.FFTQuality * MusicData.MusicInfo.Duration);
		}
		Bass.BASS_ChannelSetPosition(this.m_a, 0L);
		int num2 = 4096;
		if (MusicData.MusicInfo.IsStereo)
		{
			num2 *= 2;
		}
		if (MusicData.MusicInfo.PCMNormalizationFactor > 1f)
		{
			Bass.BASS_ChannelSetDSP(this.m_A ? this.m_b : this.m_a, this.m_A, IntPtr.Zero, 0);
		}
		int handle = (this.m_A ? this.m_b : this.m_a);
		int num3 = 0;
		float[] array = new float[MReactor.FFTQuality / 2];
		int num4 = A(MReactor.FFTQuality);
		if (!USE_FFT_HANN_WINDOW)
		{
			num4 |= 0x20;
		}
		MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT);
		float[] array2 = new float[10];
		int num5 = 0;
		int num6 = MReactor.FFTQuality / 1024;
		float num7 = MReactor.DECIBELS_RANGE.Y - MReactor.DECIBELS_RANGE.X;
		int num8 = MReactor.A;
		for (int i = 0; i < num6; i++)
		{
			num3 = i;
			long pos = i * num2;
			if (this.m_A)
			{
				double num9 = (double)MusicData.MusicInfo.SampleRate / (double)MusicData.MusicInfo.SampleRatePlayback;
				pos = (long)((double)i * ((double)num2 / num9));
			}
			Bass.BASS_ChannelSetPosition(this.m_a, pos);
			for (int j = 0; j < num; j++)
			{
				if (num3 >= MusicData.MusicInfo.TotalSamples)
				{
					continue;
				}
				Bass.BASS_ChannelGetData(handle, array, num4);
				array[0] = 0f;
				array[1] = 0f;
				for (int k = 2; k < num8; k++)
				{
					float num10 = array[k];
					if (num10 < MReactor.DECIBEL_MIN_VALUE)
					{
						this.m_A[k] = 0;
						continue;
					}
					float num11 = 20f * (float)Math.Log10(num10);
					if (num11 < MReactor.DECIBELS_RANGE.X)
					{
						num11 = MReactor.DECIBELS_RANGE.X;
					}
					else if (num11 > MReactor.DECIBELS_RANGE.Y)
					{
						num11 = MReactor.DECIBELS_RANGE.Y;
					}
					num10 = (MReactor.DECIBELS_RANGE.X - num11) / num7;
					if (num10 < 0f)
					{
						num10 = 0f - num10;
					}
					this.m_A[k] = (byte)(num10 * 255f);
				}
				if (num3 >= 240 && num3 < 250)
				{
					int num12 = 0;
					for (int l = 0; l < num8; l++)
					{
						num12 += this.m_A[l];
					}
					array2[num3 - 240] = (int)(byte)(num12 / num8);
				}
				num3 += num6;
				if (num3 >= MusicData.MusicInfo.TotalSamples)
				{
					break;
				}
				double num13 = (double)(num3 + 1) / (double)MusicData.MusicInfo.TotalSamples;
				double num14 = 1f / (float)num6;
				int num15 = (int)Math.Round(((double)i * num14 + num13 * num14) * 100.0);
				if (num15 != num5)
				{
					num5 = num15;
					MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT, num15);
				}
			}
		}
		MusicData.OnLoadingStatusChanged(MusicLoadingStatus.PerformingFFT, 100);
	}

	public override bool LoadMusicFile(bool loop)
	{
		if (base.MusicFile != string.Empty)
		{
			if (this.m_C != 0)
			{
				Bass.BASS_ChannelStop(this.m_C);
				FreeChannel(ref this.m_C);
			}
			BASSFlag bASSFlag = BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN;
			if (loop)
			{
				bASSFlag |= BASSFlag.BASS_SAMPLE_LOOP;
			}
			if (MReactor.UseSoftwareSampling)
			{
				bASSFlag |= BASSFlag.BASS_SAMPLE_SOFTWARE;
			}
			string text = base.MusicFile;
			if (MReactor.AddSpecialPrefixToLongPathNames && text.Length > 260)
			{
				text = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BG() + text;
			}
			this.m_C = Bass.BASS_StreamCreateFile(text, 0L, 0L, bASSFlag);
			if (this.m_C != 0)
			{
				if (MReactor.EnablePlaybackNormalization && MusicData.MusicInfo.PCMNormalizationFactor > 1f)
				{
					Bass.BASS_ChannelSetDSP(this.m_C, this.m_A, IntPtr.Zero, 0);
				}
				D();
				return true;
			}
			base.LastErrorCode = Bass.BASS_ErrorGetCode().ToString();
			base.LastError = base.LastErrorCode;
			return false;
		}
		base.LastError = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BM();
		return false;
	}

	public void MuteLiveSong()
	{
		if (this.m_C != 0)
		{
			float value = 0f;
			Bass.BASS_ChannelGetAttribute(this.m_C, BASSAttribute.BASS_ATTRIB_VOL, ref value);
			Bass.BASS_ChannelSetAttribute(value: (value != 0f) ? 0f : 1f, handle: this.m_C, attrib: BASSAttribute.BASS_ATTRIB_VOL);
		}
	}

	public override bool PlayMusicFile()
	{
		if (this.m_C != 0)
		{
			if (!Bass.BASS_ChannelPlay(this.m_C, restart: true))
			{
				BASSError bASSError = Bass.BASS_ErrorGetCode();
				base.LastErrorCode = bASSError.ToString();
				if (bASSError == BASSError.BASS_ERROR_BUFLOST)
				{
					base.LastError = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.Bm();
				}
				else
				{
					base.LastError = bASSError.ToString();
				}
				return false;
			}
			D();
			return true;
		}
		base.LastError = C5C9ABC1_002D2D8E_002D4B13_002D8B77_002DD687194E8BFD.BN();
		D();
		return false;
	}

	public override SongState GetLiveStreamState()
	{
		return c();
	}

	private SongState c()
	{
		SongState result = SongState.Stopped;
		if (this.m_C != 0)
		{
			switch (Bass.BASS_ChannelIsActive(this.m_C))
			{
			case BASSActive.BASS_ACTIVE_PAUSED:
				result = SongState.Paused;
				break;
			case BASSActive.BASS_ACTIVE_PLAYING:
			case BASSActive.BASS_ACTIVE_STALLED:
				result = SongState.Playing;
				break;
			case BASSActive.BASS_ACTIVE_STOPPED:
				result = SongState.Stopped;
				break;
			}
		}
		return result;
	}

	private void D()
	{
		SongState state = c();
		if (MusicData != null)
		{
			MusicData.MusicInfo.State = state;
			d();
		}
	}

	public override void PauseLiveStream()
	{
		if (this.m_C == 0)
		{
			return;
		}
		D();
		if (MusicData.MusicInfo.State == SongState.Playing)
		{
			if (FORCE_PAUSE_EXACT_POSITION)
			{
				this.m_B = Bass.BASS_ChannelGetPosition(this.m_C);
			}
			Bass.BASS_ChannelPause(this.m_C);
			if (FORCE_PAUSE_EXACT_POSITION)
			{
				Bass.BASS_ChannelSetPosition(this.m_C, this.m_B);
			}
			MusicData.MusicInfo.State = SongState.Paused;
		}
	}

	public override void ResumeLiveStream()
	{
		if (this.m_C != 0)
		{
			D();
			if (MusicData.MusicInfo.State == SongState.Paused)
			{
				Bass.BASS_ChannelPlay(this.m_C, restart: false);
				MusicData.MusicInfo.State = SongState.Playing;
			}
		}
	}

	public override void StopLiveStream()
	{
		if (this.m_C != 0)
		{
			D();
			if (MusicData != null && MusicData.MusicInfo.State != SongState.Stopped)
			{
				Bass.BASS_ChannelStop(this.m_C);
				FreeChannel(ref this.m_C);
				MusicData.MusicInfo.State = SongState.Stopped;
				MReactor.ClearRealTimeData();
			}
		}
	}

	public override void UnloadLiveStream()
	{
		Bass.BASS_ChannelStop(this.m_C);
		FreeChannel(ref this.m_C);
		MusicData.MusicInfo.State = SongState.Stopped;
		MReactor.ClearRealTimeData();
	}

	public override void ResetLiveStream()
	{
		if (this.m_C != 0)
		{
			Bass.BASS_ChannelPlay(this.m_C, restart: true);
			D();
		}
	}

	public override void Update(float elapsedSeconds)
	{
		if (MusicData != null && MusicData.MusicInfo != null)
		{
			int num = (int)(elapsedSeconds * 2000f);
			if (num < 10)
			{
				num = 10;
			}
			if (!MReactor.UseOwnThread)
			{
				Bass.BASS_Update(num);
			}
			D();
			if (MReactor.EnableRealTimeFrequencyData && MusicData.MusicInfo.State == SongState.Playing)
			{
				UpdateRealTimeFrequencyData();
			}
		}
	}

	private void d()
	{
		MusicInfo musicInfo = MusicData.MusicInfo;
		long pos = Bass.BASS_ChannelGetPosition(this.m_C);
		musicInfo.PositionWithoutLatency = Bass.BASS_ChannelBytes2Seconds(this.m_C, pos);
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

	public override void UpdateRealTimeFrequencyData()
	{
		if (MusicData.MusicInfo.State != 0 && MusicData.MusicInfo.State != SongState.Paused)
		{
			return;
		}
		Bass.BASS_ChannelGetData(this.m_C, MReactor.RealTimeFFTData, -2147483644);
		for (int i = 2; i < MReactor.A; i++)
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
				MReactor.RealTimeByteFFTData[i] = MReactor.A[(int)(num * 1000000f)];
			}
		}
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
		Bass.BASS_Stop();
		Bass.BASS_Free();
	}

	public override void SetPosition(double seconds)
	{
		Bass.BASS_ChannelSetPosition(this.m_C, seconds);
		D();
	}

	public override void ResetPosition()
	{
		Bass.BASS_ChannelSetPosition(this.m_C, 0L);
		D();
	}

	public override void SetVolume(float volume)
	{
		Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM, (int)(volume * 10000f));
	}

	public override float GetVolume()
	{
		return (float)Bass.BASS_GetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM) / 10000f;
	}

	public override void SetPlaybackSpeed(float speedFactor)
	{
		Bass.BASS_ChannelSetAttribute(this.m_C, BASSAttribute.BASS_ATTRIB_FREQ, (float)MusicData.MusicInfo.SampleRatePlayback * speedFactor);
	}
}
