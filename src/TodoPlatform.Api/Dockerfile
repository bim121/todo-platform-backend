# Multi-stage build for TodoPlatform.Api
# Context: repository root (see docker-compose build.context)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first — restore layer caches until csproj/slnx change
COPY TodoPlatform.slnx ./
COPY src/TodoPlatform.Domain/TodoPlatform.Domain.csproj src/TodoPlatform.Domain/
COPY src/TodoPlatform.Application/TodoPlatform.Application.csproj src/TodoPlatform.Application/
COPY src/TodoPlatform.Infrastructure/TodoPlatform.Infrastructure.csproj src/TodoPlatform.Infrastructure/
COPY src/TodoPlatform.Api/TodoPlatform.Api.csproj src/TodoPlatform.Api/

RUN dotnet restore src/TodoPlatform.Api/TodoPlatform.Api.csproj

COPY src/ src/

RUN dotnet publish src/TodoPlatform.Api/TodoPlatform.Api.csproj \
    -c Release \
    -o /app \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# curl for compose healthcheck (B-08.3)
RUN apk add --no-cache curl

COPY --from=build /app .

# Official Microsoft images define APP_UID (non-root)
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TodoPlatform.Api.dll"]
