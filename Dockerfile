FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LiteFactoryWeb.csproj ./
RUN dotnet restore LiteFactoryWeb.csproj

COPY . ./
RUN dotnet publish LiteFactoryWeb.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "LiteFactoryWeb.dll"]
