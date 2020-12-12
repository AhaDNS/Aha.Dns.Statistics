#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Aha.Dns.Statistics.WebUI/Aha.Dns.Statistics.WebUI.csproj", "Aha.Dns.Statistics.WebUI/"]
RUN dotnet restore "Aha.Dns.Statistics.WebUI/Aha.Dns.Statistics.WebUI.csproj"
COPY . .
WORKDIR "/src/Aha.Dns.Statistics.WebUI"
RUN dotnet build "Aha.Dns.Statistics.WebUI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Aha.Dns.Statistics.WebUI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aha.Dns.Statistics.WebUI.dll"]