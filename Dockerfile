FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
COPY . /src
RUN dotnet publish \
  /src \
  -c Release \
  -r linux-musl-x64 \
  --sc false \
  -o /publish \
  -p:PublishSingleFile=true \
  -p:DebugSymbols=false \
  -p:DebugType=None

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
EXPOSE 8080
RUN apk add --no-cache tzdata
USER app
WORKDIR /app
COPY --chown=app:app --from=build /publish .
RUN mkdir data
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["./crtmgrz"]
