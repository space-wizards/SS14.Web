FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
RUN mkdir /repo && chown 1654 /repo
USER 1654
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SS14.Web/SS14.Web.csproj", "SS14.Web/"]
RUN dotnet restore "SS14.Web/SS14.Web.csproj"
COPY . .
WORKDIR "/src/SS14.Web"
RUN dotnet build "SS14.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SS14.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SS14.Web.dll"]
