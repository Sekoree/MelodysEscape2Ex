// See https://aka.ms/new-console-template for more information

using METrackEditor;
using MiniAS2Renderer;

Console.WriteLine("Hello, World!");

var fileData = File.ReadAllLines("01 Heartbeat.m4a_2.txt");
var track = Track.LoadFromString(fileData);

var sumsAndSeconds = MiniRenderer.GetAllFloatSamples(Path.Combine(Directory.GetCurrentDirectory(), "01 Heartbeat.m4a"));
var rawNodes = MiniRenderer.GetAllTrackNodes(sumsAndSeconds.sums, sumsAndSeconds.seconds);

//skip nodes before 0 seconds
var nodes = rawNodes.Where(x => x.Seconds >= 0).OrderBy(x => x.Seconds).ToArray();

//skip nodes after track duration
nodes = nodes.Where(x => x.Seconds <= track.SongDuration).OrderBy(x => x.Seconds).ToArray();

if (nodes.Length < sumsAndSeconds.sums.Length)
    throw new Exception(string.Format("Not enough nodes? Less nodes than samples {0} Nodes, {1} Samples", nodes.Length, sumsAndSeconds.sums.Length));

var indexedNodes = new Dictionary<int, Node>(); //index, node
var sumsPerSecond = sumsAndSeconds.sums.Length / sumsAndSeconds.seconds;
var duplicates = 0;

foreach (var node in nodes)
{
    var index = (int)(node.Seconds * sumsPerSecond);
    if (indexedNodes.ContainsKey(index))
    {
        duplicates++;
        var existingNode = indexedNodes[index];
        var averagedNode = new Node()
        {
            HasBlock = node.HasBlock || existingNode.HasBlock,
            Seconds = (existingNode.Seconds + node.Seconds) / 2,
            JumpSeconds = (existingNode.JumpSeconds + node.JumpSeconds) / 2,
            Intensity = (existingNode.Intensity + node.Intensity) / 2,
            Pos = new Vector3()
            {
                X = (existingNode.Pos.X + node.Pos.X) / 2,
                Y = (existingNode.Pos.Y + node.Pos.Y) / 2,
                Z = (existingNode.Pos.Z + node.Pos.Z) / 2
            },
            RotVector = new Vector3()
            {
                X = (existingNode.RotVector.X + node.RotVector.X) / 2,
                Y = (existingNode.RotVector.Y + node.RotVector.Y) / 2,
                Z = (existingNode.RotVector.Z + node.RotVector.Z) / 2
            },
            MaxAir = (existingNode.MaxAir + node.MaxAir) / 2,
            TrafficStrength = (existingNode.TrafficStrength + node.TrafficStrength) / 2,
            TrafficChainStart = (existingNode.TrafficChainStart > nodes.Length || existingNode.TrafficChainStart < 0) ? 
                node.TrafficChainStart : existingNode.TrafficChainStart,
            TrafficChainEnd = (existingNode.TrafficChainStart > nodes.Length || existingNode.TrafficChainStart < 0) ? 
                node.TrafficChainEnd : existingNode.TrafficChainEnd,
        };
        indexedNodes[index] = averagedNode;
    }
    else
    {
        indexedNodes[index] = node;
    }
}

track.Obstacles.Clear();

var indexedNodesWithBlocks = indexedNodes.Where(x => x.Value.HasBlock)
    .OrderBy(x => x.Key)
    .ToArray();

var rng = new Random(indexedNodesWithBlocks.Length);
foreach (var indexedNode in indexedNodesWithBlocks)
{
    var lastObstacle = track.Obstacles.LastOrDefault();
    if (lastObstacle != null && (indexedNode.Key - lastObstacle.EndSampleID) < 9)
        continue;
    
    var indexToUse = indexedNode.Key;
    //if (indexedNode.Value.TrafficChainStart != indexedNode.Value.TrafficChainEnd)
    //{
    //    var endIndexSeconds = rawNodes[indexedNode.Value.TrafficChainEnd].Seconds;
    //    var endIndex = (int)(endIndexSeconds * sumsPerSecond);
    //    indexToUse = (indexToUse + endIndex) / 2;
    //}
    //AS2 Chains are a mess
    
    var obst = rng.Next(4, 8);
    var wallChance = rng.Next(0, 100);
    if (wallChance > 70)
        obst -= 4;
    
    var obstacle = new Obstacle()
    {
        SampleID = indexToUse,
        EndSampleID = indexToUse + 1, 
        ForceType = (ObstacleInputType)(obst), 
        IsHeld = false, 
        IsSolid = false
    };
    track.Obstacles.Add(obstacle);
}


Console.WriteLine("Got nodes: " + nodes.Length);
Console.WriteLine("Song duration: " + sumsAndSeconds.seconds);
Console.WriteLine("Track sample count: " + track.SampleCount);
Console.WriteLine("Track transition count: " + track.Transitions.Count);
Console.WriteLine("Track obstacle count: " + track.Obstacles.Count);
Console.WriteLine("Indexed nodes: " + indexedNodes.Count);
Console.WriteLine("Duplicates: " + duplicates);

//save track
var trackString = track.ToString();
File.WriteAllText("01 Heartbeat.m4a_2.txt", trackString);

//Console.ReadLine();