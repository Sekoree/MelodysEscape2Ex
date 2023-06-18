namespace METrackEditor;

public class Track
{
    public Version CacheFileVersion { get; set; }
    public int SampleCount { get; set; }
    public double SongDuration { get; set; }
    public double DisplayBPM { get; set; }
    public bool Is34BPM { get; set; }
    
    public List<Transition> Transitions { get; set; } = new();
    
    public List<Obstacle> Obstacles { get; set; } = new();

    public static Track LoadFromString(string[] lines)
    {
        //Line 0: Version
        //Line 1: SampleCount;SongDuration;DisplayBPM;Is34BPM
        //Line 2: Transitions
        //Line 3: Obstacles
        var track = new Track();
        track.CacheFileVersion = Version.Parse(lines[0]);
        var line1 = lines[1].Split(';');
        track.SampleCount = int.Parse(line1[0]);
        track.SongDuration = Math.Round(double.Parse(line1[1]), 2);
        track.DisplayBPM = double.Parse(line1[2]);
        track.Is34BPM = int.Parse(line1[3]) == 1;
        var line2 = lines[2].Split(';');
        var currentIntensityLevel = IntensityLevel.Low;
        foreach (var transitionString in line2)
        {
            if (transitionString.Length < 3)
                continue;
            
            var transitionStringSplit = transitionString.Split('-');
            if (transitionStringSplit.Length < 2 || transitionStringSplit[0].Length < 3 || transitionStringSplit[1].Length == 0)
                throw new Exception("Invalid transition string: " + transitionString);
            
            var flag = false;
            var num3 = 2;
            if (transitionStringSplit[0][0] == '|')
            {
                flag = true;
                num3 = 3;
            }
            var num4 = int.Parse(transitionStringSplit[0].Substring(num3));
            var num5 = int.Parse(transitionStringSplit[1]);
            if (!flag)
                num5 = num4 + num5 - 1;
            
            var c = transitionStringSplit[0][num3 - 2];
            var endIntensityLevel = Transition.GetIntensityLevelFromTextID(c);
            var transition = new Transition();
            transition.Start = num4;
            transition.End = num5;
            if (transitionStringSplit.Length >= 3 && transitionStringSplit[2][0] == 'A')
                transition.IsAngelJump = true;
            else if (endIntensityLevel == currentIntensityLevel)
                continue;
            
            transition.StartIntensityLevel = currentIntensityLevel;
            transition.EndIntensityLevel = endIntensityLevel;
            track.Transitions.Add(transition);
            currentIntensityLevel = endIntensityLevel;
            //Extra transition here??
            if (!transition.IsAngelJump || endIntensityLevel != IntensityLevel.Low) 
                continue;
            
            var extraTransition = new Transition();
            extraTransition.StartIntensityLevel = endIntensityLevel;
            extraTransition.EndIntensityLevel = endIntensityLevel;
            extraTransition.Start = transition.End;
            extraTransition.End = extraTransition.Start + 46; //AJ_LOW_RECOVERY_LEN
            if (extraTransition.End > track.SampleCount - 2)
                extraTransition.End = track.SampleCount - 2;
            extraTransition.IsAngelJump = false;
            extraTransition.IsAngelJumpEnding = true;
            if (extraTransition.End - extraTransition.Start + 1 >= 2)
                track.Transitions.Add(extraTransition);
        }
        var line3 = lines[3].Split(';');
        foreach (var obstacleString in line3)
        {
            var obstacleStringSplit = obstacleString.Split(':');
            if (obstacleStringSplit.Length < 2)
                continue;
            
            var obstacleStartSample = int.Parse(obstacleStringSplit[0]);
            var c = obstacleStringSplit[1][0];
            if (c == 'A') //Angel Jump
                continue;
            
            var obstacleLength = 1;
            var array3 = obstacleStringSplit[1].Split('-');
            if (array3.Length > 1)
                obstacleLength = int.Parse(array3[1]);
            
            var isNotWall = false;
            int obstacleControlType = 0;
            switch (c)
            {
                case 'S':
                    isNotWall = false;
                    break;
                case 'Z':
                    isNotWall = true;
                    break;
                default:
                    var type = Obstacle.GetTypeFromTextID(c);
                    if ((uint)(type - 4) <= 3u)
                        isNotWall = true;
                    obstacleControlType = (int)type + 1;
                    break;
            }
            
            var obstacle = new Obstacle
            {
                SampleID = obstacleStartSample,
                EndSampleID = obstacleStartSample + obstacleLength - 1,
                IsHeld = obstacleLength > 1,
                IsSolid = !isNotWall,
                ForceTypeID = obstacleControlType
            };
            track.Obstacles.Add(obstacle);
        }
        return track;
    }
    
    public override string ToString()
    {
        var text = this.CacheFileVersion + Environment.NewLine; //idk why newline tbh
        text = text + this.SampleCount + ";" + this.SongDuration + ";" + this.DisplayBPM + ";" + (Is34BPM ? "1" : "0") + Environment.NewLine;
        var currentIntensityLevel = IntensityLevel.Low;
        foreach (var transition in Transitions.Where(transition =>
                     transition.EndIntensityLevel != currentIntensityLevel || transition.IsAngelJump))
        {
            text += transition + ";";
            currentIntensityLevel = transition.EndIntensityLevel;
        }
        text += Environment.NewLine;
        foreach (var obstacle in Obstacles)
        {
            var str0 = text;
            var str1 = obstacle.SampleID.ToString();
            var str2 = ":";
            var str3 = obstacle.ForceTypeID > 0
                ? Obstacle.GetTextFromType((ObstacleInputType) (obstacle.ForceTypeID - 1))
                : !obstacle.IsSolid
                    ? "Z"
                    : "S";
            text = str0 + str1 + str2 + str3;
            if (obstacle.IsHeld)
            {
                var num = obstacle.EndSampleID - obstacle.SampleID + 1;
                text += "-" + num;
            }
            text += ";";
        }
        return text;
    }
}