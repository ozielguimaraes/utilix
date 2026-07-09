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

WORKDIR /app

COPY --from=build /build/output .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Utilix.Api.dll"]
