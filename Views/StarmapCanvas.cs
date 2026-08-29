using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using SCLogReader.Core;

namespace SCLogReader.Views;

public sealed class StarmapCanvas : Control
{
    public static readonly StyledProperty<string> SystemNameProperty =
        AvaloniaProperty.Register<StarmapCanvas, string>(nameof(SystemName), "Stanton");

    public static readonly StyledProperty<string> PlayerLocationNameProperty =
        AvaloniaProperty.Register<StarmapCanvas, string>(nameof(PlayerLocationName), "");

    public static readonly StyledProperty<StarmapObject?> SelectedObjectProperty =
        AvaloniaProperty.Register<StarmapCanvas, StarmapObject?>(nameof(SelectedObject));

    public static readonly StyledProperty<bool> ShowStationsProperty =
        AvaloniaProperty.Register<StarmapCanvas, bool>(nameof(ShowStations), true);

    public static readonly StyledProperty<bool> ShowMoonsProperty =
        AvaloniaProperty.Register<StarmapCanvas, bool>(nameof(ShowMoons), true);

    public static readonly StyledProperty<bool> ShowLandingZonesProperty =
        AvaloniaProperty.Register<StarmapCanvas, bool>(nameof(ShowLandingZones), true);

    public static readonly StyledProperty<bool> ShowJumpPointsProperty =
        AvaloniaProperty.Register<StarmapCanvas, bool>(nameof(ShowJumpPoints), true);

    public string SystemName
    {
        get => GetValue(SystemNameProperty);
        set => SetValue(SystemNameProperty, value);
    }

    public string PlayerLocationName
    {
        get => GetValue(PlayerLocationNameProperty);
        set => SetValue(PlayerLocationNameProperty, value);
    }

    public StarmapObject? SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public bool ShowStations
    {
        get => GetValue(ShowStationsProperty);
        set => SetValue(ShowStationsProperty, value);
    }

    public bool ShowMoons
    {
        get => GetValue(ShowMoonsProperty);
        set => SetValue(ShowMoonsProperty, value);
    }

    public bool ShowLandingZones
    {
        get => GetValue(ShowLandingZonesProperty);
        set => SetValue(ShowLandingZonesProperty, value);
    }

    public bool ShowJumpPoints
    {
        get => GetValue(ShowJumpPointsProperty);
        set => SetValue(ShowJumpPointsProperty, value);
    }

    public event Action<StarmapObject>? ObjectSelected;

    private double _zoom = 1.0;
    private Point _panOffset = new(0, 0);
    private bool _isPanning = false;
    private Point _panStart;

    private readonly DispatcherTimer _pulseTimer;
    private double _pulsePhase = 0;
    private StarmapObject? _hoveredObject;

    // Feste deterministische Hintergrund-Sterne
    private static readonly (double X, double Y, double Size, byte Alpha)[] Starfield = GenerateStarfield();

