using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SCLogMate.Models;

namespace SCLogMate.Converters;

/// <summary>Färbt Beträge: grün = rein, rot = raus, gedimmt = neutral.</summary>
public class AmountToBrushConverter : IValueConverter
{
    public static readonly AmountToBrushConverter Instance = new();

    static readonly IBrush In = new SolidColorBrush(Color.Parse("#4ADE80"));
    static readonly IBrush Out = new SolidColorBrush(Color.Parse("#F87171"));
    static readonly IBrush Neutral = new SolidColorBrush(Color.Parse("#8B949E"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        long amt = value is long l ? l : 0;
        return amt > 0 ? In : amt < 0 ? Out : Neutral;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Hintergrund eines Filter-Chips: aktiv = Akzent, sonst gedimmt.</summary>
public class FilterActiveConverter : IValueConverter
{
    public static readonly FilterActiveConverter Instance = new();

    static readonly IBrush Active = new SolidColorBrush(Color.Parse("#0C3E69"));
    static readonly IBrush Idle = new SolidColorBrush(Color.Parse("#0B1522"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.Ordinal) ? Active : Idle;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Rahmenfarbe eines Filter-Chips: aktiv = leuchtend, sonst subtil.</summary>
public class FilterActiveBorderConverter : IValueConverter
{
    public static readonly FilterActiveBorderConverter Instance = new();

    static readonly IBrush Active = new SolidColorBrush(Color.Parse("#38BDF8"));
    static readonly IBrush Idle = new SolidColorBrush(Color.Parse("#16283C"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.Ordinal) ? Active : Idle;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Vordergrund/Textfarbe eines Filter-Chips: aktiv = hell/leuchtend, sonst dezent gedimmt.</summary>
public class FilterActiveForegroundConverter : IValueConverter
{
    public static readonly FilterActiveForegroundConverter Instance = new();

    static readonly IBrush Active = new SolidColorBrush(Color.Parse("#38BDF8"));
    static readonly IBrush Idle = new SolidColorBrush(Color.Parse("#94A3B8"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.Ordinal) ? Active : Idle;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Segment-Hintergrund (aktiv = MFD-Kachel, sonst transparent).</summary>
public class SegmentActiveBgConverter : IValueConverter
{
    public static readonly SegmentActiveBgConverter Instance = new();

    static readonly IBrush Active = new SolidColorBrush(Color.Parse("#0E3B66"));
    static readonly IBrush Idle = new SolidColorBrush(Colors.Transparent);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.Ordinal) ? Active : Idle;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Segment-Rahmen (aktiv = Cyan/Akzent, sonst transparent).</summary>
public class SegmentActiveBorderConverter : IValueConverter
{
    public static readonly SegmentActiveBorderConverter Instance = new();

    static readonly IBrush Active = new SolidColorBrush(Color.Parse("#38BDF8"));
    static readonly IBrush Idle = new SolidColorBrush(Colors.Transparent);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.Ordinal) ? Active : Idle;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Akzentfarbe je Event-Typ (für den Icon-Punkt) – mit vorberechneten statischen Brushes.</summary>
public class KindToBrushConverter : IValueConverter
{
    public static readonly KindToBrushConverter Instance = new();

    static readonly IBrush Green = new SolidColorBrush(Color.Parse("#4ADE80"));
    static readonly IBrush Red = new SolidColorBrush(Color.Parse("#F87171"));
    static readonly IBrush DarkRed = new SolidColorBrush(Color.Parse("#EF4444"));
    static readonly IBrush Amber = new SolidColorBrush(Color.Parse("#FBBF24"));
    static readonly IBrush Cyan = new SolidColorBrush(Color.Parse("#22D3EE"));
    static readonly IBrush Sky = new SolidColorBrush(Color.Parse("#38BDF8"));
    static readonly IBrush Purple = new SolidColorBrush(Color.Parse("#A78BFA"));
    static readonly IBrush BrightPurple = new SolidColorBrush(Color.Parse("#C084FC"));
    static readonly IBrush Orange = new SolidColorBrush(Color.Parse("#FB923C"));
    static readonly IBrush Pink = new SolidColorBrush(Color.Parse("#F472B6"));
    static readonly IBrush Rose = new SolidColorBrush(Color.Parse("#FB7185"));
    static readonly IBrush Slate = new SolidColorBrush(Color.Parse("#94A3B8"));
    static readonly IBrush Neutral = new SolidColorBrush(Color.Parse("#8B949E"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var kind = value is EventKind k ? k : EventKind.Info;
        return kind switch
        {
            EventKind.TransferIn or EventKind.Sale or EventKind.Friend => Green,
            EventKind.TransferOut or EventKind.Fine or EventKind.MedBed or EventKind.ShipLoss => Red,
            EventKind.Death or EventKind.Kill or EventKind.Crime => DarkRed,
            EventKind.MissionReward or EventKind.Mission or EventKind.MissionDone or EventKind.MissionTaken or EventKind.Offer => Amber,
            EventKind.Trade or EventKind.Quantum or EventKind.Refinery => Cyan,
            EventKind.Location or EventKind.Hangar => Sky,
            EventKind.Inventory or EventKind.Party => Purple,
            EventKind.Blueprint or EventKind.Loot => BrightPurple,
            EventKind.Vehicle or EventKind.Impound or EventKind.Purchase => Orange,
            EventKind.Jurisdiction => Pink,
            EventKind.Gear or EventKind.Injury => Rose,
            EventKind.Loadout or EventKind.Entitlement => Slate,
            _ => Neutral
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Wandelt boolean in Akzentfarbe (aktiv = Gold/Gelb, inaktiv = gedimmt).</summary>
public class BoolToActiveBrushConverter : IValueConverter
{
    public static readonly BoolToActiveBrushConverter Instance = new();

    static readonly IBrush Active = new SolidColorBrush(Color.Parse("#FBBF24"));
    static readonly IBrush Inactive = new SolidColorBrush(Color.Parse("#8B949E"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Active : Inactive;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Wandelt boolean in Statusfarbe: true = Grün (#4ADE80), false = Rot (#F87171).</summary>
public class BoolToGreenRedBrushConverter : IValueConverter
{
    public static readonly BoolToGreenRedBrushConverter Instance = new();

    static readonly IBrush Green = new SolidColorBrush(Color.Parse("#4ADE80"));
    static readonly IBrush Red = new SolidColorBrush(Color.Parse("#F87171"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Green : Red;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Wandelt boolean in Deckkraft: true = 1.0 (voll sichtbar), false = 0.45 (ausgegraut).</summary>
public class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 1.0 : 0.45;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Wandelt boolean in dezente Status-Hintergrundfarbe: true = Grün (#1F4ADE80), false = Rot (#1FF87171).</summary>
public class BoolToGreenRedBgConverter : IValueConverter
{
    public static readonly BoolToGreenRedBgConverter Instance = new();

    static readonly IBrush GreenBg = new SolidColorBrush(Color.Parse("#1F4ADE80"));
    static readonly IBrush RedBg = new SolidColorBrush(Color.Parse("#1FF87171"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? GreenBg : RedBg;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Wandelt boolean in Akzent-Hintergrundfarbe: true = #1D6FA5 (Sci-Fi Cyan/Blau aktiv), false = #0A1422 (Idle).</summary>
public class BoolToAccentBrushConverter : IValueConverter
{
    public static readonly BoolToAccentBrushConverter Instance = new();

    static readonly IBrush Active = new SolidColorBrush(Color.Parse("#1D6FA5"));
    static readonly IBrush Idle = new SolidColorBrush(Color.Parse("#0A1422"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Active : Idle;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Wandelt boolean in Rand-Farbe: true = #38BDF8 (Neon-Rand), false = #1A3047 (Dezent).</summary>
public class BoolToAccentBorderConverter : IValueConverter
{
    public static readonly BoolToAccentBorderConverter Instance = new();

    static readonly IBrush Active = new SolidColorBrush(Color.Parse("#38BDF8"));
    static readonly IBrush Idle = new SolidColorBrush(Color.Parse("#1A3047"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Active : Idle;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}



