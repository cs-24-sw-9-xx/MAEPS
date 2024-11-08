#!/bin/sh

set -e
set -u
set -x

for filename in *.csproj; do
  dotnet format "$filename" --exclude "Assets/RosMessages/" --exclude "Assets/YamlDotNet/"
done
