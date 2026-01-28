@echo off
echo Building Embree interpolation test...

call "C:\Program Files\Microsoft Visual Studio\18\Insiders\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1

set EMBREE_INCLUDE=C:\Data\Development\Aardvark.Embree\include
set EMBREE_LIB=C:\Data\Development\Aardvark.Embree\lib

echo Using EMBREE_INCLUDE: %EMBREE_INCLUDE%
echo Using EMBREE_LIB: %EMBREE_LIB%

cl.exe /EHsc /I"%EMBREE_INCLUDE%" embree_interpolate_test.cpp /link /LIBPATH:"%EMBREE_LIB%" embree4.lib /OUT:embree_interpolate_test.exe

if %ERRORLEVEL% EQU 0 (
    echo Build succeeded. Running test...
    embree_interpolate_test.exe
) else (
    echo Build failed with error code %ERRORLEVEL%
)
