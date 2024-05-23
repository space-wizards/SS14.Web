FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN mkdir /repo && chown $APP_UID /repo
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SS14.Auth/SS14.Auth.csproj", "SS14.Auth/"]
RUN dotnet restore "SS14.Auth/SS14.Auth.csproj"
COPY . .
WORKDIR "/src/SS14.Auth"
RUN dotnet build "SS14.Auth.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SS14.Auth.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SS14.Auth.dll"]
