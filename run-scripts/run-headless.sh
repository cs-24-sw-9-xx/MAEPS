#!/bin/bash

# Log unexpected termination
trap 'echo "Script terminated unexpectedly at $(date)" >> error.log' EXIT

# Delete the folder if it exists
if [ -d "instanceslog" ]; then
    rm -rf instanceslog
fi

# Validate argument count
if [ "$#" -ne 2 ] && [ "$#" -ne 4 ]; then
    echo "Usage:" >&2
    echo "  $0 <experiment> <total_instances>                # Run all instances on this machine" >&2
    echo "  $0 <experiment> <total_instances> <start> <end>  # Run instances from including <start> to including <end>" >&2
    exit 1
fi

experiment=$1
total_instances=$2

if [ "$#" -eq 2 ]; then
    # Run all instances
    start_id=0
    end_id=$((total_instances - 1))
else
    start_id=$3
    end_id=$4
fi

if ! command -v "ts" 2>&1 >/dev/null
then
    echo "ts could not be found. Install it with 'pacman -Suy moreutils'"
    exit 1
fi

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

{ for ((i=start_id; i<=end_id; i++)); do
    "$SCRIPT_DIR/@EXECUTABLE_NAME" -logFile /dev/stdout -batchmode -nographics --instances "$total_instances" --instanceid "$i" --experiment "$experiment" | ts "[%Y-%m-%d %H:%M:%S (instance $i)]" &
done } | tee output.log

wait  # wait for all background processes to complete

echo "All instances have shut down."

grep "Simulation did not complete successfully" output.log > failed.log

grep "Exception" output.log > exceptions.log

echo "Finished generating output.log, failed.log, and exceptions.log"

# Check if grep found any matches, print error message in red if it did
if [ -s failed.log ]; then
    echo -e "\e[31mSome simulations failed. Check failed.log for details.\e[0m"
fi

# Check if grep found any matches, print error message in red if it did
if [ -s exceptions.log ]; then
    echo -e "\e[31mSome exceptions occurred. Check exceptions.log for details.\e[0m"
fi

# Create a folder for instance logs
mkdir -p instanceslog

# Separate logs for each instance
for ((i=start_id; i<=end_id; i++)); do
    grep "(instance $i)" output.log > "instanceslog/instance_$i.log"
done

echo "Logs for each instance have been saved in the 'instanceslog' folder."

# Remove the trap after successful execution
trap - EXIT