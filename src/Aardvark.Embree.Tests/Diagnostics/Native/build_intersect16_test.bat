@echo off
REM build_intersect16_test.bat
REM Compiles and runs the Embree Intersect16 native test

echo ========================================
echo Embree Intersect16 Native Test Builder
echo ========================================
echo.

REM Check for Visual Studio environment
where cl >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: cl.exe not found. Please run this from a Visual Studio Developer Command Prompt.
    echo.
    echo Options:
    echo   1. Open "x64 Native Tools Command Prompt for VS 2022"
    echo   2. Or run: "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
    echo.
    pause
    exit /b 1
)

REM Detect Embree installation path
set EMBREE_PATH=
if exist "C:\Program Files\Intel\embree4" (
    set EMBREE_PATH=C:\Program Files\Intel\embree4
) else if exist "D:\embree4" (
    set EMBREE_PATH=D:\embree4
) else if exist "%ProgramFiles%\embree4" (
    set EMBREE_PATH=%ProgramFiles%\embree4
)

if "%EMBREE_PATH%"=="" (
    echo ERROR: Embree installation not found.
    echo.
    echo Please install Embree 4 or set EMBREE_PATH manually:
    echo   set EMBREE_PATH=C:\path\to\embree4
    echo.
    echo Download from: https://github.com/RenderKit/embree/releases
    pause
    exit /b 1
)

echo Found Embree at: %EMBREE_PATH%
echo.

REM Set include and library paths
set EMBREE_INCLUDE=%EMBREE_PATH%\include
set EMBREE_LIB=%EMBREE_PATH%\lib
set EMBREE_BIN=%EMBREE_PATH%\bin

REM Verify paths exist
if not exist "%EMBREE_INCLUDE%\embree4\rtcore.h" (
    echo ERROR: Embree headers not found at: %EMBREE_INCLUDE%
    pause
    exit /b 1
)

if not exist "%EMBREE_LIB%\embree4.lib" (
    echo ERROR: Embree library not found at: %EMBREE_LIB%
    pause
    exit /b 1
)

echo Compiling embree_intersect16_test.cpp...
echo.

REM Compile with optimizations and proper alignment
cl.exe /nologo /O2 /W4 /EHsc ^
    /I"%EMBREE_INCLUDE%" ^
    embree_intersect16_test.cpp ^
    /link /LIBPATH:"%EMBREE_LIB%" embree4.lib ^
    /OUT:embree_intersect16_test.exe

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Compilation failed
    pause
    exit /b 1
)

echo.
echo Compilation successful!
echo.

REM Add Embree DLL directory to PATH for runtime
set PATH=%EMBREE_BIN%;%PATH%

echo Running embree_intersect16_test.exe...
echo ========================================
echo.

REM Run the test
embree_intersect16_test.exe

set TEST_RESULT=%errorlevel%

echo.
echo ========================================
if %TEST_RESULT%==0 (
    echo TEST PASSED
) else (
    echo TEST FAILED with exit code %TEST_RESULT%
)
echo ========================================
echo.

REM Cleanup object files
if exist embree_intersect16_test.obj del embree_intersect16_test.obj

pause
exit /b %TEST_RESULT%
