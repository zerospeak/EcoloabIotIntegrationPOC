FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EventProcessor.csproj", "./"]
RUN dotnet restore "EventProcessor.csproj"
COPY . .
RUN dotnet build "EventProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventProcessor.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet:4 AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
