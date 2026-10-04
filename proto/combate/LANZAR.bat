@echo off
REM Ventana de combate de E.N.M.A.: el equipo de prueba contra el muneco.
REM Compila el proyecto C# y abre la ventana. Con una semilla fija, los
REM mismos dados que la consola:   LANZAR.bat 7

set GODOT="%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"

if not exist %GODOT% (
  echo No se encuentra Godot en la ruta esperada.
  echo Abre el proyecto a mano: Godot ^> Importar ^> este directorio
  pause
  exit /b 1
)

cd /d "%~dp0"
dotnet build EnmaCombate.csproj -nologo -v q
if errorlevel 1 (
  echo No se pudo compilar. Revisa los errores de arriba.
  pause
  exit /b 1
)

if "%~1"=="" (
  %GODOT% --path "%~dp0"
) else (
  %GODOT% --path "%~dp0" -- --semilla=%~1
)
