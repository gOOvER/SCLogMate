using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using SCLogMate.Core;

namespace SCLogMate.Views;

public sealed class StarmapCanvas : Control
{
    public static readonly StyledProperty<string> SystemNameProperty =
        AvaloniaProperty.Register<StarmapCanvas, string>(nameof(SystemName), "Stanton");

    public static readonly StyledProperty<string> PlayerLocationNameProperty =
        AvaloniaProperty.Register<StarmapCanvas, string>(nameof(PlayerLocationName), "");

    public static readonly StyledProperty<StarmapObject?> SelectedObjectProperty =
        AvaloniaProperty.Register<StarmapCanvas, StarmapObject?>(nameof(SelectedObject));

    public static readonly StyledProperty<QuantumDriveProfile?> SelectedDriveProperty =
        AvaloniaProperty.Register<StarmapCanvas, QuantumDriveProfile?>(nameof(SelectedDrive));

    public static readonly StyledProperty<int> FocusRequestProperty =
        AvaloniaProperty.Register<StarmapCanvas, int>(nameof(FocusRequest));

    public static readonly StyledProperty<bool> ShowStationsProperty =
        AvaloniaProperty.Register<StarmapCanvas, bool>(nameof(ShowStations), true);

    public static readonly StyledProperty<bool> ShowMoonsProperty =
        AvaloniaProperty.Register<StarmapCanvas, bool>(nameof(ShowMoons), true);

    public static readonly StyledProperty<bool> ShowLandingZonesProperty =
        AvaloniaProperty.Register<StarmapCanvas, bool>(nameof(ShowLandingZones), true);

    public static readonly StyledProperty<bool> ShowJumpPointsProperty =
        AvaloniaProperty.Register<StarmapCanvas, bool>(nameof(ShowJumpPoints), true);

    public static readonly StyledProperty<IEnumerable<Models.UserPoi>?> UserPoisProperty =
        AvaloniaProperty.Register<StarmapCanvas, IEnumerable<Models.UserPoi>?>(nameof(UserPois));

    public static readonly StyledProperty<IEnumerable<string>?> RouteLocationsProperty =
        AvaloniaProperty.Register<StarmapCanvas, IEnumerable<string>?>(nameof(RouteLocations));

    public string SystemName
    {
        get => GetValue(SystemNameProperty);
        set => SetValue(SystemNameProperty, value);
    }

    public IEnumerable<string>? RouteLocations
    {
        get => GetValue(RouteLocationsProperty);
        set => SetValue(RouteLocationsProperty, value);
    }

