namespace METrackEditor;

public class Transition
{
    //Checked: EndIntensityLevel, IsAngelJump
    public int Start { get; set; }
    public int End { get; set; }

    public IntensityLevel StartIntensityLevel { get; set; }

    public IntensityLevel EndIntensityLevel { get; set; }

    public bool IsAngelJump { get; set; }

    public bool IsAngelJumpEnding { get; set; }

    public int Length => End - Start + 1;

    public override string ToString()
    {
        var text = string.Format("{0}:{1}-{2}", EndIntensityLevel.ToString().Substring(0, 1), Start, Length);
        if (this.IsAngelJump) 
            text += "-A";
        return text;
    }

    public static IntensityLevel GetIntensityLevelFromTextID(char c) =>
        c switch
        {
            'N' => IntensityLevel.Normal, 
            'H' => IntensityLevel.High, 
            'E' => IntensityLevel.Extreme, 
            _ => IntensityLevel.Low, 
        };
}