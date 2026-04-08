FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

COPY . .

RUN dotnet restore
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release --no-build -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS final
WORKDIR /app

COPY --from=build /app .

EXPOSE 8088
ENTRYPOINT ["dotnet", "SeattleHotelAgent.Hosted.Agent.dll"]
