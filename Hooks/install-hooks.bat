@echo off
setlocal

:: Get the absolute path to the repository root
for /f "delims=" %%i in ('git rev-parse --show-toplevel') do set REPO_ROOT=%%i

:: Define hooks directories
set HOOKS_DIR=%REPO_ROOT%\Hooks
set GIT_HOOKS_DIR=%REPO_ROOT%\.git\hooks

:: Display paths for debugging
echo HOOKS_DIR: %HOOKS_DIR%
echo GIT_HOOKS_DIR: %GIT_HOOKS_DIR%

:: Ensure the hooks directory exists
if not exist "%HOOKS_DIR%\pre-commit" (
    echo Error: pre-commit hook file not found in %HOOKS_DIR%
    exit /b 1
)

:: Copy the pre-commit hook to the Git hooks folder
copy /Y "%HOOKS_DIR%\pre-commit" "%GIT_HOOKS_DIR%\pre-commit" >nul

:: Ensure the file copied successfully
if not exist "%GIT_HOOKS_DIR%\pre-commit" (
    echo Error: Failed to install pre-commit hook!
    exit /b 1
)

echo Pre-commit hook installed successfully!
exit /b 0
