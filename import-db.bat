@echo off
chcp 65001 > nul
echo ===================================================
echo   NovaGames Database Importer
echo ===================================================

set MYSQL_PWD=147325689
set MYSQL_BIN=mysql.exe

where mysql.exe >nul 2>nul
if %ERRORLEVEL% equ 0 goto RUN_IMPORT

if exist "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" (
    set MYSQL_BIN="C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
    goto RUN_IMPORT
)
if exist "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" (
    set MYSQL_BIN="C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe"
    goto RUN_IMPORT
)
if exist "C:\xampp\mysql\bin\mysql.exe" (
    set MYSQL_BIN="C:\xampp\mysql\bin\mysql.exe"
    goto RUN_IMPORT
)

echo [ERROR] mysql.exe not found! Please install MySQL or set PATH.
pause
exit /b 1

:RUN_IMPORT
echo Importing database_dump.sql...
%MYSQL_BIN% -u root --password=%MYSQL_PWD% < "%~dp0database_dump.sql"
if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] Databases miniproject_auth, miniproject_catalog, miniproject_ordering imported successfully!
) else (
    echo [ERROR] Import failed! Please check MySQL service and root password.
)
pause
