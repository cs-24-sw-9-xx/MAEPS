#!/bin/bash

set -e
set -u
set -o pipefail
set -x

# Validate argument count
if [ "$#" -lt 3 ]; then
    echo "Usage:" >&2
    echo "  $0 <total servers> <server id> <experiment> [optional arguments passed to MAEPS]" >&2
    exit 1
fi

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

TOTAL_SERVERS="$1"
SERVER_ID="$2"
EXPERIMENT="$3"

SCENARIO_COUNT="$("$SCRIPT_DIR/@EXECUTABLE_NAME" -logFile /dev/stdout -batchmode -nographics --experiment "$EXPERIMENT" --scenario-count "${@:4}" | grep 'Scenario Count: ' | sed 's/.*Scenario Count: //')"

START="$(( SERVER_ID ))"
INCREMENT="$TOTAL_SERVERS"
END="$(( SCENARIO_COUNT - 1))"

echo "Scenario Count: $SCENARIO_COUNT"
echo "Start: $START"
echo "End: $END"


# Timeout a job after an hour
seq "$START" "$INCREMENT" "$END" | parallel --timeout 3600 --ungroup "$SCRIPT_DIR/@EXECUTABLE_NAME" -logFile /dev/stdout -batchmode -nographics --instances "$SCENARIO_COUNT" --experiment "$EXPERIMENT" "${@:4}" --instanceid {}\| ts "\"[%Y-%m-%d %H:%M:%S (instance {})]\"" | tee output.log
