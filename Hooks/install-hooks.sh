#!/bin/sh

HOOKS_DIR="$(git rev-parse --show-toplevel)/Hooks"
GIT_HOOKS_DIR="$(git rev-parse --show-toplevel)/.git/hooks"

echo "Installing Git hooks..."
ln -sf "$HOOKS_DIR/pre-commit" "$GIT_HOOKS_DIR/pre-commit"
chmod +x "$GIT_HOOKS_DIR/pre-commit"

echo "Pre-commit hook installed!"
