#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Cosmos.IdentityManagement.Website/Cosmos.IdentityManagement.Website.csproj", "Cosmos.IdentityManagement.Website/"]
# RUN dotnet restore "Cosmos.IdentityManagement.Website/Cosmos.IdentityManagement.Website.csproj"
COPY . .
WORKDIR "/src/Cosmos.IdentityManagement.Website"
RUN dotnet build "Cosmos.IdentityManagement.Website.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cosmos.IdentityManagement.Website.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cosmos.IdentityManagement.Website.dll"]