using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SCLogMate.Models;

namespace SCLogMate.Views;

/// <summary>
/// Interaktives Sci-Fi Vektor-Diagramm im Stil von QuantumWake.
/// Unterstützt kumulierte Zeitreihen (Einnahmen vs. Ausgaben mit Gradient-Flächen),
/// Netto-Gewinnverlauf sowie Cashflow-Balken mit Maus-Hover und mobiGlas-Tooltips.
/// </summary>
public sealed class FinanceTimelineChart : Control
{
    public static readonly StyledProperty<IEnumerable<FinanceTimelinePoint>?> ItemsSourceProperty =
        AvaloniaProperty.Register<FinanceTimelineChart, IEnumerable<FinanceTimelinePoint>?>(nameof(ItemsSource));

    /// <summary>
    /// 0 = Kumulierter Verlauf (In vs. Out)
    /// 1 = Netto-Gewinntrend (Cumulative Net)
    /// 2 = Cashflow-Balken (Einzelne Buchungen)
    /// </summary>
    public static readonly StyledProperty<int> ChartModeProperty =
        AvaloniaProperty.Register<FinanceTimelineChart, int>(nameof(ChartMode), 0);

    public static readonly StyledProperty<string> EmptyTextProperty =
        AvaloniaProperty.Register<FinanceTimelineChart, string>(nameof(EmptyText), "KEINE FINANZDATEN IN DIESEM ZEITRAUM");

    public IEnumerable<FinanceTimelinePoint>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int ChartMode
    {
        get => GetValue(ChartModeProperty);
        set => SetValue(ChartModeProperty, value);
    }

    public string EmptyText
    {
        get => GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    static FinanceTimelineChart()
    {
        AffectsRender<FinanceTimelineChart>(ItemsSourceProperty, ChartModeProperty, EmptyTextProperty);
    }

    private Point? _hoverPos;
    private FinanceTimelinePoint? _hoverItem;
    private Point _hoverPointCanvas;

    public FinanceTimelineChart()
    {
        ClipToBounds = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _hoverPos = e.GetPosition(this);
        UpdateHoverItem();
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hoverPos = null;
        _hoverItem = null;
        InvalidateVisual();
    }

    private void UpdateHoverItem()
    {
        if (_hoverPos == null || ItemsSource == null)
        {
            _hoverItem = null;
            return;
        }

        var pts = ItemsSource.ToList();
        if (pts.Count == 0)
        {
            _hoverItem = null;
            return;
        }

        double w = Bounds.Width;
        double padLeft = 70;
        double padRight = 20;
        double plotWidth = Math.Max(10, w - padLeft - padRight);

        double curX = _hoverPos.Value.X;
        if (curX < padLeft || curX > w - padRight)
        {
            _hoverItem = null;
            return;
        }

        if (pts.Count == 1)
        {
            _hoverItem = pts[0];
            return;
        }

        long tMin = pts.Min(p => p.Time.Ticks);
        long tMax = pts.Max(p => p.Time.Ticks);
        long tSpan = Math.Max(1, tMax - tMin);

        // Nächstgelegenen Datenpunkt ermitteln
        FinanceTimelinePoint? best = null;
        double bestDist = double.MaxValue;

        foreach (var p in pts)
        {
            double px = padLeft + ((double)(p.Time.Ticks - tMin) / tSpan) * plotWidth;
            double dist = Math.Abs(px - curX);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = p;
                _hoverPointCanvas = new Point(px, _hoverPos.Value.Y);
            }
        }

        _hoverItem = best;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 60 || h < 60) return;

        // 1. Hintergrund (Sci-Fi Panel)
        var bgBrush = new SolidColorBrush(Color.Parse("#060C16"));
        var borderPen = new Pen(new SolidColorBrush(Color.Parse("#1A3047")), 1);
        context.DrawRectangle(bgBrush, borderPen, new Rect(0, 0, w, h), 6, 6);

        var pts = ItemsSource?.OrderBy(p => p.Time).ToList();
        if (pts == null || pts.Count == 0)
        {
            DrawEmptyState(context, w, h);
            return;
        }

        double padLeft = 70;
        double padRight = 20;
        double padTop = 22;
        double padBottom = 28;

        double plotW = Math.Max(10, w - padLeft - padRight);
        double plotH = Math.Max(10, h - padTop - padBottom);

        long tMin = pts.Min(p => p.Time.Ticks);
        long tMax = pts.Max(p => p.Time.Ticks);
        long tSpan = Math.Max(1, tMax - tMin);

