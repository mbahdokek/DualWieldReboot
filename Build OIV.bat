@echo off
REM === CONFIGURATION ===
set "SOURCE=resources"
set "DEST=DualWield.oiv"
set "SEVENZIP=7z"

REM === CLEANUP OLD FILE ===
if exist "%DEST%" del "%DEST%"

REM === CHANGE TO SOURCE DIRECTORY ===
pushd "%SOURCE%"

REM === COMPRESS CONTENTS ONLY (NOT FOLDER ITSELF) ===
"%SEVENZIP%" a -tzip "..\%DEST%" * -mx9 -y

REM === RETURN TO ORIGINAL DIRECTORY ===
popd

echo.
echo Done! Created OIV "%DEST%"
echo REMEMBER THAT ONLY RELEASE BUILDS ARE COPIED!
pause
