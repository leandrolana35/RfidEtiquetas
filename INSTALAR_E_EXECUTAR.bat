@echo off
echo ============================================
echo  AfixRFID Etiquetas v2.0 - Instalacao
echo ============================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERRO: .NET SDK nao encontrado.
    echo.
    echo Por favor instale o .NET 8 SDK em:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    echo Baixe o "SDK" (nao apenas o Runtime).
    pause
    exit /b 1
)

echo Restaurando pacotes NuGet...
dotnet restore
if errorlevel 1 goto erro

echo.
echo Compilando...
dotnet build -c Release
if errorlevel 1 goto erro

echo.
echo ============================================
echo  Iniciando aplicacao em http://localhost:5050
echo  Pressione Ctrl+C para encerrar.
echo ============================================
echo.
dotnet run -c Release
goto fim

:erro
echo.
echo ERRO na compilacao. Verifique a mensagem acima.
pause

:fim
