#!/bin/sh

set -e
set -u
set -x

dotnet format MAEPS.sln --exclude "Assets/RosMessages/" --exclude "Assets/YamlDotNet/" --verbosity diagnostic
