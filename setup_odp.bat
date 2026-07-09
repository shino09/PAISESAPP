@echo off
echo =============================================
echo Instalacion de Oracle.ManagedDataAccess
echo =============================================
echo.

REM Check if nuget.exe exists
if not exist "nuget.exe" (
    echo Descargando nuget.exe...
    powershell -Command "Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile 'nuget.exe'"
    if %errorlevel% neq 0 (
        echo ERROR: No se pudo descargar nuget.exe
        echo Descarguelo manualmente de: https://nuget.org/downloads
        pause
        exit /b 1
    )
)

echo Instalando paquete Oracle.ManagedDataAccess...
nuget install packages.config -OutputDirectory packages
if %errorlevel% neq 0 (
    echo ERROR: No se pudo instalar el paquete NuGet.
    pause
    exit /b 1
)

echo.
echo Copiando DLL a bin...
if not exist "bin" mkdir bin
for /r packages %%f in (*Oracle.ManagedDataAccess.dll) do (
    if not "%%f"=="%%~dpfnetstandard2.0\Oracle.ManagedDataAccess.dll" (
        copy /Y "%%f" "bin\"
        echo Copiado: %%f
    )
)

echo.
echo =============================================
echo Instalacion completada.
echo.
echo Si el script no encontro la DLL, copie manualmente:
echo   packages\Oracle.ManagedDataAccess.21.12.0\lib\net472\Oracle.ManagedDataAccess.dll
echo   a la carpeta bin\
echo =============================================
pause
