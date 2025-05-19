#!/bin/bash

if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <experiment> <instances>" >&2
    exit 1
fi

if ! command -v "ts" 2>&1 >/dev/null
then
    echo "ts could not be found. Install it with 'pacman -Suy moreutils'"
    exit 1
fi

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

limit=$2  # or set this from user input or arguments

{ for ((i=0; i<limit; i++)); do
    "$SCRIPT_DIR/@EXECUTABLE_NAME" -logFile /dev/stdout -batchmode -nographics --instances "$limit" --instanceid "$i" --experiment "$1" | ts "[%Y-%m-%d %H:%M:%S (instance $i)]" &
done } | tee output.log

wait  # wait for all background processes to complete

grep "Simulation did not complete successfully" output.log > failed.log

# Check if grep found any matches, print error message in red if it did
if [ -s failed.log ]; then
    echo -e "\e[31mSome simulations failed. Check failed.log for details.\e[0m"
fi
