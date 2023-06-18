using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using METrackEditor;

namespace TrackEditorUI.Controls;

public class TransitionsVisualizer : Control
{
    private Track? _track;

    public static readonly DirectProperty<TransitionsVisualizer, Track?> TrackProperty = AvaloniaProperty.RegisterDirect<TransitionsVisualizer, Track?>(
        "Track", o => o.Track, (o, v) => o.Track = v);

    public Track? Track
    {
        get => _track;
        set => SetAndRaise(TrackProperty, ref _track, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Track == null)
            return;
        
        context.DrawText(
            new FormattedText("Penis", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 20, Brushes.Brown),
            new Point(Bounds.Width/2, Bounds.Height/2));
        
        base.Render(context);
    }
}