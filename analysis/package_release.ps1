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
New-Item -ItemType Directory -Path (Join-Path $staging "BepInEx\plugins\ShadowsOfDoubtCoop") -Force | Out-Null

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

Write-Host "Deploying compiled mod binaries (preserving verified hash CE32E4893B...)..."
$binDir = Join-Path $projectDir "bin"
Copy-Item (Join-Path $binDir "ShadowsOfDoubtCoop.dll") (Join-Path $staging "BepInEx\plugins\ShadowsOfDoubtCoop") -Force
Copy-Item (Join-Path $binDir "LiteNetLib.dll") (Join-Path $staging "BepInEx\plugins\ShadowsOfDoubtCoop") -Force

Write-Host "Writing bilingual user setup guide (EN / DE)..."
$readme = @"
================================================================================
✦ SHADOWS OF DOUBT // CO-OP INVESTIGATION SUITE (v42.04 PORTABLE)
Engineered by Syojhin & Lara | The Sunwoo Sisterhood
================================================================================

Target Game: Shadows of Doubt v42.04 (Unity 2021.3.45 IL2CPP x64)
Enhancements: DLSS 4.5 (310.9.16.0) Preset K + LiteNetLib UDP Multiplayer

================================================================================
[ENGLISH] - SHADOWS OF DOUBT v42.04 CO-OP SETUP GUIDE
================================================================================

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
1. Download and install Radmin VPN (free, zero router port-forwarding needed).
2. Host creates a private Radmin network (e.g., 'SodInvestigation').
3. Guest joins that Radmin network so both players share a 26.x.x.x virtual subnet.
4. Ensure your Windows Network Profile for Radmin is set to 'Private Network':
   - Windows Settings -> Network & Internet -> Advanced -> Network Adapters -> Radmin VPN
   - Or open PowerShell as Administrator and run:
     Set-NetConnectionProfile -InterfaceAlias "Radmin VPN" -NetworkCategory Private
5. FIREWALL RULE (HOST ONLY):
   - Right-click 'Allow_Coop_Firewall.bat' in your game directory and select:
     "Run as administrator" (opens UDP port 7777).

--------------------------------------------------------------------------------
3. HOW TO CONNECT & PLAY
--------------------------------------------------------------------------------
HOST (Detective 1):
1. Launch 'Shadows of Doubt.exe' and load your save game or generate a new city.
2. In-game, press [F9] on your keyboard to open the Obsidian/Gold Co-op HUD.
3. Click 'HOST INVESTIGATION (Port 7777)'.
4. Once your partner connects, click 'SYNC CURRENT SAVE TO PARTNER'.
   - This streams your current .sodb save file directly to your partner so you 
     both inhabit the exact same city geometry, seed, and case state.

GUEST (Detective 2):
1. Launch 'Shadows of Doubt.exe' to the main menu.
2. Press [F9] to open the Co-op HUD.
3. Enter the Host's Radmin VPN IP address in the 'Host IP:' box (e.g., 26.x.x.x).
4. Click 'JOIN INVESTIGATION'.
5. Once connected, wait for the Host to click 'SYNC CURRENT SAVE TO PARTNER'.
6. When the transfer completes ('Save received! Ready to load coop_...'), go to 
   Load Game in the main menu and select the received save!

--------------------------------------------------------------------------------
4. SYNCHRONIZED FEATURES & KNOWN V1 BOUNDARIES
--------------------------------------------------------------------------------
- Real-time 30Hz Player Avatar: 3D detective body with dynamic incandescent flashlight.
- 1Hz Master Simulation Clock: Synchronized game time and weather cycles.
- Doors & Locks: Physical door opening angles and key lock states.
- Case Board: Pins added or removed from cork boards replicate between detectives.
- Crime Authority: Host runs the authoritative murder simulation; guest autonomous 
  murder generation is suppressed to keep investigations unified.
- Gameplay Pause: Simulation keeps running when opening notebooks/menus in co-op.
- Citizen Simulation: In v1, citizen NPC schedules and physics simulate locally on
  each client (same city seed = identical city layout, but minor pedestrian path drift 
  can occur).

Press [F9] at any time during gameplay to toggle the HUD.
Enjoy solving crimes together in the rain.

✦ Made by Syojhin & Lara

