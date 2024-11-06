#!/bin/sh

set -e
set -u
set -x

dotnet format EditTests.csproj
dotnet format PlayModeTests.csproj
dotnet format CustomScriptsAssembly.csproj
