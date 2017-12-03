#!/bin/bash
nuget restore
xbuild /t:Rebuild /p:Configuration=Release /verbosity:minimal
xbuild /t:Rebuild /p:Configuration=Debug   /verbosity:minimal
mono --desktop packages/NUnit.ConsoleRunner.3.4.1/tools/nunit3-console.exe --labels=On --workers=1 --work=Universe.TinyGZip.Tests/bin/Release Universe.TinyGZip.Tests/bin/Release/Universe.TinyGZip.Tests.dll
