@echo off
setlocal

REM ========== EDIT THESE ==========
set REPO_URL=https://github.com/BeInForIt/ScanRecolorPlus
set COMMIT_MSG=Auto update
REM ================================

REM Check Git
where git >nul 2>nul
if errorlevel 1 (
    echo ERROR: Git is not installed or not in PATH.
    echo Install Git for Windows: https://git-scm.com/download/win
    pause
    exit /b 1
)

REM Create .gitignore if missing
if not exist .gitignore (
    echo Creating .gitignore...

    (
        echo # Build output
        echo bin/
        echo obj/
        echo.
        echo # Visual Studio
        echo .vs/
        echo *.user
        echo *.suo
        echo *.csproj.user
        echo *.sln
        echo *.slnx
        echo.
        echo # Debug symbols
        echo *.pdb
        echo.
        echo # OS junk
        echo Thumbs.db
        echo .DS_Store
        echo.
        echo # Local packages
        echo *.zip
    ) > .gitignore
)

REM Init repo if needed
if not exist .git (
    echo Initializing git repository...
    git init
    git branch -M main
    git remote add origin %REPO_URL%
)

REM Stage all files (gitignore will filter junk)
git add .

REM Commit
git commit -m "%COMMIT_MSG%" || echo Nothing to commit

REM Try normal push first
git push -u origin main
if errorlevel 1 (
    echo Push rejected. Trying pull...
    git pull origin main
    if errorlevel 1 (
        echo Pull failed. Trying allow-unrelated-histories...
        git pull origin main --allow-unrelated-histories
        if errorlevel 1 (
            echo Still failed. Forcing push (overwriting remote)...
            git push -f -u origin main
            goto :done
        )
    )
    git push -u origin main
)

:done
echo Done.
pause
