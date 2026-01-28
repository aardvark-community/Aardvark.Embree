@echo off
REM Build script for Embree point query instance diagnostic test
REM Requires Visual Studio 2022 or later

echo Building embree_point_query_instance_test.cpp...

REM Set paths
set EMBREE_INCLUDE=..\..\..\..\..\..\include\embree4
set EMBREE_LIB=..\..\..\..\..\..\lib\Native\Aardvark.Embree\windows\AMD64

REM Compile
cl /EHsc /I"%EMBREE_INCLUDE%" embree_point_query_instance_test.cpp /link "%EMBREE_LIB%\embree4.lib"

if %ERRORLEVEL% NEQ 0 (
    echo Build failed
    exit /b 1
)

echo Build successful
echo.
echo Running test...
echo.

embree_point_query_instance_test.exe

pause