    private static (double X, double Y, double Size, byte Alpha)[] GenerateStarfield()
    {
        var rnd = new Random(42);
        var stars = new (double X, double Y, double Size, byte Alpha)[160];
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i] = (
                rnd.NextDouble() * 2000 - 1000,
                rnd.NextDouble() * 2000 - 1000,
                rnd.NextDouble() * 1.6 + 0.6,
                (byte)rnd.Next(40, 190)
            );
        }
        return stars;
    }

    public StarmapCanvas()
    {
        ClipToBounds = true;

        _pulseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS für flüssigen Radar-Puls
        };
        _pulseTimer.Tick += (_, _) =>
        {
            _pulsePhase = (_pulsePhase + 0.06) % (2 * Math.PI);
            InvalidateVisual();
        };
        _pulseTimer.Start();
    }

    static StarmapCanvas()
    {
        AffectsRender<StarmapCanvas>(
            SystemNameProperty,
            PlayerLocationNameProperty,
            SelectedObjectProperty,
            ShowStationsProperty,
            ShowMoonsProperty,
            ShowLandingZonesProperty,
            ShowJumpPointsProperty);
    }

    public void ResetView()
    {
        _zoom = 1.0;
        _panOffset = new Point(0, 0);
        InvalidateVisual();
    }

    public void FocusOnObject(StarmapObject obj)
    {
        _zoom = 1.6;
        _panOffset = new Point(-obj.RelX * _zoom, -obj.RelY * _zoom);
        SelectedObject = obj;
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double delta = e.Delta.Y > 0 ? 1.15 : 0.85;
        _zoom = Math.Clamp(_zoom * delta, 0.4, 3.5);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var p = e.GetCurrentPoint(this);
        if (p.Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _panStart = p.Position;

            var hit = FindObjectAt(p.Position);
            if (hit != null)
            {
                SelectedObject = hit;
                ObjectSelected?.Invoke(hit);
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var p = e.GetPosition(this);

        if (_isPanning)
        {
            _panOffset = new Point(_panOffset.X + (p.X - _panStart.X), _panOffset.Y + (p.Y - _panStart.Y));
            _panStart = p;
            InvalidateVisual();
        }
        else
        {
            var hover = FindObjectAt(p);
            if (hover != _hoveredObject)
            {
                _hoveredObject = hover;
                Cursor = hover != null ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Cross);
                InvalidateVisual();
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
    }

    private StarmapObject? FindObjectAt(Point screenPos)
    {
        var center = new Point(Bounds.Width / 2 + _panOffset.X, Bounds.Height / 2 + _panOffset.Y);
        var objects = StarmapData.GetSystemObjects(SystemName);

        StarmapObject? hit = null;
        double bestDist = 24;

        foreach (var obj in objects)
        {
            if (!IsObjectVisible(obj)) continue;
            var objPos = new Point(center.X + obj.RelX * _zoom, center.Y + obj.RelY * _zoom);
            double dist = Math.Sqrt(Math.Pow(screenPos.X - objPos.X, 2) + Math.Pow(screenPos.Y - objPos.Y, 2));
            if (dist < Math.Max(obj.Size * _zoom, 16) && dist < bestDist)
            {
                bestDist = dist;
                hit = obj;
            }
        }

        return hit;
    }

    private bool IsObjectVisible(StarmapObject obj)
    {
        return obj.Type switch
        {
            StarmapObjectType.Star => true,
            StarmapObjectType.Planet => true,
            StarmapObjectType.Moon => ShowMoons,
            StarmapObjectType.LandingZone => ShowLandingZones,
            StarmapObjectType.SpaceStation => ShowStations,
            StarmapObjectType.LagrangeStation => ShowStations,
            StarmapObjectType.JumpPoint => ShowJumpPoints,
            _ => true
        };
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        var center = new Point(w / 2 + _panOffset.X, h / 2 + _panOffset.Y);

        // 1. Deep Space Nexus Background
        var bgBrush = new SolidColorBrush(Color.Parse("#05070A"));
        context.FillRectangle(bgBrush, new Rect(0, 0, w, h));

        // Sanftes radiales Nebel-Glühen im Zentrum
        var nebulaGlow = new SolidColorBrush(Color.FromArgb(14, 127, 233, 224));
        context.DrawEllipse(nebulaGlow, null, center, 450 * _zoom, 450 * _zoom);

        // 2. Starfield
        foreach (var star in Starfield)
        {
            double sx = center.X + star.X * _zoom * 0.7;
            double sy = center.Y + star.Y * _zoom * 0.7;
            if (sx >= 0 && sx <= w && sy >= 0 && sy <= h)
            {
                var sBrush = new SolidColorBrush(Color.FromArgb(star.Alpha, 220, 235, 255));
                context.DrawEllipse(sBrush, null, new Point(sx, sy), star.Size, star.Size);
            }
        }

        // 3. Sci-Fi Radar / Range Rings
        var radarPen = new Pen(new SolidColorBrush(Color.FromArgb(18, 127, 233, 224)), 1, DashStyle.Dash);
        var radarSolidPen = new Pen(new SolidColorBrush(Color.FromArgb(12, 127, 233, 224)), 1);

        double[] rangeRings = { 90, 160, 230, 300, 360 };
        string[] rangeLabels = { "75 GM", "150 GM", "225 GM", "300 GM", "GATEWAYS" };

        for (int i = 0; i < rangeRings.Length; i++)
        {
            double r = rangeRings[i] * _zoom;
            context.DrawEllipse(null, radarPen, center, r, r);

            // Kleine Distanzmarke auf dem Ring
            DrawText(context, rangeLabels[i], new Point(center.X + r + 4, center.Y - 4), "#4B5563", 8, false, false);
        }

        // Fadenkreuz / Achsen
        context.DrawLine(radarSolidPen, new Point(0, center.Y), new Point(w, center.Y));
        context.DrawLine(radarSolidPen, new Point(center.X, 0), new Point(center.X, h));

        var objects = StarmapData.GetSystemObjects(SystemName);

        // 4. Zentralstern mit Korona & Flares
        var starObj = objects.FirstOrDefault(o => o.Type == StarmapObjectType.Star);
        if (starObj != null)
        {
            double starR = (starObj.Size / 2) * _zoom;
            var starCol = Color.Parse(starObj.ColorHex);

            // Äußerer Flare-Glow
            var flareBrush = new SolidColorBrush(Color.FromArgb(30, starCol.R, starCol.G, starCol.B));
            context.DrawEllipse(flareBrush, null, center, starR * 3.2, starR * 3.2);

            // Mittlerer Glow
            var midGlow = new SolidColorBrush(Color.FromArgb(70, starCol.R, starCol.G, starCol.B));
            context.DrawEllipse(midGlow, null, center, starR * 1.8, starR * 1.8);

            // Kern
            var coreBrush = new SolidColorBrush(starCol);
            context.DrawEllipse(coreBrush, null, center, starR, starR);

            // Name
            DrawText(context, starObj.Name, new Point(center.X, center.Y + starR + 6), "#FFD089", 11, true, true);
        }

        // 5. Orbit-Tether-Linien (Mond / Station -> Planet)
        var tetherPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 127, 233, 224)), 1, DashStyle.Dot);
        foreach (var obj in objects.Where(o => o.ParentId != null && o.ParentId != "stanton_star" && o.ParentId != "pyro_star" && o.ParentId != "nyx_star"))
        {
            if (!IsObjectVisible(obj)) continue;
            var parent = objects.FirstOrDefault(p => p.Id == obj.ParentId);
            if (parent != null)
            {
                var p1 = new Point(center.X + parent.RelX * _zoom, center.Y + parent.RelY * _zoom);
                var p2 = new Point(center.X + obj.RelX * _zoom, center.Y + obj.RelY * _zoom);
                context.DrawLine(tetherPen, p1, p2);
            }
        }

        // 6. Objekte zeichnen
        StarmapObject? playerObj = objects.FirstOrDefault(obj =>
            !string.IsNullOrEmpty(PlayerLocationName) &&
            (obj.Name.Contains(PlayerLocationName, StringComparison.OrdinalIgnoreCase) ||
             PlayerLocationName.Contains(obj.Name, StringComparison.OrdinalIgnoreCase) ||
             obj.Id.Contains(PlayerLocationName, StringComparison.OrdinalIgnoreCase)));

        if (playerObj == null && !string.IsNullOrEmpty(PlayerLocationName))
        {
            var res = StarmapData.Resolve(PlayerLocationName);
            if (!string.IsNullOrEmpty(res.DisplayName))
            {
                playerObj = objects.FirstOrDefault(o => o.Name.Equals(res.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                                                        (!string.IsNullOrEmpty(res.ParentBody) && o.Name.Equals(res.ParentBody, StringComparison.OrdinalIgnoreCase)));
            }
        }

        foreach (var obj in objects.Where(o => o.Type != StarmapObjectType.Star))
        {
            if (!IsObjectVisible(obj)) continue;

            var objPos = new Point(center.X + obj.RelX * _zoom, center.Y + obj.RelY * _zoom);
            var col = Color.Parse(obj.ColorHex);
            double objR = (obj.Size / 2) * _zoom;

            bool isSelected = SelectedObject != null && SelectedObject.Id == obj.Id;
            bool isHovered = _hoveredObject != null && _hoveredObject.Id == obj.Id;

            // Selektions-Klammern [ ◰ ◱ ◲ ◳ ] (Sci-Fi Target Reticle)
            if (isSelected)
            {
                DrawTargetReticle(context, objPos, objR + 8, "#FFD089");
            }
            else if (isHovered)
            {
                DrawTargetReticle(context, objPos, objR + 6, "#7FE9E0");
            }

            // Atmosphären-Ring für Planeten
            if (obj.Type == StarmapObjectType.Planet)
            {
                var atmoBrush = new SolidColorBrush(Color.FromArgb(35, col.R, col.G, col.B));
                context.DrawEllipse(atmoBrush, null, objPos, objR * 1.7, objR * 1.7);

                var ringPen = new Pen(new SolidColorBrush(Color.FromArgb(140, col.R, col.G, col.B)), 1.5);
                context.DrawEllipse(null, ringPen, objPos, objR + 3, objR + 3);
            }

            // Stationen / Sprungtore als Diamant / Raute
            if (obj.Type == StarmapObjectType.SpaceStation || obj.Type == StarmapObjectType.JumpPoint)
            {
                DrawDiamond(context, objPos, objR + 1, col);
            }
            else
            {
                // Planet / Mond / Landezone Sphäre
                var bodyBrush = new SolidColorBrush(col);
                context.DrawEllipse(bodyBrush, null, objPos, objR, objR);
            }

            // Beschriftung
            string labelColor = isSelected ? "#FFD089" : isHovered ? "#7FE9E0" : "#EAF1F6";
            double fontSize = obj.Type == StarmapObjectType.Planet ? 11.5 : 9.5;
            bool isBold = obj.Type == StarmapObjectType.Planet || obj.Type == StarmapObjectType.LandingZone;

            DrawText(context, obj.Name, new Point(objPos.X, objPos.Y + objR + 3), labelColor, fontSize, isBold, true);
        }

        // 7. Live Player Radar Beacon ("📍 DU BIST HIER")
        if (playerObj != null)
        {
            var pPos = new Point(center.X + playerObj.RelX * _zoom, center.Y + playerObj.RelY * _zoom);

            // 3-fache pulsierende Radarwelle
            for (int i = 0; i < 3; i++)
            {
                double phaseOffset = (_pulsePhase + i * (2 * Math.PI / 3)) % (2 * Math.PI);
                double waveR = (playerObj.Size * _zoom) + 6 + (phaseOffset / (2 * Math.PI)) * 26;
                byte waveAlpha = (byte)(Math.Max(0, (1 - phaseOffset / (2 * Math.PI))) * 180);

                var wavePen = new Pen(new SolidColorBrush(Color.FromArgb(waveAlpha, 255, 178, 62)), 1.5);
                context.DrawEllipse(null, wavePen, pPos, waveR, waveR);
            }

            // Amber "YOU" Pill Badge
            DrawPlayerBadge(context, $"📍 DU BIST HIER: {playerObj.Name}", new Point(pPos.X, pPos.Y - (playerObj.Size * _zoom) - 22));
        }

        // 8. HUD Chrome (Breadcrumb oben links, Scalebar unten links)
        DrawHudChrome(context, w, h);
    }

    private void DrawDiamond(DrawingContext context, Point center, double size, Color color)
    {
        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(new Point(center.X, center.Y - size), true);
            ctx.LineTo(new Point(center.X + size, center.Y));
            ctx.LineTo(new Point(center.X, center.Y + size));
            ctx.LineTo(new Point(center.X - size, center.Y));
            ctx.EndFigure(true);
        }

        var fill = new SolidColorBrush(color);
        var stroke = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 1);
        context.DrawGeometry(fill, stroke, geom);
    }

    private void DrawTargetReticle(DrawingContext context, Point center, double r, string colorHex)
    {
        var pen = new Pen(new SolidColorBrush(Color.Parse(colorHex)), 1.5);
        double len = Math.Max(4, r * 0.45);

        // Oben Links
        context.DrawLine(pen, new Point(center.X - r, center.Y - r + len), new Point(center.X - r, center.Y - r));
        context.DrawLine(pen, new Point(center.X - r, center.Y - r), new Point(center.X - r + len, center.Y - r));

        // Oben Rechts
        context.DrawLine(pen, new Point(center.X + r - len, center.Y - r), new Point(center.X + r, center.Y - r));
        context.DrawLine(pen, new Point(center.X + r, center.Y - r), new Point(center.X + r, center.Y - r + len));

        // Unten Links
        context.DrawLine(pen, new Point(center.X - r, center.Y + r - len), new Point(center.X - r, center.Y + r));
        context.DrawLine(pen, new Point(center.X - r, center.Y + r), new Point(center.X - r + len, center.Y + r));

        // Unten Rechts
        context.DrawLine(pen, new Point(center.X + r - len, center.Y + r), new Point(center.X + r, center.Y + r));
        context.DrawLine(pen, new Point(center.X + r, center.Y + r), new Point(center.X + r, center.Y + r - len));
    }

    private void DrawPlayerBadge(DrawingContext context, string text, Point pos)
    {
        var ft = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Rajdhani, Segoe UI, Arial", FontStyle.Normal, FontWeight.Bold),
            11,
            new SolidColorBrush(Color.Parse("#05070A")));

        double padX = 9;
        double padY = 3;
        var bgRect = new Rect(pos.X - ft.Width / 2 - padX, pos.Y - padY, ft.Width + padX * 2, ft.Height + padY * 2);

        var bgBrush = new SolidColorBrush(Color.Parse("#FFB23E"));
        context.DrawRectangle(bgBrush, null, bgRect, 4, 4);

        context.DrawText(ft, new Point(pos.X - ft.Width / 2, pos.Y));
    }

    private void DrawText(DrawingContext context, string text, Point centerPos, string colorHex, double size, bool bold, bool centerX)
    {
        var ft = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Rajdhani, Segoe UI, Arial", FontStyle.Normal, bold ? FontWeight.Bold : FontWeight.SemiBold),
            size,
            new SolidColorBrush(Color.Parse(colorHex)));

        double x = centerX ? centerPos.X - ft.Width / 2 : centerPos.X;
        context.DrawText(ft, new Point(x, centerPos.Y));
    }

    private void DrawHudChrome(DrawingContext context, double w, double h)
    {
        // 1. Breadcrumb oben links
        string sys = SystemName.ToUpperInvariant();
        string objName = SelectedObject != null ? $" > {SelectedObject.Name.ToUpperInvariant()}" : "";
        string bc = $"VERSE > {sys}{objName}";

        var ftBc = new FormattedText(
            bc,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Rajdhani, Segoe UI, Arial", FontStyle.Normal, FontWeight.Bold),
            12,
            new SolidColorBrush(Color.Parse("#7FE9E0")));

        var bcBg = new SolidColorBrush(Color.FromArgb(210, 8, 12, 18));
        var bcBorder = new Pen(new SolidColorBrush(Color.FromArgb(60, 127, 233, 224)), 1);
        context.DrawRectangle(bcBg, bcBorder, new Rect(14, 14, ftBc.Width + 20, ftBc.Height + 10), 4, 4);
        context.DrawText(ftBc, new Point(24, 19));

        // 2. Scalebar unten links
        double barWidth = 70 * _zoom;
        string scaleText = $"{Math.Round(50 / _zoom)} GM";

        var ftScale = new FormattedText(
            scaleText,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Cascadia Code, Consolas, monospace", FontStyle.Normal, FontWeight.Normal),
            9.5,
            new SolidColorBrush(Color.Parse("#8693A0")));

        var scalePen = new Pen(new SolidColorBrush(Color.Parse("#7FE9E0")), 2);
        context.DrawLine(scalePen, new Point(16, h - 22), new Point(16 + barWidth, h - 22));
        context.DrawLine(scalePen, new Point(16, h - 26), new Point(16, h - 22));
        context.DrawLine(scalePen, new Point(16 + barWidth, h - 26), new Point(16 + barWidth, h - 22));
        context.DrawText(ftScale, new Point(16, h - 18));
    }
}
