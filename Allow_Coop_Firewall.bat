@echo off
netsh advfirewall firewall delete rule name="Shadows of Doubt Co-op UDP 7777" >nul 2>&1
netsh advfirewall firewall add rule name="Shadows of Doubt Co-op UDP 7777" dir=in action=allow protocol=UDP localport=7777
netsh advfirewall firewall add rule name="Shadows of Doubt Co-op UDP 7777" dir=out action=allow protocol=UDP localport=7777
echo [OK] UDP Port 7777 opened for Shadows of Doubt Co-op! / UDP-Port 7777 erfolgreich freigegeben!
pause
