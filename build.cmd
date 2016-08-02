@echo off
mkdir out 1>nul 2>&1

echo. > out\Universe.TinyGZip.cs.tmp
for %%f in (Universe.TinyGZip\*.cs) DO (
  type %%f >> out\Universe.TinyGZip.cs.tmp
  echo. >> out\Universe.TinyGZip.cs.tmp
  echo. >> out\Universe.TinyGZip.cs.tmp
)
rem type *.cs > ..\Universe.TinyGZip.cs.tmp
type out\Universe.TinyGZip.cs.tmp | grep -v '^//' > out\Universe.TinyGZip.cs
del out\Universe.TinyGZip.cs.tmp

IF "%PROCESSOR_ARCHITECTURE%"=="x86" (set FW=Framework) else (set FW=Framework64)

set CSC4=%windir%\Microsoft.NET\%FW%\v4.0.30319\csc.exe
set CSC2=%windir%\Microsoft.NET\%FW%\v2.0.50727\csc.exe

mkdir bin 1>nul 2>&1
%CSC4% /out:bin\Universe.TinyGZip.%FW%-csc4.dll /target:library /debug- /optimize+ ^
  /langversion:4 /platform:anycpu ^
  out\Universe.TinyGZip.cs

%CSC4% /out:bin\Universe.TinyGZip.%FW%-csc2.dll /target:library /debug- /optimize+ ^
  /langversion:4 /platform:anycpu ^
  out\Universe.TinyGZip.cs
  