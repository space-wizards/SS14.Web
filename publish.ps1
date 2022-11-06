#!/usr/bin/env pwsh

dotnet publish -c Release -r linux-x64 --no-self-contained SS14.Web/SS14.Web.csproj
dotnet publish -c Release -r linux-x64 --no-self-contained SS14.Auth/SS14.Auth.csproj
dotnet publish -c Release -r linux-x64 --no-self-contained SS14.ServerHub/SS14.ServerHub.csproj
