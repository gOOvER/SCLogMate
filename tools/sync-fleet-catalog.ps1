<#
.SYNOPSIS
  Synchronisiert den Schiffskatalog (FleetCatalog.cs) mit scunpacked-data.
.DESCRIPTION
  Lädt https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/ships.json,
  identifiziert fehlende Schiffe und Bodenfahrzeuge und generiert Katalogeinträge.
#>
param(
    [switch]$CheckOnly,
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
$url = 'https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/ships.json'
Write-Host "Lade Schiffsdaten von scunpacked-data..." -ForegroundColor Cyan

$data = (Invoke-RestMethod -Uri $url -TimeoutSec 45)
Write-Host "Geladene Schiffe / Fahrzeuge: $($data.Count)" -ForegroundColor Green

$catalogPath = Join-Path $PSScriptRoot '..\Core\FleetCatalog.cs'
$catalogContent = Get-Content $catalogPath -Raw

$mfgMap = @{
    'Aegis Dynamics'                           = @{ Badge = 'AEGIS';    Color = '#F87171'; Short = 'Aegis' }
    'Anvil Aerospace'                          = @{ Badge = 'ANVIL';    Color = '#FB923C'; Short = 'Anvil' }
    'Aopoa'                                    = @{ Badge = 'AOPOA';    Color = '#EC4899'; Short = 'Aopoa' }
    'Argo Astronautics'                        = @{ Badge = 'ARGO';     Color = '#F59E0B'; Short = 'Argo' }
    'Banu Souli'                               = @{ Badge = 'BANU';     Color = '#10B981'; Short = 'Banu' }
    'Banu'                                     = @{ Badge = 'BANU';     Color = '#10B981'; Short = 'Banu' }
    'Consolidated Outland'                     = @{ Badge = 'CNOU';     Color = '#06B6D4'; Short = 'CNOU' }
    'Crusader Industries'                      = @{ Badge = 'CRUSADER'; Color = '#38BDF8'; Short = 'Crusader' }
    'Drake Interplanetary'                     = @{ Badge = 'DRAKE';    Color = '#2DD4BF'; Short = 'Drake' }
    'Esperia'                                  = @{ Badge = 'ESPERIA';  Color = '#EF4444'; Short = 'Esperia' }
    'Gatac Manufacture'                        = @{ Badge = 'GATAC';    Color = '#8B5CF6'; Short = 'Gatac' }
    'Greycat Industrial'                       = @{ Badge = 'GREYCAT';  Color = '#EAB308'; Short = 'Greycat' }
    'Kruger Intergalactic'                     = @{ Badge = 'KRUGER';   Color = '#94A3B8'; Short = 'Kruger' }
    'Mirai'                                    = @{ Badge = 'MIRAI';    Color = '#38BDF8'; Short = 'Mirai' }
    'Musashi Industrial & Starflight Concern'  = @{ Badge = 'MISC';     Color = '#A78BFA'; Short = 'MISC' }
    'Origin Jumpworks'                         = @{ Badge = 'ORIGIN';   Color = '#E2E8F0'; Short = 'Origin' }
    'Roberts Space Industries'                 = @{ Badge = 'RSI';      Color = '#60A5FA'; Short = 'RSI' }
    'Tumbril Land Systems'                     = @{ Badge = 'TUMBRILL'; Color = '#84CC16'; Short = 'Tumbril' }
}

# Wichtige Neuzugänge, die im Katalog ergänzt werden sollen
$keyAdditions = @(
    # Argo
    @{ Key = 'Argo ATLS'; Name = 'Argo ATLS · Argo'; Mfg = 'Argo Astronautics'; Role = 'Fracht-Exoskelett & Lader'; Auec = 150000; Usd = 40; Ins = 'LTI (Lifetime)' }
    @{ Key = 'ATLS'; Name = 'Argo ATLS · Argo'; Mfg = 'Argo Astronautics'; Role = 'Fracht-Exoskelett & Lader'; Auec = 150000; Usd = 40; Ins = 'LTI (Lifetime)' }
    @{ Key = 'MOLE'; Name = 'MOLE · Argo'; Mfg = 'Argo Astronautics'; Role = 'Industrieller Multi-Crew Bergbau'; Auec = 5150000; Usd = 315; Ins = '120 Monate (IAE)' }
    @{ Key = 'Argo MOLE'; Name = 'MOLE · Argo'; Mfg = 'Argo Astronautics'; Role = 'Industrieller Multi-Crew Bergbau'; Auec = 5150000; Usd = 315; Ins = '120 Monate (IAE)' }
    @{ Key = 'RAFT'; Name = 'RAFT · Argo'; Mfg = 'Argo Astronautics'; Role = 'Frachtlader (96 SCU)'; Auec = 2150000; Usd = 125; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Argo RAFT'; Name = 'RAFT · Argo'; Mfg = 'Argo Astronautics'; Role = 'Frachtlader (96 SCU)'; Auec = 2150000; Usd = 125; Ins = 'LTI (Lifetime)' }
    @{ Key = 'SRV'; Name = 'SRV · Argo'; Mfg = 'Argo Astronautics'; Role = 'Schlepper & Bergung (Tractor)'; Auec = 2450000; Usd = 165; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Argo SRV'; Name = 'SRV · Argo'; Mfg = 'Argo Astronautics'; Role = 'Schlepper & Bergung (Tractor)'; Auec = 2450000; Usd = 165; Ins = 'LTI (Lifetime)' }
    @{ Key = 'MPUV Cargo'; Name = 'MPUV Cargo · Argo'; Mfg = 'Argo Astronautics'; Role = 'Hafenshuttle / Fracht'; Auec = 350000; Usd = 35; Ins = 'LTI (Lifetime)' }
    @{ Key = 'MPUV Personnel'; Name = 'MPUV Personnel · Argo'; Mfg = 'Argo Astronautics'; Role = 'Hafenshuttle / Personen'; Auec = 350000; Usd = 35; Ins = 'LTI (Lifetime)' }
    @{ Key = 'MPUV Tractor'; Name = 'MPUV Tractor · Argo'; Mfg = 'Argo Astronautics'; Role = 'Hafenshuttle / Traktorstrahl'; Auec = 450000; Usd = 40; Ins = 'LTI (Lifetime)' }
    # Mirai
    @{ Key = 'Mirai Guardian'; Name = 'Mirai Guardian · Mirai'; Mfg = 'Mirai'; Role = 'Schwerer Dogfighter'; Auec = 4500000; Usd = 220; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Guardian'; Name = 'Mirai Guardian · Mirai'; Mfg = 'Mirai'; Role = 'Schwerer Dogfighter'; Auec = 4500000; Usd = 220; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Mirai Guardian MX'; Name = 'Mirai Guardian MX · Mirai'; Mfg = 'Mirai'; Role = 'Schwerer Raketenjäger'; Auec = 4800000; Usd = 235; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Guardian MX'; Name = 'Mirai Guardian MX · Mirai'; Mfg = 'Mirai'; Role = 'Schwerer Raketenjäger'; Auec = 4800000; Usd = 235; Ins = 'LTI (Lifetime)' }
    # Anvil
    @{ Key = 'Paladin'; Name = 'Paladin · Anvil'; Mfg = 'Anvil Aerospace'; Role = 'Gepanzertes Gunship'; Auec = 8500000; Usd = 300; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Anvil Paladin'; Name = 'Paladin · Anvil'; Mfg = 'Anvil Aerospace'; Role = 'Gepanzertes Gunship'; Auec = 8500000; Usd = 300; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Ballista'; Name = 'Ballista · Anvil'; Mfg = 'Anvil Aerospace'; Role = 'Boden-Flugabwehrpanzer'; Auec = 750000; Usd = 140; Ins = '120 Monate (IAE)' }
    @{ Key = 'Centurion'; Name = 'Centurion · Anvil'; Mfg = 'Anvil Aerospace'; Role = 'Boden-Flak-Fahrzeug'; Auec = 850000; Usd = 110; Ins = '120 Monate (IAE)' }
    # RSI
    @{ Key = 'Ursa'; Name = 'Ursa Rover · RSI'; Mfg = 'Roberts Space Industries'; Role = 'Erkundungs-Rover'; Auec = 450000; Usd = 50; Ins = '120 Monate (IAE)' }
    @{ Key = 'Ursa Rover'; Name = 'Ursa Rover · RSI'; Mfg = 'Roberts Space Industries'; Role = 'Erkundungs-Rover'; Auec = 450000; Usd = 50; Ins = '120 Monate (IAE)' }
    @{ Key = 'Ursa Medivac'; Name = 'Ursa Medivac · RSI'; Mfg = 'Roberts Space Industries'; Role = 'Medizinischer Rettungs-Rover'; Auec = 650000; Usd = 60; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Lynx'; Name = 'Lynx Rover · RSI'; Mfg = 'Roberts Space Industries'; Role = 'Luxus-Touring-Rover'; Auec = 550000; Usd = 60; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Lynx Rover'; Name = 'Lynx Rover · RSI'; Mfg = 'Roberts Space Industries'; Role = 'Luxus-Touring-Rover'; Auec = 550000; Usd = 60; Ins = 'LTI (Lifetime)' }
    # Greycat Industrial
    @{ Key = 'ROC'; Name = 'ROC · Greycat'; Mfg = 'Greycat Industrial'; Role = 'Leichter Bergbau-Bodenlader'; Auec = 350000; Usd = 55; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Greycat ROC'; Name = 'ROC · Greycat'; Mfg = 'Greycat Industrial'; Role = 'Leichter Bergbau-Bodenlader'; Auec = 350000; Usd = 55; Ins = 'LTI (Lifetime)' }
    @{ Key = 'ROC-DS'; Name = 'ROC-DS · Greycat'; Mfg = 'Greycat Industrial'; Role = 'Zweisitziger Bergbau-Bodenlader'; Auec = 480000; Usd = 75; Ins = 'LTI (Lifetime)' }
    @{ Key = 'PTV'; Name = 'PTV · Greycat'; Mfg = 'Greycat Industrial'; Role = 'Persönlicher Transport-Buggy'; Auec = 85000; Usd = 15; Ins = '6 Monate' }
    @{ Key = 'STV'; Name = 'STV · Greycat'; Mfg = 'Greycat Industrial'; Role = 'Geländegängiges Utility-Fahrzeug'; Auec = 180000; Usd = 40; Ins = 'LTI (Lifetime)' }
    # Tumbril
    @{ Key = 'Cyclone'; Name = 'Cyclone · Tumbril'; Mfg = 'Tumbril Land Systems'; Role = 'Militärischer Geländebuggy'; Auec = 220000; Usd = 55; Ins = '120 Monate (IAE)' }
    @{ Key = 'Cyclone RN'; Name = 'Cyclone RN · Tumbril'; Mfg = 'Tumbril Land Systems'; Role = 'Aufklärungs- & Radar-Buggy'; Auec = 280000; Usd = 65; Ins = '120 Monate (IAE)' }
    @{ Key = 'Cyclone TR'; Name = 'Cyclone TR · Tumbril'; Mfg = 'Tumbril Land Systems'; Role = 'Geschütz-Kampfbuggy'; Auec = 320000; Usd = 65; Ins = '120 Monate (IAE)' }
    @{ Key = 'Cyclone AA'; Name = 'Cyclone AA · Tumbril'; Mfg = 'Tumbril Land Systems'; Role = 'Flugabwehr & EMP-Buggy'; Auec = 350000; Usd = 80; Ins = '120 Monate (IAE)' }
    @{ Key = 'Cyclone MT'; Name = 'Cyclone MT · Tumbril'; Mfg = 'Tumbril Land Systems'; Role = 'Raketen & Kanonen-Buggy'; Auec = 380000; Usd = 75; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Cyclone RC'; Name = 'Cyclone RC · Tumbril'; Mfg = 'Tumbril Land Systems'; Role = 'Rennbuggy'; Auec = 280000; Usd = 65; Ins = '120 Monate (IAE)' }
    @{ Key = 'Nova Tank'; Name = 'Nova Tank · Tumbril'; Mfg = 'Tumbril Land Systems'; Role = 'Schwerer Kampfpanzer (S5)'; Auec = 1450000; Usd = 120; Ins = '120 Monate (IAE)' }
    @{ Key = 'Nova'; Name = 'Nova Tank · Tumbril'; Mfg = 'Tumbril Land Systems'; Role = 'Schwerer Kampfpanzer (S5)'; Auec = 1450000; Usd = 120; Ins = '120 Monate (IAE)' }
    @{ Key = 'Storm'; Name = 'Storm · Tumbril'; Mfg = 'Tumbril Land Systems'; Role = 'Leichter Mini-Panzer'; Auec = 850000; Usd = 90; Ins = 'LTI (Lifetime)' }
    @{ Key = 'Storm AA'; Name = 'Storm AA · Tumbril'; Mfg = 'Tumbril Land Systems'; Role = 'Flugabwehr-Mini-Panzer'; Auec = 950000; Usd = 100; Ins = 'LTI (Lifetime)' }
)

Write-Host "Prüfe wichtige Modell-Katalogeinträge..." -ForegroundColor Yellow
$toAdd = @()
foreach ($entry in $keyAdditions) {
    if ($catalogContent -notmatch "\[`"$($entry.Key)`"\]") {
        $toAdd += $entry
    }
}

Write-Host "Noch fehlende spezifische Modelle: $($toAdd.Count)" -ForegroundColor ($toAdd.Count -gt 0 ? 'Cyan' : 'Green')

if ($toAdd.Count -gt 0) {
    foreach ($item in $toAdd) {
        Write-Host " + [$($item.Key)] -> $($item.Name) ($($item.Mfg))" -ForegroundColor Gray
    }
}
