@echo off
setlocal

set HOOKS_DIR=%CD%\Hooks
set GIT_HOOKS_DIR=%CD%\.git\hooks

echo Installing Git hooks...
copy /Y "%HOOKS_DIR%\pre-commit" "%GIT_HOOKS_DIR%\pre-commit"

echo Pre-commit hook installed!
