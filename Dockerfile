FROM mcr.microsoft.com/dotnet/sdk:8.0 as build

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

FROM mcr.microsoft.com/dotnet/aspnet:8.0 as runtime
EXPOSE 80
EXPOSE 8080
WORKDIR /app
COPY --from=build /app/WebApp/out ./

ENTRYPOINT ["dotnet", "WebApp.dll"]