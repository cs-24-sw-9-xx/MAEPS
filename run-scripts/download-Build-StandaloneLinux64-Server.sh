#!/bin/bash
set -e

# === Validate Required Environment Variables ===
if [[ -z "$GITHUB_TOKEN" || -z "$GITHUB_OWNER" || -z "$REPO_NAME" ]]; then
  echo "❌ Error: GITHUB_TOKEN, GITHUB_OWNER, and REPO_NAME must be set."
  exit 1
fi

# === Config ===
ARTIFACT_NAME="Build-StandaloneLinux64-Server"
BRANCH_NAME="main"
DOWNLOAD_DIR="./artifacts"
BACKUP_DIR="./backup_data"

artifact_zip="$DOWNLOAD_DIR/$ARTIFACT_NAME.zip"
artifact_output_dir="$DOWNLOAD_DIR/$ARTIFACT_NAME"

DATA_FOLDER="$artifact_output_dir/StandaloneLinux64/data"

mkdir -p "$DOWNLOAD_DIR"

# === Backup existing data if present ===
if [[ -d "$artifact_output_dir" ]]; then
  if [[ -d "$DATA_FOLDER" ]]; then
    echo "📦 Backing up existing data files..."
    rm -rf "$BACKUP_DIR"
    cp -r "$DATA_FOLDER" "$BACKUP_DIR"
  fi
  echo "🧹 Removing old artifact directory..."
  rm -rf "$artifact_output_dir"
fi

# === Fetch run ID ===
if [[ -z "$1" ]]; then
  echo "📡 Fetching latest successful workflow run on branch '$BRANCH_NAME'..."
  latest_run_id=$(curl -s -H "Authorization: token $GITHUB_TOKEN" \
    "https://api.github.com/repos/$GITHUB_OWNER/$REPO_NAME/actions/runs?branch=$BRANCH_NAME&status=success&per_page=1" \
    | jq -r '.workflow_runs[0].id')

  if [[ -z "$latest_run_id" || "$latest_run_id" == "null" ]]; then
    echo "❌ No successful workflow runs found on branch '$BRANCH_NAME'."
    exit 1
  fi

  echo "✅ Latest successful workflow run ID: $latest_run_id"
  run_id=$latest_run_id
else
  run_id=$1
  echo "📡 Using provided run ID: $run_id"
fi


# === Get artifact info ===
echo "📦 Fetching artifacts for run ID $run_id..."
artifact_info=$(curl -s -H "Authorization: token $GITHUB_TOKEN" \
  "https://api.github.com/repos/$GITHUB_OWNER/$REPO_NAME/actions/runs/$run_id/artifacts" \
  | jq -r --arg name "$ARTIFACT_NAME" \
    '.artifacts | map(select(.name == $name)) | first')

if [[ -z "$artifact_info" || "$artifact_info" == "null" ]]; then
  echo "❌ Artifact '$ARTIFACT_NAME' not found in run $run_id."
  exit 1
fi

artifact_id=$(echo "$artifact_info" | jq -r '.id')
echo "🎯 Found artifact '$ARTIFACT_NAME' (ID: $artifact_id)"

# === Download artifact zip ===
echo "⬇️  Downloading artifact..."
curl -L -s -H "Authorization: token $GITHUB_TOKEN" \
  -H "Accept: application/vnd.github+json" \
  -o "$artifact_zip" \
  "https://api.github.com/repos/$GITHUB_OWNER/$REPO_NAME/actions/artifacts/$artifact_id/zip"

# === Extract ===
echo "📂 Unzipping to: $artifact_output_dir"
mkdir -p "$artifact_output_dir"
unzip -o "$artifact_zip" -d "$artifact_output_dir"
rm -f "$artifact_zip"

# === Restore data if backup exists ===
if [[ -d "$BACKUP_DIR" ]]; then
  echo "🔄 Restoring backed-up data files..."
  cp -r "$BACKUP_DIR" "$DATA_FOLDER"
  echo "🧹 Cleaning up backup..."
  rm -rf "$BACKUP_DIR"
fi

echo "✅ Done. Artifact ready at: $artifact_output_dir"