    public IEnumerable<Models.UserPoi>? UserPois
    {
        get => GetValue(UserPoisProperty);
        set => SetValue(UserPoisProperty, value);
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

    public QuantumDriveProfile? SelectedDrive
    {
        get => GetValue(SelectedDriveProperty);
        set => SetValue(SelectedDriveProperty, value);
    }

    public int FocusRequest
    {
        get => GetValue(FocusRequestProperty);
        set => SetValue(FocusRequestProperty, value);
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
    public event Action<string>? SystemJumpRequested;

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
        var stars = new (double X, double Y, double Size, byte Alpha)[180];
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i] = (
                rnd.NextDouble() * 2600 - 1300,
                rnd.NextDouble() * 2600 - 1300,
                rnd.NextDouble() * 1.8 + 0.5,
                (byte)rnd.Next(35, 195)
            );
        }
        return stars;
    }

    public StarmapCanvas()
    {
        ClipToBounds = true;

        _pulseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        _pulseTimer.Tick += (s, e) =>
        {
            _pulsePhase = (_pulsePhase + 0.08) % (Math.PI * 2);
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
            SelectedDriveProperty,
            ShowStationsProperty,
            ShowMoonsProperty,
            ShowLandingZonesProperty,
            ShowJumpPointsProperty,
            UserPoisProperty,
            RouteLocationsProperty
        );
    }

    public void ResetView()
    {
        _zoom = 1.0;
        _panOffset = new Point(0, 0);
        InvalidateVisual();
    }

    public void FocusOnObject(StarmapObject obj)
    {
        SelectedObject = obj;
        _panOffset = new Point(-obj.RelX * _zoom, -obj.RelY * _zoom);
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FocusRequestProperty && SelectedObject is not null)
        {
            FocusOnObject(SelectedObject);
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double delta = e.Delta.Y;
        double oldZoom = _zoom;
        _zoom = Math.Clamp(_zoom + delta * 0.15 * _zoom, 0.35, 6.0);

        var mousePos = e.GetPosition(this);
        var center = new Point(Bounds.Width / 2 + _panOffset.X, Bounds.Height / 2 + _panOffset.Y);
        double zoomFactor = _zoom / oldZoom;
        _panOffset = new Point(
            mousePos.X - (mousePos.X - center.X) * zoomFactor - Bounds.Width / 2,
            mousePos.Y - (mousePos.Y - center.Y) * zoomFactor - Bounds.Height / 2
        );

        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);

        if (pt.Properties.IsLeftButtonPressed)
        {
            var hit = FindObjectAt(pt.Position);
            if (hit != null)
            {
                SelectedObject = hit;
                ObjectSelected?.Invoke(hit);

                // Wenn auf ein Sprungtor geklickt wird -> Systemwechsel anbieten / ausführen
                if (hit.Type == StarmapObjectType.JumpPoint && !string.IsNullOrEmpty(hit.TargetSystem))
                {
                    SystemJumpRequested?.Invoke(hit.TargetSystem);
                }

                InvalidateVisual();
                return;
            }
        }

        if (pt.Properties.IsRightButtonPressed || pt.Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _panStart = pt.Position - _panOffset;
            e.Pointer.Capture(this);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetPosition(this);

        if (_isPanning)
        {
            _panOffset = pt - _panStart;
            InvalidateVisual();
        }
        else
        {
            var hover = FindObjectAt(pt);
            if (hover != _hoveredObject)
            {
                _hoveredObject = hover;
                Cursor = hover != null ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow);
                InvalidateVisual();
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
        }
    }

    private StarmapObject? FindObjectAt(Point screenPos)
    {
        var center = new Point(Bounds.Width / 2 + _panOffset.X, Bounds.Height / 2 + _panOffset.Y);
        var objects = StarmapData.GetSystemObjects(SystemName);

        StarmapObject? hit = null;
        double minDist = 18.0;

        foreach (var obj in objects)
        {
            if (!IsObjectVisible(obj)) continue;

            var objPos = new Point(center.X + obj.RelX * _zoom, center.Y + obj.RelY * _zoom);
            double dist = Math.Sqrt(Math.Pow(screenPos.X - objPos.X, 2) + Math.Pow(screenPos.Y - objPos.Y, 2));
            double hitRadius = Math.Max(12.0, (obj.Size * _zoom) / 2 + 5.0);

            if (dist <= hitRadius && dist < minDist)
            {
                minDist = dist;
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

        // 1. Tiefschwarzer Weltraum-Hintergrund (Cosmic Void)
        context.FillRectangle(new SolidColorBrush(Color.Parse("#040711")), new Rect(0, 0, w, h));

        // 2. Sternenfeld
        foreach (var star in Starfield)
        {
            double sx = center.X + star.X * (_zoom * 0.45);
            double sy = center.Y + star.Y * (_zoom * 0.45);

            if (sx >= 0 && sx <= w && sy >= 0 && sy <= h)
            {
                var starBrush = new SolidColorBrush(Color.FromArgb(star.Alpha, 190, 225, 255));
                context.DrawEllipse(starBrush, null, new Point(sx, sy), star.Size, star.Size);
            }
        }

        // 3. Sci-Fi Radar-Grid & Koordinatenringe
        var radarPen = new Pen(new SolidColorBrush(Color.FromArgb(20, 56, 189, 248)), 1, DashStyle.Dash);
        var radarSolidPen = new Pen(new SolidColorBrush(Color.FromArgb(32, 56, 189, 248)), 1);

        double[] ringRadii = { 90, 170, 250, 330, 390 };
        foreach (var r in ringRadii)
        {
            double scaledR = r * _zoom;
            context.DrawEllipse(null, radarPen, center, scaledR, scaledR);
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
            var flareBrush = new SolidColorBrush(Color.FromArgb(28, starCol.R, starCol.G, starCol.B));
            context.DrawEllipse(flareBrush, null, center, starR * 3.5, starR * 3.5);

            // Mittlerer Glow
            var midGlow = new SolidColorBrush(Color.FromArgb(70, starCol.R, starCol.G, starCol.B));
            context.DrawEllipse(midGlow, null, center, starR * 1.9, starR * 1.9);

            // Kern
            var coreBrush = new SolidColorBrush(starCol);
            context.DrawEllipse(coreBrush, null, center, starR, starR);

            // Name
            DrawText(context, starObj.Name, new Point(center.X, center.Y + starR + 7), "#FFD089", 11, true, true);
        }

        // 5. Orbit-Tether-Linien (Mond / Station -> Planet)
        var tetherPen = new Pen(new SolidColorBrush(Color.FromArgb(35, 56, 189, 248)), 1, DashStyle.Dot);
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

        // 6. Spieler-Standort ermitteln
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

        // 7. Vektor-Fluglinie (Quantum Flight Route) zeichnen
        if (playerObj != null && SelectedObject != null && SelectedObject.Id != playerObj.Id)
        {
            var pPos = new Point(center.X + playerObj.RelX * _zoom, center.Y + playerObj.RelY * _zoom);
            var tPos = new Point(center.X + SelectedObject.RelX * _zoom, center.Y + SelectedObject.RelY * _zoom);

            // Glowing Line
            var glowPen = new Pen(new SolidColorBrush(Color.FromArgb(45, 56, 189, 248)), 5);
            var routePen = new Pen(new SolidColorBrush(Color.Parse("#38BDF8")), 1.8, DashStyle.Dash);
            context.DrawLine(glowPen, pPos, tPos);
            context.DrawLine(routePen, pPos, tPos);

            // Flugdistanz & Dauer Badge in der Mitte der Linie
            var drive = SelectedDrive ?? StarmapData.AvailableDrives[0];
            var (distKm, distGm, flightTime) = StarmapData.CalculateRoute(playerObj, SelectedObject, drive);
            var midPoint = new Point((pPos.X + tPos.X) / 2, (pPos.Y + tPos.Y) / 2);

            string routeText = $"✈ {distGm:F1} GM ({distKm:N0} km) · {flightTime.Minutes}m {flightTime.Seconds:D2}s ({drive.Name})";
            DrawRouteBadge(context, routeText, midPoint);
        }

        // 7b. Mehretappen-Flugroute aus RouteLocations
        if (RouteLocations != null)
        {
            var resolvedPoints = new List<(Point Pt, StarmapObject Obj)>();
            foreach (var locName in RouteLocations)
            {
                if (string.IsNullOrWhiteSpace(locName)) continue;
                var obj = objects.FirstOrDefault(o => o.Name.Equals(locName, StringComparison.OrdinalIgnoreCase) ||
                                                      o.Id.Equals(locName, StringComparison.OrdinalIgnoreCase));
                if (obj == null)
                {
                    var res = StarmapData.Resolve(locName);
                    if (!string.IsNullOrEmpty(res.DisplayName))
                    {
                        obj = objects.FirstOrDefault(o => o.Name.Equals(res.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                                                          (!string.IsNullOrEmpty(res.ParentBody) && o.Name.Equals(res.ParentBody, StringComparison.OrdinalIgnoreCase)));
                    }
                }
                if (obj != null)
                {
                    var pt = new Point(center.X + obj.RelX * _zoom, center.Y + obj.RelY * _zoom);
                    if (resolvedPoints.Count == 0 || resolvedPoints[^1].Obj.Id != obj.Id)
                    {
                        resolvedPoints.Add((pt, obj));
                    }
                }
            }

            if (resolvedPoints.Count > 1)
            {
                var trailGlowPen = new Pen(new SolidColorBrush(Color.FromArgb(50, 96, 165, 250)), 4);
                var trailLinePen = new Pen(new SolidColorBrush(Color.Parse("#38BDF8")), 1.6, DashStyle.Dash);

                for (int i = 0; i < resolvedPoints.Count - 1; i++)
                {
                    context.DrawLine(trailGlowPen, resolvedPoints[i].Pt, resolvedPoints[i + 1].Pt);
                    context.DrawLine(trailLinePen, resolvedPoints[i].Pt, resolvedPoints[i + 1].Pt);
                }

                // Wegpunkt-Nummern
                for (int i = 0; i < resolvedPoints.Count; i++)
                {
                    var p = resolvedPoints[i].Pt;
                    var badgeBrush = new SolidColorBrush(Color.Parse("#0C233C"));
                    var badgeBorder = new Pen(new SolidColorBrush(Color.Parse("#38BDF8")), 1.2);
                    context.DrawEllipse(badgeBrush, badgeBorder, p, 8, 8);
                    DrawText(context, (i + 1).ToString(), new Point(p.X, p.Y - 5), "#38BDF8", 8.5, true, true);
                }
            }
        }

        // 8. Objekte zeichnen
        foreach (var obj in objects.Where(o => o.Type != StarmapObjectType.Star))
        {
            if (!IsObjectVisible(obj)) continue;

            var objPos = new Point(center.X + obj.RelX * _zoom, center.Y + obj.RelY * _zoom);
            var col = Color.Parse(obj.ColorHex);
            double objR = (obj.Size / 2) * _zoom;

            bool isSelected = SelectedObject != null && SelectedObject.Id == obj.Id;
            bool isHovered = _hoveredObject != null && _hoveredObject.Id == obj.Id;

            // Halo für Sicherheits- / Gefahrenzonen
            if (obj.SecurityLevel == "Lawless" || !obj.HasArmistice)
            {
                var dangerBrush = new SolidColorBrush(Color.FromArgb(35, 239, 68, 68)); // Rotes Warn-Halo
                context.DrawEllipse(dangerBrush, null, objPos, objR * 2.2, objR * 2.2);
            }

            // Selektions-Klammern (Target Reticle)
            if (isSelected)
            {
                DrawTargetReticle(context, objPos, objR + 8, "#FFB23E");
            }
            else if (isHovered)
            {
                DrawTargetReticle(context, objPos, objR + 6, "#38BDF8");
            }

            // Atmosphären-Ring für Planeten
            if (obj.Type == StarmapObjectType.Planet)
            {
                var atmoBrush = new SolidColorBrush(Color.FromArgb(35, col.R, col.G, col.B));
                context.DrawEllipse(atmoBrush, null, objPos, objR * 1.7, objR * 1.7);

                var ringPen = new Pen(new SolidColorBrush(Color.FromArgb(140, col.R, col.G, col.B)), 1.5);
                context.DrawEllipse(null, ringPen, objPos, objR + 3, objR + 3);
            }

            // Stationen / Lagrange / Sprungtore als Diamant / Raute / Vortex
            if (obj.Type == StarmapObjectType.SpaceStation || obj.Type == StarmapObjectType.LagrangeStation || obj.Type == StarmapObjectType.JumpPoint)
            {
                DrawDiamond(context, objPos, objR + 1.5, col);
            }
            else
            {
                // Planet / Mond / Landezone Sphäre
                var bodyBrush = new SolidColorBrush(col);
                context.DrawEllipse(bodyBrush, null, objPos, objR, objR);
            }

            // Beschriftung & Spezialisierungs-Icon
            string labelColor = isSelected ? "#FFB23E" : isHovered ? "#38BDF8" : "#EAF1F6";
            double fontSize = obj.Type == StarmapObjectType.Planet ? 11.5 : 9.5;
            bool isBold = obj.Type == StarmapObjectType.Planet || obj.Type == StarmapObjectType.LandingZone;

            string label = obj.Name;
            if (!string.IsNullOrEmpty(obj.Specialization) && _zoom > 1.2)
            {
                var icon = obj.Specialization.Split(' ')[0];
                label = $"{icon} {obj.Name}";
            }

            DrawText(context, label, new Point(objPos.X, objPos.Y + objR + 3), labelColor, fontSize, isBold, true);
        }

        // 9. Live Player Radar Beacon ("📍 DU BIST HIER")
        if (playerObj != null)
        {
            var pPos = new Point(center.X + playerObj.RelX * _zoom, center.Y + playerObj.RelY * _zoom);

            for (int i = 0; i < 3; i++)
            {
                double phaseOffset = (_pulsePhase + i * (2 * Math.PI / 3)) % (2 * Math.PI);
                double waveR = (playerObj.Size * _zoom) + 6 + (phaseOffset / (2 * Math.PI)) * 26;
                byte waveAlpha = (byte)(Math.Max(0, (1 - phaseOffset / (2 * Math.PI))) * 180);

                var wavePen = new Pen(new SolidColorBrush(Color.FromArgb(waveAlpha, 255, 178, 62)), 1.5);
                context.DrawEllipse(null, wavePen, pPos, waveR, waveR);
            }

            DrawPlayerBadge(context, $"📍 DU BIST HIER: {playerObj.Name}", new Point(pPos.X, pPos.Y - (playerObj.Size * _zoom) - 22));
        }

        // 10. Persönliche POIs / Notizen auf der Starmap
        if (UserPois != null)
        {
            foreach (var poi in UserPois)
            {
                if (!string.Equals(poi.System, SystemName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var targetObj = objects.FirstOrDefault(o => o.Name.Equals(poi.Body, StringComparison.OrdinalIgnoreCase) ||
                                                            o.Id.Equals(poi.Body, StringComparison.OrdinalIgnoreCase));
                if (targetObj == null) continue;

                var poiPos = new Point(center.X + (targetObj.RelX + 15) * _zoom, center.Y + (targetObj.RelY - 18) * _zoom);
                Color poiCol;
                try { poiCol = Color.Parse(poi.Color); } catch { poiCol = Color.Parse("#F59E0B"); }

                DrawDiamond(context, poiPos, 5 * _zoom + 2, poiCol);
                string catIcon = poi.Category switch
                {
                    "Mining" => "⛏️",
                    "Salvage" => "🧲",
                    "Secret" => "🔒",
                    "Bunker" => "🛡️",
                    "Trade" => "💰",
                    _ => "📌"
                };
                DrawText(context, $"{catIcon} {poi.Name}", new Point(poiPos.X, poiPos.Y + 8), "#FBBF24", 9.0, true, true);
            }
        }

        // 11. HUD Chrome (Breadcrumb & Scalebar)
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

        context.DrawLine(pen, new Point(center.X - r, center.Y - r + len), new Point(center.X - r, center.Y - r));
        context.DrawLine(pen, new Point(center.X - r, center.Y - r), new Point(center.X - r + len, center.Y - r));

        context.DrawLine(pen, new Point(center.X + r - len, center.Y - r), new Point(center.X + r, center.Y - r));
        context.DrawLine(pen, new Point(center.X + r, center.Y - r), new Point(center.X + r, center.Y - r + len));

        context.DrawLine(pen, new Point(center.X - r, center.Y + r - len), new Point(center.X - r, center.Y + r));
        context.DrawLine(pen, new Point(center.X - r, center.Y + r), new Point(center.X - r + len, center.Y + r));

        context.DrawLine(pen, new Point(center.X + r - len, center.Y + r), new Point(center.X + r, center.Y + r));
        context.DrawLine(pen, new Point(center.X + r, center.Y + r), new Point(center.X + r, center.Y + r - len));
    }

    private void DrawRouteBadge(DrawingContext context, string text, Point center)
    {
        var ft = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Cascadia Code, Consolas, monospace", FontStyle.Normal, FontWeight.Bold),
            10.5,
            new SolidColorBrush(Color.Parse("#38BDF8"))
        );

        double padX = 8;
        double padY = 3;
        var rect = new Rect(center.X - ft.Width / 2 - padX, center.Y - ft.Height / 2 - padY, ft.Width + padX * 2, ft.Height + padY * 2);

        context.FillRectangle(new SolidColorBrush(Color.FromArgb(220, 6, 12, 22)), rect, 5);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#1A3857")), 1), rect, 5);
        context.DrawText(ft, new Point(center.X - ft.Width / 2, center.Y - ft.Height / 2));
    }

    private void DrawPlayerBadge(DrawingContext context, string text, Point pos)
    {
        var ft = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
            10.5,
            new SolidColorBrush(Color.Parse("#FFB23E"))
        );

        double padX = 8;
        double padY = 3;
        var rect = new Rect(pos.X - ft.Width / 2 - padX, pos.Y - ft.Height / 2 - padY, ft.Width + padX * 2, ft.Height + padY * 2);

        context.FillRectangle(new SolidColorBrush(Color.FromArgb(220, 11, 19, 32)), rect, 5);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#FFB23E")), 1.2), rect, 5);
        context.DrawText(ft, new Point(pos.X - ft.Width / 2, pos.Y - ft.Height / 2));
    }

    private void DrawHudChrome(DrawingContext context, double w, double h)
    {
        // 1. Breadcrumb oben links
        string sysTitle = $"✦ STARMAP // {SystemName.ToUpperInvariant()} SYSTEM";
        var ftSys = new FormattedText(
            sysTitle,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Cascadia Code, Consolas, monospace", FontStyle.Normal, FontWeight.Bold),
            12,
            new SolidColorBrush(Color.Parse("#38BDF8"))
        );
        context.DrawText(ftSys, new Point(16, 16));

        // 2. Zoom & Maßstab unten links
        double scaleKm = (100 / _zoom) * 150000.0;
        string scaleText = scaleKm >= 1000000.0 ? $"{scaleKm / 1000000.0:F1} GM" : $"{scaleKm:N0} km";
        var ftScale = new FormattedText(
            $"MAßSTAB: {scaleText}  ·  ZOOM: {_zoom * 100:F0}%",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Cascadia Code, Consolas, monospace", FontStyle.Normal, FontWeight.SemiBold),
            9.5,
            new SolidColorBrush(Color.Parse("#7E97AD"))
        );
        context.DrawText(ftScale, new Point(16, h - 26));

        // Maßstabs-Linie
        var scalePen = new Pen(new SolidColorBrush(Color.Parse("#38BDF8")), 2);
        context.DrawLine(scalePen, new Point(16, h - 30), new Point(116, h - 30));
    }

    private void DrawText(DrawingContext context, string text, Point pos, string colorHex, double size, bool isBold = false, bool center = false)
    {
        var ft = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, isBold ? FontWeight.Bold : FontWeight.Normal),
            size,
            new SolidColorBrush(Color.Parse(colorHex))
        );

        double x = center ? pos.X - ft.Width / 2 : pos.X;
        double y = pos.Y;

        // Subtiler Textschatten für maximale Lesbarkeit im Weltraum
        var shadowFt = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, isBold ? FontWeight.Bold : FontWeight.Normal),
            size,
            new SolidColorBrush(Color.FromArgb(200, 4, 7, 17))
        );
        context.DrawText(shadowFt, new Point(x + 1, y + 1));
        context.DrawText(ft, new Point(x, y));
    }
}
