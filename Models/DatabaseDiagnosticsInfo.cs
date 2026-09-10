using System;
using System.Collections.Generic;

namespace SCLogMate.Models;

/// <summary>
/// Detaillierte Diagnose- und Statusinformationen der lokalen SQLite-Datenbank (sessions.db).
/// Beinhaltet Schema- & Parser-Versionen, Struktur- und Integritätsprüfung sowie Datensatz-Metriken.
/// </summary>
public class DatabaseDiagnosticsInfo
{
    public int CurrentSchemaVersion { get; set; } = Core.Database.CurrentSchemaVersion;
    public int InstalledSchemaVersion { get; set; }
    public int CurrentParserVersion { get; set; } = Core.Database.CurrentParserVersion;
    public int InstalledParserVersion { get; set; }
    public string SqliteVersion { get; set; } = "";
    public string DatabasePath { get; set; } = "";
    public long DatabaseSizeBytes { get; set; }
    public string FormattedSize => Core.Database.FormatBytes(DatabaseSizeBytes);
    public string JournalMode { get; set; } = "WAL";
    public bool IntegrityCheckOk { get; set; }
    public string IntegrityMessage { get; set; } = "";
    public DateTime CheckedAt { get; set; } = DateTime.Now;

    // Datensatz-Statistiken
    public int SessionCount { get; set; }
    public int EventCount { get; set; }
    public int ContractCount { get; set; }
    public int FleetShipCount { get; set; }
    public int PoiCount { get; set; }
    public int ReputationCount { get; set; }
    public int WarehouseItemCount { get; set; }
    public int WikiItemCount { get; set; }

    public int TotalRecordsCount => SessionCount + EventCount + ContractCount + FleetShipCount + PoiCount + ReputationCount + WarehouseItemCount + WikiItemCount;

    // Tabellen- & Struktur-Validierung
    public List<string> ExistingTables { get; set; } = new();
    public List<string> MissingTables { get; set; } = new();
    public List<string> MissingColumns { get; set; } = new();
    public List<string> MissingIndexes { get; set; } = new();

    public bool IsSchemaUpToDate => InstalledSchemaVersion == CurrentSchemaVersion;
    public bool IsParserUpToDate => InstalledParserVersion == CurrentParserVersion;
    public bool IsStructureValid => IntegrityCheckOk && MissingTables.Count == 0 && MissingColumns.Count == 0 && MissingIndexes.Count == 0 && IsSchemaUpToDate;

    public string SchemaBadgeText => IsSchemaUpToDate ? "✓ Aktuell" : $"v{InstalledSchemaVersion} (Soll: v{CurrentSchemaVersion})";
    public string SchemaBadgeColor => IsSchemaUpToDate ? "#4ADE80" : "#F59E0B";
    public string SchemaBadgeBackground => IsSchemaUpToDate ? "#0D2818" : "#2E1B05";

    public string ParserBadgeText => IsParserUpToDate ? "✓ Synchron" : $"v{InstalledParserVersion} (Neu: v{CurrentParserVersion})";
    public string ParserBadgeColor => IsParserUpToDate ? "#4ADE80" : "#38BDF8";
    public string ParserBadgeBackground => IsParserUpToDate ? "#0D2818" : "#081E36";

    public string StructureBadgeText => IsStructureValid ? "✓ Gültig" : (!IntegrityCheckOk ? "✕ Fehlerhaft" : "⚠️ Unvollständig");
    public string StructureBadgeColor => IsStructureValid ? "#4ADE80" : (!IntegrityCheckOk ? "#F87171" : "#F59E0B");
    public string StructureBadgeBackground => IsStructureValid ? "#0D2818" : (!IntegrityCheckOk ? "#2A0D10" : "#2E1B05");

    public string StructureSummaryText
    {
        get
        {
            if (!IntegrityCheckOk)
                return $"Integritätsprüfung fehlgeschlagen: {IntegrityMessage}";
            if (MissingTables.Count > 0)
                return $"Fehlende Tabellen: {string.Join(", ", MissingTables)}";
            if (MissingColumns.Count > 0)
                return $"Fehlende Spalten: {string.Join(", ", MissingColumns)}";
            if (MissingIndexes.Count > 0)
                return $"Fehlende Indizes: {string.Join(", ", MissingIndexes)}";
            if (!IsSchemaUpToDate)
                return $"Schema veraltet: Installiert ist v{InstalledSchemaVersion}, Zielversion ist v{CurrentSchemaVersion}.";
            if (!IsParserUpToDate)
                return $"Parser-Version weicht ab: Installiert ist v{InstalledParserVersion}, LogParser ist v{CurrentParserVersion} (Re-Scan empfohlen).";
            return $"Alle {ExistingTables.Count} Tabellen und 7 Indizes vorhanden · Physische Integrität fehlerfrei · Schema v{InstalledSchemaVersion}";
        }
    }
}
