FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app 

COPY *.sln .

COPY App.DAL/*.csproj ./App.DAL/
COPY App.Infrastructure/*.csproj ./App.Infrastructure/
COPY App.Models/*.csproj ./App.Models/
COPY App.Tests/*.csproj ./App.Tests/

COPY WebApp/*.csproj ./WebApp/

RUN dotnet restore

COPY App.DAL/. ./App.DAL/
COPY App.Infrastructure/. ./App.Infrastructure/
COPY App.Models/. ./App.Models/
COPY App.Tests/. ./App.Tests/
COPY WebApp/. ./WebApp/

WORKDIR /app/WebApp
RUN dotnet publish -c Release -o out



FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
EXPOSE 80
EXPOSE 8080
WORKDIR /app

RUN apt-get update && apt-get install -y \
    firefox-esr \
    wget \
    gnupg \
    && rm -rf /var/lib/apt/lists/*

RUN wget -q https://github.com/mozilla/geckodriver/releases/download/v0.34.0/geckodriver-v0.34.0-linux64.tar.gz \
    && tar -xzf geckodriver-v0.34.0-linux64.tar.gz \
    && mv geckodriver /usr/local/bin/ \
    && rm geckodriver-v0.34.0-linux64.tar.gz
COPY --from=build /app/WebApp/out ./
ENTRYPOINT ["dotnet", "WebApp.dll"]