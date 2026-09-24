@echo off
REM Grabador de temas musicales.
REM Deja tus canciones (.mp3, .ogg o .wav) en la carpeta audio\ de al lado.

set GODOT="%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"

if not exist %GODOT% (
  echo No se encuentra Godot en la ruta esperada.
  echo Abre el proyecto a mano: Godot ^> Importar ^> este directorio
  pause
  exit /b 1
)

cd /d "%~dp0"
%GODOT% --path "%~dp0"
