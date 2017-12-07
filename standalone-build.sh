#!/bin/bash
nuget restore
xbuild /t:Rebuild /p:Configuration=Release /verbosity:minimal
xbuild /t:Rebuild /p:Configuration=Debug   /verbosity:minimal

pushd packages/NUnit.ConsoleRunner.*/tools; export RUNNER_PATH=$(pwd); popd; echo RUNNER_PATH: $RUNNER_PATH;
mono --desktop $RUNNER_PATH/nunit3-console.exe --labels=On --workers=1 --work=Universe.TinyGZip.Tests/bin/Release Universe.TinyGZip.Tests/bin/Release/Universe.TinyGZip.Tests.dll