================================================================================
[DEUTSCH] - SHADOWS OF DOUBT v42.04 CO-OP INSTALLATIONSANLEITUNG
================================================================================

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
1. Lade Radmin VPN herunter und installiere es (kostenlos, keine Portfreigabe im Router noetig).
2. Der Host erstellt ein privates Radmin-Netzwerk (z. B. 'SodErmittlung').
3. Der Gast tritt diesem Netzwerk bei, sodass beide Spieler im 26.x.x.x-Subnetz sind.
4. Stelle sicher, dass das Radmin-Netzwerkprofil in Windows auf 'Privates Netzwerk' steht:
   - Windows-Einstellungen -> Netzwerk und Internet -> Erweiterte Netzwerkeinstellungen -> Radmin VPN
   - Oder in PowerShell als Administrator ausfuehren:
     Set-NetConnectionProfile -InterfaceAlias "Radmin VPN" -NetworkCategory Private
5. FIREWALL-FREIGABE (NUR FUER DEN HOST ERFORDERLICH):
   - Klicke mit der rechten Maustaste auf die Datei 'Allow_Coop_Firewall.bat' im
     Spielverzeichnis und waehle: "Als Administrator ausfuehren" (oeffnet UDP-Port 7777).

--------------------------------------------------------------------------------
3. VERBINDUNG HERSTELLEN & GEMEINSAM SPIELEN
--------------------------------------------------------------------------------
HOST (Detektiv 1):
1. Starte 'Shadows of Doubt.exe' und lade deinen Spielstand oder generiere eine Stadt.
2. Druecke im Spiel die Taste [F9], um das edle Obsidian/Gold-Koop-Menue zu oeffnen.
3. Klicke auf 'HOST INVESTIGATION (Port 7777)'.
4. Sobald dein Partner verbunden ist, klicke auf 'SYNC CURRENT SAVE TO PARTNER'.
   - Dadurch wird dein aktueller Spielstand (.sodb) direkt an deinen Partner gestreamt,
     sodass ihr in exakt derselben Stadt mit identischem Fallfortschritt spielt.

GAST (Detektiv 2):
1. Starte 'Shadows of Doubt.exe' bis zum Hauptmenue.
2. Druecke die Taste [F9], um das Koop-Menue zu oeffnen.
3. Trage die Radmin VPN-IP des Hosts in das Feld 'Host IP:' ein (z. B. 26.x.x.x).
4. Klicke auf 'JOIN INVESTIGATION'.
5. Sobald die Verbindung steht, klickt der Host auf 'SYNC CURRENT SAVE TO PARTNER'.
6. Wenn der Transfer abgeschlossen ist ('Save received! Ready to load coop_...'),
   gehe im Hauptmenue auf 'Spiel laden' (Load Game) und waehle den empfangenen Spielstand!

--------------------------------------------------------------------------------
4. SYNCHRONISIERTE FUNKTIONEN & BEKANNTE V1-LIMITIERUNGEN
--------------------------------------------------------------------------------
- Echtzeit-3D-Detektiv-Avatar (30Hz): Sichtbarer Partner-Koerper mit Taschenlampe.
- Master-Simulationsuhr (1Hz): Synchronisierte Spielzeit und Wetterzyklen.
- Tueren & Schloesser: Tueroeffnungswinkel und Schliesszustaende werden live synchronisiert.
- Pinnwand (Case Board): Hinweise, die an die Pinnwand geheftet oder entfernt werden,
  erscheinen synchron bei beiden Detektiven.
- Mord-Autoritaet: Der Host steuert die Mordsimulation autoritativ; autonome Morde auf
  dem Gast-Client werden unterdrueckt, um Verzweigungen zu verhindern.
- Kein Pausieren: Das Oeffnen von Notizbuechern/Menues friert das Spiel im Koop nicht ein.
- Buerger-Simulation: In Version 1 wird die Wegfindung der Buerger lokal berechnet
  (gleiche Stadt = gleiches Layout, leichte Abweichungen bei Passanten moeglich).

Druecke [F9] jederzeit im Spiel, um das Menue ein- oder auszublenden.
Viel Erfolg bei den Ermittlungen im Regen.

✦ Made by Syojhin & Lara
================================================================================
"@

$readmePath = Join-Path $staging "README_COOP_SETUP.txt"
[System.IO.File]::WriteAllText($readmePath, $readme, [System.Text.Encoding]::UTF8)

# Also update project root README
Copy-Item $readmePath (Join-Path $projectDir "README_COOP_SETUP.txt") -Force

Write-Host "Creating portable release ZIP..."
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($staging, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

$zipInfo = Get-Item $zipPath
Write-Host "Successfully packaged: $($zipInfo.FullName)"
Write-Host "Archive size: $([math]::Round($zipInfo.Length / 1MB, 2)) MB"

Write-Host "Cleaning up staging files..."
Remove-Item -Path $staging -Recurse -Force
Write-Host "All packaging operations complete!"
