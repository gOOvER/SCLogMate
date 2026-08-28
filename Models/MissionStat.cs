namespace SCLogReader.Models;

/// <summary>Ein Auftraggeber/Fraktion mit Anzahl gespielter Missionen (Ruf-Proxy) und Balkenbreite.</summary>
public class MissionStat
{
    public string Faction { get; init; } = "";
    public int Count { get; init; }
    public string CountText => Count.ToString("N0");
    public double BarWidth { get; init; }
    public string SubText { get; init; } = "";   // z.B. "Fracht/Bergung · meist Schwer"
}
