namespace METrackEditor;

public class Obstacle
{
    public int SampleID { get; set; }

    public int EndSampleID { get; set; }

    //public int GridX { get; set; }

    public bool IsHeld { get; set; }

    public bool IsSolid { get; set; }

    //public bool IsMainTempo { get; set; }

    //public bool IsEnergyPeak { get; set; }

    //public bool IsAltEnergyPeak { get; set; }

    //public bool HasStartBeenAdjusted { get; set; }

    //public bool HasEndBeenAdjusted { get; set; }

    //public int GroupdID { get; set; }

    //public int GroupPosition { get; set; }
    
    /// <summary>
    /// ALWAYS 1-8 CAUSE THEY GET -1'd IDK WHY BUT IT BE HOW IT BE
    /// </summary>
    public int ForceTypeID { get; set; }
    
    public ObstacleInputType ForceType
    {
        get => (ObstacleInputType) (ForceTypeID - 1);
        set => ForceTypeID = (int) value + 1;
    }

    public Obstacle()
    { }
    
    public static string GetTextFromType(ObstacleInputType inputType) =>
        inputType switch
        {
            ObstacleInputType.Down => "D", 
            ObstacleInputType.Up => "U", 
            ObstacleInputType.Left => "L", 
            ObstacleInputType.Right => "R", 
            ObstacleInputType.ColorDown => "2", 
            ObstacleInputType.ColorUp => "8", 
            ObstacleInputType.ColorLeft => "4",
            ObstacleInputType.ColorRight => "6", 
            _ => "?", 
        };
    
    public static ObstacleInputType GetTypeFromTextID(char typeID)
    {
        return typeID switch
        {
            'D' => ObstacleInputType.Down, 
            'U' => ObstacleInputType.Up, 
            'L' => ObstacleInputType.Left, 
            'R' => ObstacleInputType.Right, 
            '8' => ObstacleInputType.ColorUp, 
            '2' => ObstacleInputType.ColorDown, 
            '4' => ObstacleInputType.ColorLeft, 
            '6' => ObstacleInputType.ColorRight, 
            _ => throw new Exception("Invalid Obstacle Type"), 
        };
    }

    public static int GetIDFromText(string s)
    {
        var typeID = s[0];
        var type = GetTypeFromTextID(typeID);
        return (int) type;
    }
}