namespace SCLogReader.Models;

/// <summary>
/// Definiert einen Bildschirmbereich (in physischen Pixeln) für OCR-Scans.
/// </summary>
public record ScanRegion
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    public bool IsValid => Width > 5 && Height > 5;

    public override string ToString() => $"{Width}x{Height} @ ({X},{Y})";
}
