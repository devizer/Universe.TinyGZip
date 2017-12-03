@echo off
rem git log --pretty=format:"%h %h" | grep ' ' -c > "\temp\Universe.TinyGZip.commits"
for /f %%i in ('datetime local') do set datetime=%%i
"C:\Program Files\7-Zip\7zG.exe" a -t7z -mx=9 -mfb=128 -md=128m -ms=on -xr!.git -xr!.vs ^
  "C:\Users\Backups on Google Drive\Universe.TinyGZip (%datetime%) 1.0.7z" .

:end