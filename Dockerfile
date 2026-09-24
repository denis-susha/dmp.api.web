# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore in a separate layer so it is cached until project files change.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY API.Web/API.Web.csproj API.Web/
COPY DMP.BL/DMP.BL.csproj DMP.BL/
COPY DMP.Crosscutting/DMP.Crosscutting.csproj DMP.Crosscutting/
COPY DMP.DataAccess/DMP.DataAccess.csproj DMP.DataAccess/
RUN dotnet restore API.Web/API.Web.csproj

COPY . .
RUN dotnet publish API.Web/API.Web.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "API.Web.dll"]
