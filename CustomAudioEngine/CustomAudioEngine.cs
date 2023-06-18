using System;
using System.Diagnostics;
using System.Threading;
using ManagedBass;
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
            //this.m_A = A; //DSP and Stream Proc
            //this.m_A = A;
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

        private static int GetBassFlags(int fftQuality)
        {
            return fftQuality switch
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
    }
}