        Func<long, double> getX = ticks =>
            padLeft + (tSpan <= 0 ? plotW / 2 : ((double)(ticks - tMin) / tSpan) * plotW);

        // Je nach Modus zeichnen
        if (ChartMode == 0)
        {
            RenderCumulativeInVsOut(context, pts, padLeft, padTop, plotW, plotH, getX);
        }
        else if (ChartMode == 1)
        {
            RenderNetProfitTrend(context, pts, padLeft, padTop, plotW, plotH, getX);
        }
        else
        {
            RenderCashflowBars(context, pts, padLeft, padTop, plotW, plotH, getX);
        }

        // Datum-Ticks auf X-Achse
        DrawTimeAxis(context, pts, tMin, tMax, padLeft, padTop, plotW, plotH, getX);

        // 4. Interaktives Crosshair & Hover Tooltip
        if (_hoverItem != null && _hoverPos != null)
        {
            DrawHoverOverlay(context, _hoverItem, _hoverPointCanvas.X, padTop, plotH, w, h);
        }
    }

    private void RenderCumulativeInVsOut(DrawingContext context, List<FinanceTimelinePoint> pts,
        double padLeft, double padTop, double plotW, double plotH, Func<long, double> getX)
    {
        long maxVal = Math.Max(1000, Math.Max(pts.Max(p => p.CumulativeIncome), pts.Max(p => p.CumulativeSpend)));
        long minVal = 0;
        double valRange = maxVal - minVal;

        Func<long, double> getY = v => padTop + plotH - ((v - minVal) / valRange) * plotH;

        // Horizontale Gitterlinien & Achsen-Labels
        DrawHorizontalGrid(context, padLeft, padTop, plotW, plotH, minVal, maxVal, getY);

        // Kurve 1: Einnahmen (Neon-Cyan/Grün #38BDF8)
        DrawSmoothCurve(context, pts, getX, p => getY(p.CumulativeIncome), padTop + plotH,
            Color.Parse("#38BDF8"), Color.FromArgb(40, 56, 189, 248));

        // Kurve 2: Ausgaben (Neon-Orange/Koralle #F87171)
        DrawSmoothCurve(context, pts, getX, p => getY(p.CumulativeSpend), padTop + plotH,
            Color.Parse("#F87171"), Color.FromArgb(32, 248, 113, 113));
    }

    private void RenderNetProfitTrend(DrawingContext context, List<FinanceTimelinePoint> pts,
        double padLeft, double padTop, double plotW, double plotH, Func<long, double> getX)
    {
        long maxVal = pts.Max(p => p.CumulativeNet);
        long minVal = pts.Min(p => p.CumulativeNet);

        // Nulllinie immer mit einbeziehen
        if (maxVal < 0) maxVal = 0;
        if (minVal > 0) minVal = 0;
        if (maxVal == minVal) { maxVal = 1000; minVal = -1000; }

        double valRange = maxVal - minVal;
        Func<long, double> getY = v => padTop + plotH - ((v - minVal) / valRange) * plotH;

        // Horizontale Gitterlinien & Achsen-Labels
        DrawHorizontalGrid(context, padLeft, padTop, plotW, plotH, minVal, maxVal, getY);

        // Null-Linie hervorheben
        double zeroY = getY(0);
        var zeroPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 110, 140, 170)), 1, DashStyle.Dash);
        context.DrawLine(zeroPen, new Point(padLeft, zeroY), new Point(padLeft + plotW, zeroY));

        // Netto-Kurve (Neon Sky-Blue #60A5FA)
        DrawSmoothCurve(context, pts, getX, p => getY(p.CumulativeNet), zeroY,
            Color.Parse("#60A5FA"), Color.FromArgb(45, 96, 165, 250));
    }

    private void RenderCashflowBars(DrawingContext context, List<FinanceTimelinePoint> pts,
        double padLeft, double padTop, double plotW, double plotH, Func<long, double> getX)
    {
        long maxVal = pts.Max(p => Math.Max(0, p.Amount));
        long minVal = pts.Min(p => Math.Min(0, p.Amount));
        if (maxVal == 0 && minVal == 0) { maxVal = 1000; minVal = -1000; }
        if (maxVal == minVal) { maxVal += 1000; minVal -= 1000; }

        double valRange = maxVal - minVal;
        Func<long, double> getY = v => padTop + plotH - ((v - minVal) / valRange) * plotH;

        DrawHorizontalGrid(context, padLeft, padTop, plotW, plotH, minVal, maxVal, getY);

        double zeroY = getY(0);
        var zeroPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 110, 140, 170)), 1);
        context.DrawLine(zeroPen, new Point(padLeft, zeroY), new Point(padLeft + plotW, zeroY));

        double barWidth = Math.Max(3, Math.Min(18, plotW / Math.Max(1, pts.Count) * 0.7));

        var greenBrush = new SolidColorBrush(Color.Parse("#4ADE80"));
        var redBrush = new SolidColorBrush(Color.Parse("#F87171"));

        foreach (var p in pts)
        {
            double bx = getX(p.Time.Ticks) - barWidth / 2;
            double by = getY(p.Amount);
            double barH = Math.Abs(by - zeroY);
            double topY = Math.Min(by, zeroY);

            if (barH < 1.5) barH = 1.5;

            var brush = p.Amount >= 0 ? greenBrush : redBrush;
            context.FillRectangle(brush, new Rect(bx, topY, barWidth, barH));
        }
    }

    private void DrawSmoothCurve(DrawingContext context, List<FinanceTimelinePoint> pts,
        Func<long, double> getX, Func<FinanceTimelinePoint, double> getY, double baselineY,
        Color lineColor, Color areaFillColor)
    {
        if (pts.Count == 0) return;

        var points = pts.Select(p => new Point(getX(p.Time.Ticks), getY(p))).ToList();

        // 1. Fläche unter der Kurve mit sanftem Farbverlauf
        var areaGeom = new StreamGeometry();
        using (var sgc = areaGeom.Open())
        {
            sgc.BeginFigure(new Point(points[0].X, baselineY), true);
            foreach (var pt in points)
            {
                sgc.LineTo(pt);
            }
            sgc.LineTo(new Point(points.Last().X, baselineY));
            sgc.EndFigure(true);
        }

        var gradientBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(areaFillColor, 0.0),
                new GradientStop(Color.FromArgb(0, areaFillColor.R, areaFillColor.G, areaFillColor.B), 1.0)
            }
        };
        context.DrawGeometry(gradientBrush, null, areaGeom);

        // 2. Neon-Hauptlinie
        var lineGeom = new StreamGeometry();
        using (var sgc = lineGeom.Open())
        {
            sgc.BeginFigure(points[0], false);
            for (int i = 1; i < points.Count; i++)
            {
                sgc.LineTo(points[i]);
            }
            sgc.EndFigure(false);
        }

        var linePen = new Pen(new SolidColorBrush(lineColor), 2.2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        context.DrawGeometry(null, linePen, lineGeom);

        // 3. Markante Event-Punkte hervorheben (wenn nicht zu viele Punkte)
        if (points.Count <= 45)
        {
            var dotBrush = new SolidColorBrush(lineColor);
            var dotBorderPen = new Pen(new SolidColorBrush(Color.Parse("#060C16")), 1.5);
            foreach (var pt in points)
            {
                context.DrawEllipse(dotBrush, dotBorderPen, pt, 3.2, 3.2);
            }
        }
    }

    private void DrawHorizontalGrid(DrawingContext context, double padLeft, double padTop,
        double plotW, double plotH, long minVal, long maxVal, Func<long, double> getY)
    {
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(28, 56, 189, 248)), 1);

        int steps = 4;
        long stepVal = (maxVal - minVal) / steps;
        if (stepVal <= 0) stepVal = 1;

        for (int i = 0; i <= steps; i++)
        {
            long val = minVal + i * stepVal;
            double gy = getY(val);

            context.DrawLine(gridPen, new Point(padLeft, gy), new Point(padLeft + plotW, gy));

            // Achsenbeschriftung links
            string label = FormatAuecMetric(val);
            var ft = CreateText(label, "#64748B", 9.5);
            context.DrawText(ft, new Point(padLeft - ft.Width - 6, gy - ft.Height / 2));
        }
    }

    private void DrawTimeAxis(DrawingContext context, List<FinanceTimelinePoint> pts,
        long tMin, long tMax, double padLeft, double padTop, double plotW, double plotH, Func<long, double> getX)
    {
        double yPos = padTop + plotH + 8;

        var first = pts.First();
        var last = pts.Last();

        var ftStart = CreateText(first.WhenText, "#64748B", 9.5);
        context.DrawText(ftStart, new Point(padLeft, yPos));

        if (pts.Count > 1)
        {
            var ftEnd = CreateText(last.WhenText, "#64748B", 9.5);
            context.DrawText(ftEnd, new Point(padLeft + plotW - ftEnd.Width, yPos));

            // Mittlerer Timestamp, falls Zeitspanne groß genug
            if (pts.Count >= 3)
            {
                var mid = pts[pts.Count / 2];
                var ftMid = CreateText(mid.WhenText, "#64748B", 9.5);
                double midX = getX(mid.Time.Ticks) - ftMid.Width / 2;
                if (midX > padLeft + ftStart.Width + 10 && midX + ftMid.Width < padLeft + plotW - ftEnd.Width - 10)
                {
                    context.DrawText(ftMid, new Point(midX, yPos));
                }
            }
        }
    }

    private void DrawHoverOverlay(DrawingContext context, FinanceTimelinePoint item,
        double curX, double padTop, double plotH, double w, double h)
    {
        // 1. Vertikale Fadenkreuz-Linie
        var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 56, 189, 248)), 1, DashStyle.Dash);
        context.DrawLine(crossPen, new Point(curX, padTop), new Point(curX, padTop + plotH));

        // 2. Fadenkreuz-Zielpunkt
        var reticleBrush = new SolidColorBrush(Color.Parse("#38BDF8"));
        context.DrawEllipse(reticleBrush, null, new Point(curX, _hoverPos?.Y ?? padTop + plotH / 2), 4, 4);

        // 3. Schwebendes mobiGlas Tooltip-Badge
        string line1 = $"{item.WhenText} · {item.Detail}";
        if (string.IsNullOrWhiteSpace(item.Detail)) line1 = $"{item.WhenText} · {item.Label}";

        string line2 = item.AmountText;
        string line3 = ChartMode == 0
            ? $"Kumuliert: +{item.CumulativeIncome:N0} / -{item.CumulativeSpend:N0} aUEC"
            : $"Netto-Saldo: {(item.CumulativeNet >= 0 ? "+" : "")}{item.CumulativeNet:N0} aUEC";

        var ft1 = CreateText(line1, "#94A3B8", 10, isBold: false);
        var ft2 = CreateText(line2, item.IsIncome ? "#4ADE80" : "#F87171", 12.5, isBold: true);
        var ft3 = CreateText(line3, "#38BDF8", 10.5, isBold: true);

        double tipW = Math.Max(ft1.Width, Math.Max(ft2.Width, ft3.Width)) + 20;
        double tipH = ft1.Height + ft2.Height + ft3.Height + 16;

        double tipX = curX + 12;
        if (tipX + tipW > w - 10) tipX = curX - tipW - 12;
        if (tipX < 10) tipX = 10;

        double tipY = (_hoverPos?.Y ?? padTop + 20) - tipH / 2;
        if (tipY < padTop) tipY = padTop;
        if (tipY + tipH > h - 10) tipY = h - tipH - 10;

        var tipRect = new Rect(tipX, tipY, tipW, tipH);
        var tipBg = new SolidColorBrush(Color.FromArgb(235, 10, 22, 38));
        var tipBorder = new Pen(new SolidColorBrush(Color.Parse("#38BDF8")), 1);

        context.DrawRectangle(tipBg, tipBorder, tipRect, 5, 5);

        double textY = tipY + 7;
        context.DrawText(ft1, new Point(tipX + 10, textY));
        textY += ft1.Height + 2;
        context.DrawText(ft2, new Point(tipX + 10, textY));
        textY += ft2.Height + 2;
        context.DrawText(ft3, new Point(tipX + 10, textY));
    }

    private void DrawEmptyState(DrawingContext context, double w, double h)
    {
        var ft = CreateText(EmptyText, "#64748B", 12, isBold: true);
        context.DrawText(ft, new Point(w / 2 - ft.Width / 2, h / 2 - ft.Height / 2));
    }

    private static string FormatAuecMetric(long val)
    {
        long abs = Math.Abs(val);
        string sign = val < 0 ? "-" : "";
        if (abs >= 1_000_000)
            return $"{sign}{(abs / 1_000_000.0):0.#}M";
        if (abs >= 1_000)
            return $"{sign}{(abs / 1_000.0):0.#}k";
        return $"{val:N0}";
    }

    private static FormattedText CreateText(string text, string colorHex, double size, bool isBold = false)
    {
        return new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, isBold ? FontWeight.Bold : FontWeight.Normal),
            size,
            new SolidColorBrush(Color.Parse(colorHex))
        );
    }
}
