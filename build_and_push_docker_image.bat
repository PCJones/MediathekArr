@echo off
SET IMAGE_NAME=pcjones/mediathekarr
SET BUILDER=mediathekarr-builder

echo Enter the version number for the Docker image:
set /p VERSION="Version: "

docker buildx inspect %BUILDER% >nul 2>&1 || docker buildx create --name %BUILDER% --driver docker-container

echo Building and pushing Docker image with version %VERSION% (amd64 + arm64)...
docker buildx build --builder %BUILDER% --platform linux/amd64,linux/arm64 -t %IMAGE_NAME%:%VERSION% --push .

#echo Pushing Docker image with tag latest...
#docker buildx build --builder %BUILDER% --platform linux/amd64,linux/arm64 -t %IMAGE_NAME%:%VERSION% -t %IMAGE_NAME%:latest --push .

echo Done.
pause
