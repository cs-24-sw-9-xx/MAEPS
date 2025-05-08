#!/bin/bash

if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <experiment> <instances>" >&2
    exit 1
fi

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

limit=$2  # or set this from user input or arguments

for ((i=0; i<limit; i++)); do
    "$SCRIPT_DIR"/*.x86_64 -logFile /dev/stdout -batchmode -nographics --instances "$limit" --instanceid "$i" --experiment "$1" &
done

wait  # wait for all background processes to complete
