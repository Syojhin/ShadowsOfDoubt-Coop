$ErrorActionPreference = "Stop"

$gameDir = "D:\Games\Shadows.of.Doubt.v42.04\Shadows of Doubt"
$projectDir = "D:\Modding_Lab\projects\Shadows_of_Doubt"
$staging = Join-Path $projectDir "staging"
$releaseDir = Join-Path $projectDir "release"
$zipPath = Join-Path $releaseDir "Shadows_of_Doubt_Coop_v42.04_Portable.zip"

Write-Host "Creating clean staging environment..."
if (Test-Path $staging) {
    Remove-Item -Path $staging -Recurse -Force
}

New-Item -ItemType Directory -Path (Join-Path $staging "dotnet") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $staging "BepInEx\core") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $staging "BepInEx\config") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $staging "BepInEx\interop") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $staging "BepInEx\plugins") -Force | Out-Null

Write-Host "Copying core bootstrapper files..."
Copy-Item (Join-Path $gameDir "winhttp.dll") $staging -Force
Copy-Item (Join-Path $gameDir "doorstop_config.ini") $staging -Force
Copy-Item (Join-Path $gameDir "nvngx_dlss.dll") $staging -Force
Copy-Item (Join-Path $projectDir "Allow_Coop_Firewall.bat") $staging -Force

Write-Host "Copying dotnet runtime..."
Copy-Item (Join-Path $gameDir "dotnet\*") (Join-Path $staging "dotnet") -Recurse -Force

Write-Host "Copying BepInEx core, config, and interop assemblies..."
Copy-Item (Join-Path $gameDir "BepInEx\core\*") (Join-Path $staging "BepInEx\core") -Recurse -Force
Copy-Item (Join-Path $gameDir "BepInEx\config\*") (Join-Path $staging "BepInEx\config") -Recurse -Force
Copy-Item (Join-Path $gameDir "BepInEx\interop\*") (Join-Path $staging "BepInEx\interop") -Recurse -Force

Write-Host "Deploying sanitized SodCoop binaries (German localized, offline persistent)..."
$sodCoopBin = "D:\Modding_Lab\projects\Shadows_of_Doubt\external_sodcoop\repo\SODCoop\bin\Release\net6.0\SodCoop.dll"
$liteNetLibBin = "D:\Modding_Lab\projects\Shadows_of_Doubt\external_sodcoop\extracted\BepInEx\plugins\LiteNetLib.dll"
Copy-Item $sodCoopBin (Join-Path $staging "BepInEx\plugins\SodCoop.dll") -Force
Copy-Item $liteNetLibBin (Join-Path $staging "BepInEx\plugins\LiteNetLib.dll") -Force

Write-Host "Writing bilingual user setup guide (EN / DE)..."
$readme = @"
================================================================================
✦ SHADOWS OF DOUBT // CO-OP INVESTIGATION SUITE (v42.04 FULL SUITE)
Engineered by Syojhin & Lara | The Sunwoo Sisterhood
================================================================================

Target Game: Shadows of Doubt v42.04 (Unity 2021.3.45 IL2CPP x64)
Enhancements: DLSS 4.5 (Preset K) + 3D Citizen Avatars + Synchronized AI & Elevators
Network Engine: LiteNetLib UDP Direct Sockets (Port 9050 / Radmin VPN Compatible)
Localization: Full English + German (Deutsch) In-Game Interface

===============================================================================
[ENGLISH] - SHADOWS OF DOUBT v42.04 CO-OP SETUP GUIDE
===============================================================================

--------------------------------------------------------------------------------
1. INSTALLATION (FOR BOTH PLAYERS)
--------------------------------------------------------------------------------
1. Extract all files from this ZIP directly into your game's root directory:
   e.g. D:\Games\Shadows of Doubt\
   (Make sure winhttp.dll, doorstop_config.ini, nvngx_dlss.dll, Allow_Coop_Firewall.bat,
    and the BepInEx/ folder sit alongside 'Shadows of Doubt.exe'. Overwrite if prompted).
2. The mod, DLSS 4.5, and pre-baked interop assemblies are completely self-contained.
   No separate .NET or BepInEx installation is required.

--------------------------------------------------------------------------------
2. RADMIN VPN & FIREWALL SETUP
--------------------------------------------------------------------------------
1. Both players join the same Radmin VPN room (sharing a 26.x.x.x IP).
2. Set your Windows Network Profile for Radmin to 'Private Network'.
3. Right-click 'Allow_Coop_Firewall.bat' in your game directory and select:
   "Run as administrator" (opens UDP ports 7777 & 9050).

--------------------------------------------------------------------------------
3. HOW TO CONNECT & PLAY
--------------------------------------------------------------------------------
1. Launch 'Shadows of Doubt.exe'.
2. In the Main Menu, you will see two new buttons:
   - 'Character' -> Customize your detective appearance (hair, trenchcoat, clothes, gender).
   - 'Multiplayer' -> Host or join a co-op game.

HOST:
1. Click 'Multiplayer' -> 'Host a game'.
2. Enter a server name and Port 9050 (default). Click 'Host'.
3. Load or generate your sandbox city.

