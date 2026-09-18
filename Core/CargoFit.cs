using System;
using System.Collections.Generic;
using System.Linq;

namespace SCLogMate.Core;

/// <summary>
/// Dimensions of a cargo grid in metres and SCU.
/// </summary>
public sealed record CargoGridDim(
    string GridName,
    double X, // Width (metres)
    double Y, // Length (metres)
    double Z, // Height (metres)
    int Scu,
    (double X, double Y, double Z) MaxBox);

/// <summary>
/// Definition of standard Star Citizen container crates (1.25m cell lattice).
/// </summary>
public sealed record StandardCrate(
    int Scu,
    double WidthM,
    double LengthM,
    double HeightM,
    int CellsX,
    int CellsY,
    int CellsZ);

/// <summary>
/// Position of a placed crate in grid coordinates.
/// </summary>
public sealed record PlacedCrateDto(
    int Scu,
    int X,
    int Y,
    int Z,
    int DimX,
    int DimY,
    int DimZ);

/// <summary>
/// Result of packing a single cargo grid.
/// </summary>
public sealed record GridPackingResultDto(
    string GridName,
    int GridWidthCells,
    int GridLengthCells,
    int GridHeightCells,
    int CapacityScu,
    int UsedScu,
    int FreeScu,
    IReadOnlyList<PlacedCrateDto> PlacedCrates);

/// <summary>
/// Compatibility status of an owned fleet ship.
/// </summary>
public sealed record FleetFitMatchDto(
    string ShipName,
    bool Fits,
    int TotalCapacityScu,
    int PlacedScu,
    int FreeScu,
    string StatusBadge,
    string StatusColor,
    string? Notes);

/// <summary>
/// Full outcome of the Cargo-Fit evaluation.
/// </summary>
public sealed record CargoFitResult(
    bool Fits,
    string ShipName,
    int RequestedTotalScu,
    int TotalCapacityScu,
    int TotalPlacedScu,
    int RemainingFreeScu,
    IReadOnlyDictionary<int, int> LeftoverCrates,
    IReadOnlyList<string> RejectionReasons,
    IReadOnlyList<GridPackingResultDto> Grids,
    IReadOnlyList<FleetFitMatchDto> CompatibleFleetShips);

/// <summary>
/// Cargo-Fit 3D lattice packing engine and ship cargo grid catalog for Star Citizen 4.x.
/// </summary>
public static class CargoFit
{
    public const double CellSizeMeters = 1.25;

    public static readonly IReadOnlyDictionary<int, StandardCrate> CrateDefinitions = new Dictionary<int, StandardCrate>
    {
        [1]  = new(1,  1.25, 1.25, 1.25, 1, 1, 1),
        [2]  = new(2,  1.25, 2.50, 1.25, 1, 2, 1),
        [4]  = new(4,  2.50, 2.50, 1.25, 2, 2, 1),
        [8]  = new(8,  2.50, 2.50, 2.50, 2, 2, 2),
        [16] = new(16, 2.50, 5.00, 2.50, 2, 4, 2),
        [24] = new(24, 2.50, 7.50, 2.50, 2, 6, 2),
        [32] = new(32, 2.50, 10.0, 2.50, 2, 8, 2)
    };

