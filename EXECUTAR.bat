@echo off
title AfixRFID Etiquetas - NAO FECHE ESTA JANELA
echo ===================================================
echo    AfixRFID Etiquetas
echo ---------------------------------------------------
echo    Iniciando o programa... aguarde alguns segundos.
echo    O navegador vai abrir sozinho.
echo.
echo    NAO FECHE esta janela enquanto estiver usando!
echo    Para encerrar: feche esta janela ou aperte Ctrl+C.
echo ===================================================
echo.

set ASPNETCORE_URLS=http://localhost:5050

REM Abre o navegador depois de ~8 segundos (em paralelo), dando tempo do servidor subir
start "" cmd /c "ping -n 9 127.0.0.1 >nul & start http://localhost:5050"

REM Inicia o servidor (a pasta do .bat e usada automaticamente)
dotnet run -c Release --project "%~dp0RfidEtiquetas.csproj"

pause
