# Build from repository `src` directory context.
# Example:
# docker build -f api/Fig.Api/Dockerfile -t fig.api ./src

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "api/Fig.Api/Fig.Api.csproj"
RUN dotnet publish "api/Fig.Api/Fig.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080
EXPOSE 8443

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Fig.Api.dll"]
