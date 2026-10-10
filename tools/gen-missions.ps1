<#
.SYNOPSIS
  Generiert und aktualisiert den Missionskatalog (Data/missions.json) aus scunpacked-data / StarCitizenWiki API.
.DESCRIPTION
  Lädt alle extrahierten Missionsdaten von der StarCitizenWiki API (basierend auf scunpacked-data),
  kombiniert sie mit den lokalen global.ini Event-Strings (z. B. RSI Discovery Month) und
  aktualisiert die eingebettete Ressource Data/missions.json für SCLogMate.
#>
param(
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
$missionsPath = Join-Path $PSScriptRoot '..\Data\missions.json'

if ($CheckOnly) {
    if (Test-Path $missionsPath) {
        $data = Get-Content $missionsPath -Raw | ConvertFrom-Json
        Write-Host "Missionskatalog-Status:" -ForegroundColor Cyan
        Write-Host "  Datei: $missionsPath"
        Write-Host "  Gespeicherte Missionen: $($data.Count)"
        $withReward = ($data | Where-Object { $_.BaseReward -gt 0 }).Count
        Write-Host "  Missionen mit fester Belohnung: $withReward"
    } else {
        Write-Host "Data/missions.json existiert nicht." -ForegroundColor Yellow
    }
    return
}

Write-Host "Lade Missionsdaten von StarCitizenWiki API (scunpacked-data)..." -ForegroundColor Cyan

$all = @()
$existingTitles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

# 1. Alle Seiten der StarCitizenWiki API abrufen
for ($p = 1; $p -le 25; $p++) {
    try {
        $res = Invoke-RestMethod -Uri "https://api.star-citizen.wiki/api/v2/missions?page[size]=100&page[number]=$p" -Headers @{ 'User-Agent' = 'SCLogMate' } -TimeoutSec 20
        if (-not $res.data -or $res.data.Count -eq 0) { break }
        foreach ($m in $res.data) {
            if (-not $m.title) { continue }
            $reward = 0
            if ($m.reward_min -and $m.reward_min -gt 0) { $reward = [int]$m.reward_min }
            elseif ($m.reward_max -and $m.reward_max -gt 0) { $reward = [int]$m.reward_max }

            $contractor = 'Unbekannt'
            if ($m.mission_giver) { $contractor = $m.mission_giver }
            elseif ($m.faction -and $m.faction.name) { $contractor = $m.faction.name }

            $faction = if ($m.faction -and $m.faction.name) { $m.faction.name } else { $contractor }
            $type = if ($m.mission_type) { $m.mission_type } else { 'Auftrag' }
            $systems = if ($m.star_systems) { $m.star_systems -join ', ' } else { 'Stanton' }

            $rep = 0
            if ($m.reputation_amount) { $rep = [int]$m.reputation_amount }

            if ($existingTitles.Add($m.title)) {
                $all += [PSCustomObject]@{
                    Id = $m.uuid
                    Title = $m.title
                    Contractor = $contractor
                    Faction = $faction
                    MissionType = $type
                    BaseReward = $reward
                    ContractFee = 0
                    ReputationGain = $rep
                    IsIllegal = [bool]$m.illegal
                    StarSystems = $systems
                    Blueprints = @()
                    Description = if ($m.description) { $m.description } else { '' }
                }
            }
        }
        if ($res.meta.current_page -ge $res.meta.last_page) { break }
    } catch {
        Write-Host "Fehler oder Ende beim Laden von Seite $p: $($_.Exception.Message)" -ForegroundColor Yellow
        break
    }
}

Write-Host "Geladene Missionen aus StarCitizenWiki API: $($all.Count)" -ForegroundColor Green

# 2. Lokale Event-Missionsdefinitionen aus global.ini abgleichen (z.B. RSI Discovery Month)
$iniPath = 'J:\StarCitizen\LIVE\data\Localization\english\global.ini'
if (Test-Path $iniPath) {
    Write-Host "Gleiche moderne Event-Strings aus global.ini ab..." -ForegroundColor Cyan
    $lines = Get-Content $iniPath
    $kv = @{}
    foreach ($l in $lines) {
        if ($l -match '^([a-zA-Z0-9_]+)(?:,P)?=(.*)$') {
            $kv[$Matches[1]] = $Matches[2]
        }
    }

    $eventCount = 0
    foreach ($k in $kv.Keys) {
        if ($k -like 'iasi_*_title*' -or $k -like 'iasi_*_Title*') {
            $title = $kv[$k].Trim()
            if (-not $title -or -not $existingTitles.Add($title)) { continue }

            $baseKey = $k -replace '(?i)_title.*$', ''
            $desc = ''
            foreach ($dk in $kv.Keys) {
                if ($dk -like ($baseKey + '_desc*') -or $dk -like ($baseKey + '_Desc*')) {
                    $desc = $kv[$dk]
                    break
                }
            }

            $contractor = 'Roberts Space Industries'
            $type = 'Event'
            if ($title -like '*Defend*' -or $title -like '*Attack*' -or $title -like '*Support Ship*' -or $title -like '*Patrol*' -or $title -like '*Threats*' -or $title -like '*Sweep*') {
                $contractor = 'Foxwell Enforcement'
                $type = 'Söldner'
            } elseif ($title -like '*Refuel*') {
                $contractor = 'United Wayfarers Club'
                $type = 'Service'
            } elseif ($title -like '*UCM Order*' -or $title -like '*Salvage*') {
                $contractor = 'Adagio Holdings'
                $type = 'Bergung & Salvage'
            } elseif ($title -like '*Courier*' -or $title -like '*Delivery*') {
                $contractor = 'Roberts Space Industries'
                $type = 'Lieferung'
            } elseif ($title -like '*Haul*') {
                $contractor = 'Roberts Space Industries'
                $type = 'Fracht/Transport'
            } elseif ($title -like '*Procure*' -or $title -like '*Mining*') {
                $contractor = 'Roberts Space Industries'
                $type = 'Bergbau'
            } elseif ($title -like '*Outlaw*' -or $title -like '*Gang Leader*') {
                $contractor = 'Northrock Service Group'
                $type = 'Kopfgeld'
            } elseif ($title -like '*Clear Hostiles*') {
                $contractor = 'Northrock Service Group'
                $type = 'Söldner'
            }

            $all += [PSCustomObject]@{
                Id = 'iasi_' + $baseKey
                Title = $title
                Contractor = $contractor
                Faction = $contractor
                MissionType = $type
                BaseReward = 0
                ContractFee = 0
                ReputationGain = 100
                IsIllegal = $false
                StarSystems = 'Stanton'
                Blueprints = @()
                Description = $desc
            }
            $eventCount++
        }
    }
    Write-Host "Hinzugefügte Event-Missionen: $eventCount" -ForegroundColor Green
}

$json = $all | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText($missionsPath, $json, [System.Text.Encoding]::UTF8)
Write-Host "Data/missions.json erfolgreich aktualisiert ($($all.Count) Einträge)." -ForegroundColor Green
