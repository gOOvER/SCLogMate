namespace SCLogMate.Models;

/// <summary>Beobachteter Marktpreis je Ware aus den eigenen Trades: bester Verkaufs- und
/// günstigster Kaufpreis (pro SCU), inkl. Terminal und Marge.</summary>
public class MarketPrice
{
    public string Commodity { get; init; } = "";
    public string SellText { get; init; } = "";   // z.B. "1.102/SCU · Admin lt base g"
    public string BuyText { get; init; } = "";     // z.B. "782/SCU · Pyro RestStop"
    public string MarginText { get; init; } = "";  // z.B. "+320/SCU"
    public long MarginValue { get; init; }
}
