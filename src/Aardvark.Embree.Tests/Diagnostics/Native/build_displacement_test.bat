@echo off
REM Build script for Embree displacement mapping test

echo Looking for Visual Studio installation...

REM Try VS 2022 Community
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" (
    echo Found VS 2022 Community
    call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
    goto :build
)

REM Try VS 2022 Professional
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvarsall.bat" (
    echo Found VS 2022 Professional
    call "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvarsall.bat" x64
    goto :build
)

REM Try VS 2019
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" (
    echo Found VS 2019 Community
    call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
    goto :build
)

echo ERROR: Could not find Visual Studio installation
exit /b 1

:build
echo.
echo Building embree_displacement_test.cpp...
echo.

REM Set paths
set EMBREE_INCLUDE=C:\Data\Development\Aardvark.Embree\include
set EMBREE_LIB=C:\Data\Development\Aardvark.Embree\lib
set EMBREE_BIN=C:\Data\Development\Aardvark.Embree\lib\Native\Aardvark.Embree\windows\AMD64

REM Copy DLLs to current directory so exe can find them
copy "%EMBREE_BIN%\embree4.dll" . >nul
copy "%EMBREE_BIN%\tbb12.dll" . >nul
copy "%EMBREE_BIN%\tbbmalloc.dll" . >nul

REM Compile with Embree headers and libraries
cl.exe /EHsc /W3 /I"%EMBREE_INCLUDE%" embree_displacement_test.cpp /link /LIBPATH:"%EMBREE_LIB%" embree4.lib tbb12.lib

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed!
    exit /b %ERRORLEVEL%
)

echo.
echo Build successful!
echo.
echo Running test...
echo.

embree_displacement_test.exe
set TEST_RESULT=%ERRORLEVEL%

echo.
echo Test finished with exit code: %TEST_RESULT%
exit /b %TEST_RESULT%
