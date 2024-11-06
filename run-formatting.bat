@echo off

dotnet format EditTests.csproj
dotnet format PlayModeTests.csproj
dotnet format CustomScriptsAssembly.csproj

echo Formatting completed successfully!
