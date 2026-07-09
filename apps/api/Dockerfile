# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /build

COPY ["apps/api/Utilix.Api/Utilix.Api.csproj", "apps/api/Utilix.Api/"]
COPY ["apps/api/Utilix.Abstractions/Utilix.Abstractions.csproj", "apps/api/Utilix.Abstractions/"]
COPY ["apps/api/Utilix.Tests/Utilix.Tests.csproj", "apps/api/Utilix.Tests/"]

RUN dotnet restore "apps/api/Utilix.Api/Utilix.Api.csproj"

COPY . .

RUN dotnet build "apps/api/Utilix.Api/Utilix.Api.csproj" -c Release -o /build/output

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0

RUN apt-get update && apt-get install -y --no-install-recommends \
    ffmpeg python3 python3-pip \
    && pip3 install --no-cache-dir --break-system-packages yt-dlp \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /build/output .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Utilix.Api.dll"]
