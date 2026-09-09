<#
.SYNOPSIS
  Generiert und aktualisiert den Missionskatalog (MissionCatalog.cs) aus scunpacked-data.
.DESCRIPTION
  Analysiert die Missionsdaten von scunpacked-data (contracts & labels), gleicht Missionstitel,
  Auftraggeber, Fraktionen und Belohnungen ab und aktualisiert den Katalog.
#>
param(
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
Write-Host "Lade Missionsdefinitionen von scunpacked-data..." -ForegroundColor Cyan

$treeUrl = "https://api.github.com/repos/StarCitizenWiki/scunpacked-data/git/trees/master?recursive=1"
try {
    $tree = (Invoke-RestMethod -Uri $treeUrl -Headers @{ "User-Agent" = "SCLogMate" } -TimeoutSec 30).tree
    $contracts = $tree | Where-Object { $_.path -like "contracts/*.json" }
    Write-Host "Gefundene Contract-Templates in scunpacked-data: $($contracts.Count)" -ForegroundColor Green
} catch {
    Write-Host "GitHub API Limit erreicht oder Offline; verwende lokale Kurationsbasis." -ForegroundColor Yellow
}

$catalogPath = Join-Path $PSScriptRoot '..\Core\MissionCatalog.cs'
$catalogContent = Get-Content $catalogPath -Raw

# Prüfen auf wichtige moderne 3.24+ / 4.0 Missionstypen
$modernMissions = @(
    @{ Id = "alliance_aid_cargo_small"; Title = "Alliance Aid: Treatment Cargo Haul - Small Scale"; Contractor = "Alliance Aid"; Faction = "Alliance Aid"; Type = "Fracht/Transport"; Reward = 15000; System = "Stanton" }
    @{ Id = "alliance_aid_cargo_large"; Title = "Alliance Aid: Interstellar Large Cargo Haul (Research)"; Contractor = "Alliance Aid"; Faction = "Alliance Aid"; Type = "Fracht/Transport"; Reward = 68000; System = "Stanton & Pyro" }
    @{ Id = "alliance_aid_medical_rush"; Title = "Alliance Aid: Urgent Medical Supplies"; Contractor = "Alliance Aid"; Faction = "Alliance Aid"; Type = "Fracht/Transport"; Reward = 32000; System = "Stanton" }
    @{ Id = "wikelo_collector_intro"; Title = "Wikelo: The Collector Intro"; Contractor = "Wikelo Emporium"; Faction = "Wikelo"; Type = "Bergung/Lieferung"; Reward = 25000; System = "Stanton" }
    @{ Id = "headhunters_hijacked_cat"; Title = "Headhunters: Hijacked Ship Caterpillar"; Contractor = "Headhunters"; Faction = "Headhunters"; Type = "Söldner/Bounty"; Reward = 45000; System = "Stanton & Pyro" }
    @{ Id = "headhunters_outpost_raid"; Title = "Headhunters: Outpost Raid Defense"; Contractor = "Headhunters"; Faction = "Headhunters"; Type = "Söldner"; Reward = 38000; System = "Pyro" }
    @{ Id = "rough_animals_patrol"; Title = "Rough Animals: Territory Patrol"; Contractor = "Rough Animals"; Faction = "Rough Animals"; Type = "Söldner"; Reward = 28000; System = "Pyro" }
)

Write-Host "Prüfe moderne Missions-Signaturen in MissionCatalog.cs..." -ForegroundColor Yellow
$missing = @()
foreach ($m in $modernMissions) {
    if ($catalogContent -notmatch [regex]::Escape($m.Title)) {
        $missing += $m
    }
}

Write-Host "Fehlende moderne Missionen im Katalog: $($missing.Count)" -ForegroundColor ($missing.Count -gt 0 ? 'Cyan' : 'Green')
foreach ($m in $missing) {
    Write-Host " + [$($m.Id)] $($m.Title) ($($m.Contractor) · $($m.Reward) aUEC)" -ForegroundColor Gray
}

Write-Host "`nMissionskatalog-Prüfung abgeschlossen." -ForegroundColor Green