    /// <summary>
    /// Canonical cargo grid configurations for major Star Citizen ships.
    /// Derived from scunpacked-data / ships.json (1.25m cell lattice).
    /// </summary>
    public static readonly Dictionary<string, List<CargoGridDim>> ShipGrids = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Crusader C2 Hercules"] = [
            new("Main Deck Front", 7.5, 30.0, 5.0, 480, (2.5, 10.0, 2.5)),
            new("Main Deck Rear",  5.0, 12.5, 3.75, 216, (2.5, 10.0, 2.5))
        ],
        ["Crusader M2 Hercules"] = [
            new("Main Deck", 7.5, 30.0, 5.0, 522, (2.5, 10.0, 2.5))
        ],
        ["Drake Caterpillar"] = [
            new("Front Bay 1", 5.0, 10.0, 3.75, 144, (2.5, 10.0, 2.5)),
            new("Mid Bay 2",   5.0, 10.0, 3.75, 144, (2.5, 10.0, 2.5)),
            new("Mid Bay 3",   5.0, 10.0, 3.75, 144, (2.5, 10.0, 2.5)),
            new("Rear Bay 4",  5.0, 10.0, 3.75, 144, (2.5, 10.0, 2.5))
        ],
        ["Anvil Carrack"] = [
            new("Cargo Pod Alpha", 5.0, 10.0, 3.75, 152, (2.5, 10.0, 2.5)),
            new("Cargo Pod Beta",  5.0, 10.0, 3.75, 152, (2.5, 10.0, 2.5)),
            new("Cargo Pod Gamma", 5.0, 10.0, 3.75, 152, (2.5, 10.0, 2.5))
        ],
        ["RSI Constellation Taurus"] = [
            new("Main Cargo Hold", 5.0, 15.0, 2.5, 168, (2.5, 10.0, 2.5)),
            new("Smuggler Hold",   2.5, 2.5, 1.25, 6,   (1.25, 2.5, 1.25))
        ],
        ["RSI Constellation Andromeda"] = [
            new("Main Cargo Hold", 5.0, 10.0, 2.5, 96, (2.5, 10.0, 2.5))
        ],
        ["MISC Freelancer MAX"] = [
            new("Main Cargo Bay", 5.0, 12.5, 2.5, 120, (2.5, 10.0, 2.5))
        ],
        ["MISC Freelancer"] = [
            new("Main Hold", 2.5, 12.5, 2.5, 66, (2.5, 10.0, 2.5))
        ],
        ["Crusader C1 Spirit"] = [
            new("Cargo Walkway Left",  2.5, 10.0, 1.25, 32, (2.5, 10.0, 1.25)),
            new("Cargo Walkway Right", 2.5, 10.0, 1.25, 32, (2.5, 10.0, 1.25))
        ],
        ["Drake Corsair"] = [
            // Corsair accepts max 24 SCU length/height, not 32 SCU
            new("Elevator Bay", 5.0, 7.5, 2.5, 72, (2.5, 7.5, 2.5))
        ],
        ["Drake Cutlass Black"] = [
            new("Cargo Hold", 3.75, 7.5, 2.5, 46, (2.5, 5.0, 2.5))
        ],
        ["RSI Zeus Mk II CL"] = [
            new("Main Cargo Bay", 5.0, 10.0, 2.5, 128, (2.5, 10.0, 2.5))
        ],
        ["ARGO RAFT"] = [
            new("Container Clamp 1", 2.5, 10.0, 2.5, 32, (2.5, 10.0, 2.5)),
            new("Container Clamp 2", 2.5, 10.0, 2.5, 32, (2.5, 10.0, 2.5)),
            new("Container Clamp 3", 2.5, 10.0, 2.5, 32, (2.5, 10.0, 2.5))
        ],
        ["MISC Hull A"] = [
            new("Spindle Left",  2.5, 10.0, 2.5, 32, (2.5, 10.0, 2.5)),
            new("Spindle Right", 2.5, 10.0, 2.5, 32, (2.5, 10.0, 2.5))
        ],
        ["Crusader Mercury Star Runner"] = [
            new("Main Cargo Bay", 5.0, 12.5, 2.5, 114, (2.5, 10.0, 2.5))
        ],
        ["Drake Vulture"] = [
            new("Salvage Grid", 2.5, 3.75, 2.5, 12, (2.5, 2.5, 2.5))
        ],
        ["Aegis Reclaimer"] = [
            new("Salvage Hold", 7.5, 15.0, 5.0, 420, (2.5, 10.0, 2.5))
        ],
        ["Consolidated Outland Nomad"] = [
            new("Open Bed", 3.75, 5.0, 1.25, 24, (2.5, 5.0, 1.25))
        ],
        ["Aegis Avenger Titan"] = [
            new("Cargo Bay", 2.5, 3.75, 1.25, 8, (1.25, 2.5, 1.25))
        ]
    };

    /// <summary>
    /// Evaluates if a collection of crates can be packed onto the cargo grids of a ship.
    /// </summary>
    public static CargoFitResult Pack(
        string shipName,
        IReadOnlyDictionary<int, int> requestedCrates,
        IReadOnlyList<string>? ownedFleetShips = null)
    {
        var matchedShipKey = ShipGrids.Keys.FirstOrDefault(k => k.Contains(shipName, StringComparison.OrdinalIgnoreCase)
                                                              || shipName.Contains(k, StringComparison.OrdinalIgnoreCase));
        
        List<CargoGridDim> grids = matchedShipKey != null ? ShipGrids[matchedShipKey] : [];
        if (grids.Count == 0)
        {
            // Generic single-box fallback grid if ship is unknown
            grids = [new("Standard Grid", 5.0, 10.0, 2.5, 96, (2.5, 10.0, 2.5))];
            matchedShipKey = shipName;
        }

        int requestedTotalScu = requestedCrates.Sum(c => c.Key * c.Value);
        int totalCapacityScu = grids.Sum(g => g.Scu);

        // Flatten crates into individual items, sorted largest first (First-Fit Decreasing)
        var crateList = new List<StandardCrate>();
        foreach (var (scu, count) in requestedCrates)
        {
            if (CrateDefinitions.TryGetValue(scu, out var crateDef))
            {
                for (int i = 0; i < count; i++) crateList.Add(crateDef);
            }
        }
        crateList = [.. crateList.OrderByDescending(c => c.Scu)];

        var gridResults = new List<GridPackingResultDto>();
        var leftovers = new Dictionary<int, int>();
        var reasons = new List<string>();

        // Create 3D cell occupancy maps for each grid
        var gridBins = grids.Select(g => new GridBin(g)).ToList();

        foreach (var crate in crateList)
        {
            bool placed = false;
            string? lastReason = null;

            foreach (var bin in gridBins)
            {
                if (bin.TryPlace(crate, out var placement, out var failReason))
                {
                    placed = true;
                    break;
                }
                lastReason = failReason;
            }

            if (!placed)
            {
                leftovers[crate.Scu] = leftovers.GetValueOrDefault(crate.Scu) + 1;
                if (lastReason != null && !reasons.Contains(lastReason))
                {
                    reasons.Add(lastReason);
                }
            }
        }

        foreach (var bin in gridBins)
        {
            gridResults.Add(bin.ToDto());
        }

        int placedScu = gridResults.Sum(g => g.UsedScu);
        int remainingFreeScu = Math.Max(0, totalCapacityScu - placedScu);
        bool fits = leftovers.Count == 0;

        // Fleet Matcher: Check which player-owned ships can fit this exact load
        var fleetMatches = new List<FleetFitMatchDto>();
        if (ownedFleetShips != null && ownedFleetShips.Count > 0)
        {
            foreach (var fleetShip in ownedFleetShips.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var fKey = ShipGrids.Keys.FirstOrDefault(k => k.Contains(fleetShip, StringComparison.OrdinalIgnoreCase)
                                                            || fleetShip.Contains(k, StringComparison.OrdinalIgnoreCase));
                if (fKey == null) continue;

                var fGrids = ShipGrids[fKey];
                int fCapacity = fGrids.Sum(g => g.Scu);
                
                // Pack against fleet ship
                var testBins = fGrids.Select(g => new GridBin(g)).ToList();
                int fPlacedScu = 0;
                bool allPlaced = true;

                foreach (var c in crateList)
                {
                    bool cPlaced = false;
                    foreach (var tb in testBins)
                    {
                        if (tb.TryPlace(c, out _, out _))
                        {
                            cPlaced = true;
                            fPlacedScu += c.Scu;
                            break;
                        }
                    }
                    if (!cPlaced)
                    {
                        allPlaced = false;
                        break;
                    }
                }

                string badge = allPlaced ? "PASST PERFEKT ✓" : fCapacity >= requestedTotalScu ? "KAPAZITÄT REICHT, ABER KISTEN-LIMIT" : "ZU KLEIN ✖";
                string color = allPlaced ? "#10B981" : fCapacity >= requestedTotalScu ? "#F59E0B" : "#EF4444";

                fleetMatches.Add(new(
                    fleetShip,
                    allPlaced,
                    fCapacity,
                    allPlaced ? requestedTotalScu : fPlacedScu,
                    Math.Max(0, fCapacity - (allPlaced ? requestedTotalScu : fPlacedScu)),
                    badge,
                    color,
                    allPlaced ? null : $"Max. Kistengröße oder Gitter-Geometrie blockiert die Restkisten."
                ));
            }
        }

        return new CargoFitResult(
            fits,
            matchedShipKey ?? shipName,
            requestedTotalScu,
            totalCapacityScu,
            placedScu,
            remainingFreeScu,
            leftovers,
            reasons,
            gridResults,
            fleetMatches
        );
    }

    private sealed class GridBin
    {
        public CargoGridDim Grid { get; }
        public int WidthCells { get; }
        public int LengthCells { get; }
        public int HeightCells { get; }

        private readonly bool[,,] _occupied;
        private readonly List<PlacedCrateDto> _placed = [];

        public GridBin(CargoGridDim grid)
        {
            Grid = grid;
            WidthCells = Math.Max(1, (int)Math.Round(grid.X / CellSizeMeters));
            LengthCells = Math.Max(1, (int)Math.Round(grid.Y / CellSizeMeters));
            HeightCells = Math.Max(1, (int)Math.Round(grid.Z / CellSizeMeters));
            _occupied = new bool[WidthCells, LengthCells, HeightCells];
        }

        public bool TryPlace(StandardCrate crate, out PlacedCrateDto placement, out string? reason)
        {
            placement = null!;
            reason = null;

            // Check MaxBox bounds for this grid
            if (crate.WidthM > Grid.MaxBox.X + 0.05 || crate.LengthM > Grid.MaxBox.Y + 0.05 || crate.HeightM > Grid.MaxBox.Z + 0.05)
            {
                // Can it be rotated horizontally (X <-> Y)?
                if (crate.LengthM > Grid.MaxBox.X + 0.05 || crate.WidthM > Grid.MaxBox.Y + 0.05 || crate.HeightM > Grid.MaxBox.Z + 0.05)
                {
                    reason = $"Kiste ({crate.Scu} SCU, {crate.WidthM}×{crate.LengthM}×{crate.HeightM}m) überschreitet MaxBox-Grenze von {Grid.GridName} ({Grid.MaxBox.X}×{Grid.MaxBox.Y}×{Grid.MaxBox.Z}m).";
                    return false;
                }
            }

            // Two valid orientations: Standard (CellsX × CellsY) or Rotated 90° (CellsY × CellsX)
            var orientations = new List<(int dx, int dy, int dz)>
            {
                (crate.CellsX, crate.CellsY, crate.CellsZ)
            };
            if (crate.CellsX != crate.CellsY)
            {
                orientations.Add((crate.CellsY, crate.CellsX, crate.CellsZ));
            }

            // First-fit search: lowest Z first, then Y (front-to-back), then X (left-to-right)
            for (int z = 0; z < HeightCells; z++)
            {
                for (int y = 0; y < LengthCells; y++)
                {
                    for (int x = 0; x < WidthCells; x++)
                    {
                        foreach (var (dx, dy, dz) in orientations)
                        {
                            if (CanFitAt(x, y, z, dx, dy, dz))
                            {
                                PlaceAt(x, y, z, dx, dy, dz);
                                placement = new PlacedCrateDto(crate.Scu, x, y, z, dx, dy, dz);
                                _placed.Add(placement);
                                return true;
                            }
                        }
                    }
                }
            }

            reason = $"Kein ausreichender zusammenhängender Gitterplatz in {Grid.GridName} für {crate.Scu} SCU ({crate.CellsX}×{crate.CellsY}×{crate.CellsZ} Zellen).";
            return false;
        }

        private bool CanFitAt(int x, int y, int z, int dx, int dy, int dz)
        {
            if (x + dx > WidthCells || y + dy > LengthCells || z + dz > HeightCells)
                return false;

            // Check if all target cells are unoccupied
            for (int iz = 0; iz < dz; iz++)
            {
                for (int iy = 0; iy < dy; iy++)
                {
                    for (int ix = 0; ix < dx; ix++)
                    {
                        if (_occupied[x + ix, y + iy, z + iz])
                            return false;
                    }
                }
            }

            // Check physical support: if not on floor (z > 0), every cell underneath must be solid
            if (z > 0)
            {
                for (int iy = 0; iy < dy; iy++)
                {
                    for (int ix = 0; ix < dx; ix++)
                    {
                        if (!_occupied[x + ix, y + iy, z - 1])
                            return false;
                    }
                }
            }

            return true;
        }

        private void PlaceAt(int x, int y, int z, int dx, int dy, int dz)
        {
            for (int iz = 0; iz < dz; iz++)
            {
                for (int iy = 0; iy < dy; iy++)
                {
                    for (int ix = 0; ix < dx; ix++)
                    {
                        _occupied[x + ix, y + iy, z + iz] = true;
                    }
                }
            }
        }

        public GridPackingResultDto ToDto()
        {
            int usedScu = _placed.Sum(p => p.Scu);
            int freeScu = Math.Max(0, Grid.Scu - usedScu);
            return new GridPackingResultDto(
                Grid.GridName,
                WidthCells,
                LengthCells,
                HeightCells,
                Grid.Scu,
                usedScu,
                freeScu,
                _placed
            );
        }
    }
}