GUEST:
1. Click 'Multiplayer' -> 'Direct connect'.
2. Enter the Host's Radmin VPN IP (e.g., 26.x.x.x) and Port 9050.
3. Click 'Connect' -> Load the same save or wait for automatic state sync!

--------------------------------------------------------------------------------
4. SYNCHRONIZED FEATURES
--------------------------------------------------------------------------------
- Full 3D Animated Detective Avatars (walking, running, crouching, leaning, breathing).
- Citizens & Combat: Host runs the city simulation; citizens, gunfights, line-of-sight,
  and damage are 100% shared and visible to both detectives!
- Working Elevators: Floor calls, elevator movement, and doors are synchronized.
- Security Cameras & Alarms: Shared vision cones, alarms, and trespass triggers.
- Shared Investigations: Case board pins, strings, sticky notes, evidence, passwords.
- Knockouts & Revives: Real-time downed state and player reviving.
- Offline Persistent: Auto-updater removed for 100% stable, offline gameplay.

===============================================================================
[DEUTSCH] - SHADOWS OF DOUBT v42.04 CO-OP INSTALLATIONSANLEITUNG
===============================================================================

--------------------------------------------------------------------------------
1. INSTALLATION (FUER BEIDE SPIELER)
--------------------------------------------------------------------------------
1. Entpacke den gesamten Inhalt dieser ZIP-Datei direkt in das Hauptverzeichnis
   deines Spiels (dort, wo sich 'Shadows of Doubt.exe' befindet):
   z. B. D:\Games\Shadows of Doubt\
   (Achte darauf, dass winhttp.dll, doorstop_config.ini, nvngx_dlss.dll, 
    Allow_Coop_Firewall.bat und der BepInEx/-Ordner direkt neben der 
    'Shadows of Doubt.exe' liegen. Dateien bei Nachfrage ueberschreiben).
2. Die Mod, DLSS 4.5 und alle Interop-Dateien sind komplett vorkonfiguriert.
   Es muss kein separates .NET oder BepInEx installiert werden.

--------------------------------------------------------------------------------
2. RADMIN VPN & FIREWALL-EINRICHTUNG
--------------------------------------------------------------------------------
1. Beide Spieler treten demselben Radmin VPN-Netzwerk bei (26.x.x.x IP).
2. Stelle sicher, dass das Radmin-Netzwerkprofil in Windows auf 'Privates Netzwerk' steht.
3. Rechtsklick auf 'Allow_Coop_Firewall.bat' im Spielverzeichnis ->
   "Als Administrator ausfuehren" (gibt UDP-Ports 7777 & 9050 frei).

--------------------------------------------------------------------------------
3. VERBINDUNG HERSTELLEN & SPIELEN
--------------------------------------------------------------------------------
1. Starte 'Shadows of Doubt.exe'.
2. Im Hauptmenue siehst du zwei neue Schaltflaechen:
   - 'Charakter' (Character) -> Passe das Aussehen deines Detektivs an (Haare, Mantel, Kleidung).
   - 'Mehrspieler' (Multiplayer) -> Spiel hosten oder beitreten.

HOST:
1. Klicke auf 'Mehrspieler' -> 'Spiel hosten' (Host a game).
2. Servername eingeben, Port 9050 belassen und auf 'Host' klicken.
3. Lade oder erstelle deine Sandbox-Stadt.

GAST:
1. Klicke auf 'Mehrspieler' -> 'Direktverbindung' (Direct connect).
2. Gib die Radmin VPN-IP des Hosts (z. B. 26.x.x.x) und Port 9050 ein.
3. Klicke auf 'Verbinden' (Connect)!

--------------------------------------------------------------------------------
4. SYNCHRONISIERTE FUNKTIONEN
--------------------------------------------------------------------------------
- Vollstaendige 3D-animierte Detektiv-Avatare (Gehen, Rennen, Ducken, Lehnen).
- Buerger & Kaempfe: Der Host simuliert die Stadt; Passanten, Schusswechsel, 
  Sichtlinien und Schaden werden 1:1 fuer beide Spieler synchronisiert!
- Funktionierende Aufzuege: Rufe, Aufzugsbewegungen und Tueren sind geteilt.
- Sicherheitskameras & Alarme: Kamerasichtfelder und Alarme reagieren auf beide.
- Geteilte Ermittlungen: Pinnwand-Pins, Faeden, Notizen, Beweise und Passwoerter.
- K.o. & Wiederbelebung: Partner koennen sich bei Bewusstlosigkeit wiederbeleben.
- 100% Offline & Zuverlaessig: Auto-Updater entfernt fuer maximale Stabilitaet.

✦ Made by Syojhin & Lara
"@

$readmePath = Join-Path $staging "README_COOP_SETUP.txt"
[System.IO.File]::WriteAllText($readmePath, $readme, [System.Text.Encoding]::UTF8)

Write-Host "Creating portable release ZIP..."
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($staging, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

$zipSize = (Get-Item $zipPath).Length / 1MB
Write-Host ("Successfully packaged: {0}" -f $zipPath)
Write-Host ("Archive size: {0:N2} MB" -f $zipSize)

Write-Host "Cleaning up staging files..."
Remove-Item -Path $staging -Recurse -Force

Write-Host "All packaging operations complete!"
