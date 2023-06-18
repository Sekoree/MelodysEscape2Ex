using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace AS22ME2.Controls;

public class Audiosurf2Parameters : TemplatedControl
{
    public static readonly StyledProperty<decimal> MinSpeedProperty =
        AvaloniaProperty.Register<Audiosurf2Parameters, decimal>(
            "MinSpeed", defaultBindingMode: BindingMode.TwoWay);

    public decimal MinSpeed
    {
        get => GetValue(MinSpeedProperty);
        set => SetValue(MinSpeedProperty, value);
    }

    public static readonly StyledProperty<decimal> MaxSpeedProperty =
        AvaloniaProperty.Register<Audiosurf2Parameters, decimal>(
            "MaxSpeed", defaultBindingMode: BindingMode.TwoWay);

    public decimal MaxSpeed
    {
        get => GetValue(MaxSpeedProperty);
        set => SetValue(MaxSpeedProperty, value);
    }

    public static readonly StyledProperty<decimal> MinBestJumpTimeProperty =
        AvaloniaProperty.Register<Audiosurf2Parameters, decimal>(
            "MinBestJumpTime", defaultBindingMode: BindingMode.TwoWay);

    public decimal MinBestJumpTime
    {
        get => GetValue(MinBestJumpTimeProperty);
        set => SetValue(MinBestJumpTimeProperty, value);
    }

    public static readonly StyledProperty<bool> DownhillOnlyProperty =
        AvaloniaProperty.Register<Audiosurf2Parameters, bool>(
            "DownhillOnly", defaultBindingMode: BindingMode.TwoWay);

    public bool DownhillOnly
    {
        get => GetValue(DownhillOnlyProperty);
        set => SetValue(DownhillOnlyProperty, value);
    }

    public static readonly StyledProperty<decimal> SteepUphillScalerProperty =
        AvaloniaProperty.Register<Audiosurf2Parameters, decimal>(
            "SteepUphillScaler", defaultBindingMode: BindingMode.TwoWay);

    public decimal SteepUphillScaler
    {
        get => GetValue(SteepUphillScalerProperty);
        set => SetValue(SteepUphillScalerProperty, value);
    }

    public static readonly StyledProperty<decimal> SteepDownhillScalerProperty =
        AvaloniaProperty.Register<Audiosurf2Parameters, decimal>(
            "SteepDownhillScaler", defaultBindingMode: BindingMode.TwoWay);

    public decimal SteepDownhillScaler
    {
        get => GetValue(SteepDownhillScalerProperty);
        set => SetValue(SteepDownhillScalerProperty, value);
    }

    public static readonly StyledProperty<bool> UseAveragedFlatSlopesProperty =
        AvaloniaProperty.Register<Audiosurf2Parameters, bool>(
            "UseAveragedFlatSlopes", defaultBindingMode: BindingMode.TwoWay);

    public bool UseAveragedFlatSlopes
    {
        get => GetValue(UseAveragedFlatSlopesProperty);
        set => SetValue(UseAveragedFlatSlopesProperty, value);
    }

    public static readonly StyledProperty<decimal> TiltSmootherUphillProperty =
        AvaloniaProperty.Register<Audiosurf2Parameters, decimal>(
            "TiltSmootherUphill", defaultBindingMode: BindingMode.TwoWay);

    public decimal TiltSmootherUphill
    {
        get => GetValue(TiltSmootherUphillProperty);
        set => SetValue(TiltSmootherUphillProperty, value);
    }

    public static readonly StyledProperty<decimal> TiltSmootherDownhillProperty =
        AvaloniaProperty.Register<Audiosurf2Parameters, decimal>(
            "TiltSmootherDownhill", defaultBindingMode: BindingMode.TwoWay);

    public decimal TiltSmootherDownhill
    {
        get => GetValue(TiltSmootherDownhillProperty);
        set => SetValue(TiltSmootherDownhillProperty, value);
    }

    public static readonly StyledProperty<decimal> GravityProperty =
        AvaloniaProperty.Register<Audiosurf2Parameters, decimal>(
            "Gravity", defaultBindingMode: BindingMode.TwoWay);

    public decimal Gravity
    {
        get => GetValue(GravityProperty);
        set => SetValue(GravityProperty, value);
    }
}