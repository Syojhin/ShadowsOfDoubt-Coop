@echo off
netsh advfirewall firewall delete rule name="Shadows of Doubt Co-op UDP" >nul 2>&1
netsh advfirewall firewall add rule name="Shadows of Doubt Co-op UDP" dir=in action=allow protocol=UDP localport=7777,9050
netsh advfirewall firewall add rule name="Shadows of Doubt Co-op UDP" dir=out action=allow protocol=UDP localport=7777,9050
echo [OK] UDP Ports 7777 and 9050 opened for Shadows of Doubt Co-op!
echo [OK] UDP-Ports 7777 und 9050 erfolgreich freigegeben!
pause
