@echo off
echo Starting GameGuild CMS...

cd /d "w:\repositories\game-guild\game-guild\apps\cms"

echo Building the application...
dotnet build --verbosity quiet --no-restore
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Starting the application...
start /b dotnet run --no-build --verbosity quiet

echo Waiting for application to start...
timeout /t 10 /nobreak > nul

echo Testing API endpoints...

echo.
echo Testing admin status endpoint:
curl -s http://localhost:5001/api/admin/status

echo.
echo.
echo Testing projects endpoint (should return 401):
curl -s http://localhost:5001/projects

echo.
echo.
echo Testing admin seed endpoint:
curl -s -X POST http://localhost:5001/api/admin/seed

echo.
echo.
echo Testing projects endpoint again (should work now with public access):
curl -s http://localhost:5001/projects

echo.
echo.
echo Press any key to stop the application...
pause > nul

echo Stopping the application...
taskkill /f /im dotnet.exe > nul 2>&1

echo Done.
