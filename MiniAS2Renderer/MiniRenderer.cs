using System.Collections.Specialized;
using System.Runtime.InteropServices;
using ManagedBass;

namespace MiniAS2Renderer;

public class MiniRenderer
{
    public static (float[] sums, double seconds) GetAllFloatSamples(string path)
    {
        var could = Bass.Init(-1);
        var p = Bass.PluginLoad("bass_aac.dll");
        p = Bass.PluginLoad("bassflac.dll");
        p = Bass.PluginLoad("basshls.dll");
        p = Bass.PluginLoad("bassopus.dll");
        p = Bass.PluginLoad("basswebm.dll");
        p = Bass.PluginLoad("basswma.dll");
        
        var stream = Bass.CreateStream(path, 0, 0, BassFlags.Decode | BassFlags.Float | BassFlags.Prescan);
        if (stream == 0)
        {
            var error = Bass.LastError;
            throw new Exception("Failed to load audio file");
        }

        var seconds = Bass.ChannelBytes2Seconds(stream, Bass.ChannelGetLength(stream));
        
        var data = new List<float>();
        var buffer = new float[512];
        while (Bass.ChannelGetData(stream, buffer, (int)(DataFlags.Float | DataFlags.FFT1024)) > 0)
        {
            var sum = buffer.Sum();
            var valToAdd = Math.Max(0f, (float)Math.Sqrt(sum));
            data.Add(valToAdd);
        }
        
        Bass.StreamFree(stream);
        Bass.Free();
        return (data.ToArray(), seconds);
    }

    public static Node[] GetAllTrackNodes(
        float[] sums, 
        double seconds, 
        float minSpeed = 0.01f, 
        float maxSpeed = 6.1f, 
        float minBestJumpTime = 2.15f, 
        bool downhillOnly = false, 
        float uphillScaler = 5.5f, 
        float downhillScaler = 5.5f, 
        bool useAveragedFlatSlopes = false, 
        float uphillSmoother = 0.03f, 
        float downhillSmoother = 0.03f, 
        float gravity = 0.45f)
    {
        var handle = GCHandle.Alloc(sums, GCHandleType.Pinned);
        var result = cppBuildAll(handle.AddrOfPinnedObject(), 
            sums.Length, 
            seconds, 
            minSpeed, 
            maxSpeed, 
            minBestJumpTime, 
            downhillOnly, 
            uphillScaler, 
            downhillScaler, 
            useAveragedFlatSlopes, 
            uphillSmoother, 
            downhillSmoother, 
            gravity);
        
        if (result == 0)
            throw new Exception("Failed to build track");
        
        var nodes = new Node[result];
        var nodesHandle = GCHandle.Alloc(nodes, GCHandleType.Pinned);
        var success = cppGetBuiltData(nodesHandle.AddrOfPinnedObject(), result);
        if (!success)
            throw new Exception("Failed to get built data");
        
        return nodes;
    }
    
    [DllImport("compost.dll", CharSet = CharSet.Unicode)]
    private static extern int cppBuildAll(IntPtr sums, int sumsCount, double durationSeconds, float playerminspeed, float playerspeed, float minimumBestJumpTime, bool bDownhillOnly, float exsteepUphillScaler, float exsteepDownhillScaler, bool useAveragedFlatSlopes, float fTiltSmootherUphill, float fTiltSmootherDownhill, float gravity);

    [DllImport("compost.dll", CharSet = CharSet.Unicode)]
    private static extern bool cppGetBuiltData(IntPtr ringsData, int count);

}

public struct Vector3
{
    public float X;

    public float Y;

    public float Z;
}

public struct Node
{
    public Vector3 Pos;

    public Vector3 RotVector;

    public float Intensity;

    public float Seconds;

    public float JumpSeconds;

    public float MaxAir;

    public bool HasBlock;

    public float TrafficStrength;

    public int TrafficChainStart;

    public int TrafficChainEnd;
}