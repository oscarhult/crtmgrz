FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY . /src
RUN dotnet publish /src -c Release -r linux-musl-x64 --sc false -o /out -p:PublishSingleFile=true -p:DebugSymbols=false -p:DebugType=None

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
RUN apk add --no-cache tzdata
USER app
WORKDIR /app
RUN mkdir -p /app/data
COPY --chown=app:app --from=build /out /app
ENTRYPOINT ["./crtmgrz"]
