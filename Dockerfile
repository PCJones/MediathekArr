FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

ARG PUID=1000
ARG PGID=1000

RUN addgroup --gid $PGID appgroup && \
    adduser --disabled-password --gecos '' --uid $PUID --gid $PGID appuser && \
    chown -R appuser:appgroup /app

USER appuser

COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "MediathekArr.dll"]
