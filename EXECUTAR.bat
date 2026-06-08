@echo off
echo Iniciando AfixRFID Etiquetas...
start http://localhost:5050
dotnet run --project RfidEtiquetas.csproj
