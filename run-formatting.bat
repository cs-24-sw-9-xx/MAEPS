@echo off

for %f in (*.csproj) do dotnet format "%f" --exclude "Assets\RosMessages" --exclude "Assets\YamlDotNet"
