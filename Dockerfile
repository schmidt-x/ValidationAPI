FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY src/ValidationAPI/*.csproj src/ValidationAPI/
WORKDIR src/ValidationAPI
RUN dotnet restore

COPY src/ValidationAPI/. ./
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "ValidationAPI.dll"]