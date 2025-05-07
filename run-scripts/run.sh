#!/bin/bash

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

limit=$1  # or set this from user input or arguments

for ((i=0; i<limit; i++)); do
    "$SCRIPT_DIR"/*.x86_64 -logFile /dev/stdout --instances "$limit" --instanceid "$i" &
done

wait  # wait for all background processes to complete
