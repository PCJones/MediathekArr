@echo off
SET IMAGE_NAME=pcjones/mediathekarr

echo Enter the version number for the Docker image:
set /p VERSION="Version: "

echo Building Docker image with version %VERSION%...
docker build -t %IMAGE_NAME%:%VERSION% .
docker tag %IMAGE_NAME%:%VERSION% %IMAGE_NAME%:latest

echo Pushing Docker image with version %VERSION%...
docker push %IMAGE_NAME%:%VERSION%

echo Pushing Docker image with tag latest...
docker push %IMAGE_NAME%:latest

echo Done.
pause