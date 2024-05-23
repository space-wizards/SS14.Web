FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN mkdir /repo && chown $APP_UID /repo
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SS14.ServerHub/SS14.ServerHub.csproj", "SS14.ServerHub/"]
RUN dotnet restore "SS14.ServerHub/SS14.ServerHub.csproj"
COPY . .
WORKDIR "/src/SS14.ServerHub"
RUN dotnet build "SS14.ServerHub.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SS14.ServerHub.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SS14.ServerHub.dll"]
