@echo off
call "C:\Program Files\Microsoft Visual Studio\18\Insiders\VC\Auxiliary\Build\vcvarsall.bat" x64
cl.exe /EHsc /I"C:\Data\Development\Aardvark.Embree\include" embree_interpolate_test.cpp /link /LIBPATH:"C:\Data\Development\Aardvark.Embree\lib" embree4.lib /OUT:embree_interpolate_test.exe
