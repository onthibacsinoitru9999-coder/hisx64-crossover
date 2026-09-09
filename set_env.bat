@echo off
:: set_env.bat: Tu dong nap Git, Python, MinGW C++, .NET C# vao PATH
if defined HIS_ENV_READY goto :eof

:: 1. Git
for /d %%p in ("%LOCALAPPDATA%\Microsoft\WinGet\Packages\Git.MinGit*") do if exist "%%p\cmd\git.exe" set "PATH=%%p\cmd;%PATH%"
if exist "C:\Program Files\Git\cmd\git.exe" set "PATH=C:\Program Files\Git\cmd;%PATH%"

:: 2. Python 3.12+
for /d %%p in ("%LOCALAPPDATA%\Programs\Python\Python*") do if exist "%%p\python.exe" set "PATH=%%p;%%p\Scripts;%PATH%"

:: 3. C++ MinGW (GCC/G++)
for /d %%p in ("%LOCALAPPDATA%\Microsoft\WinGet\Packages\BrechtSanders.WinLibs*") do if exist "%%p\mingw64\bin\g++.exe" set "PATH=%%p\mingw64\bin;%PATH%"
if exist "C:\MinGW\bin\g++.exe" set "PATH=C:\MinGW\bin;%PATH%"

:: 4. C# (csc.exe)
if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319;%PATH%"

set HIS_ENV_READY=1
