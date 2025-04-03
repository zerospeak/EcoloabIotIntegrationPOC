FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["IoTSimulator.csproj", "./"]
RUN dotnet restore "IoTSimulator.csproj"
COPY . .
RUN dotnet build "IoTSimulator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IoTSimulator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IoTSimulator.dll"]
