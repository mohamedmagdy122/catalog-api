FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY Catalog.API/*.csproj Catalog.API/
RUN dotnet restore Catalog.API/Catalog.API.csproj
COPY Catalog.API/. Catalog.API/
RUN dotnet publish Catalog.API/Catalog.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
RUN useradd -m appuser
WORKDIR /app
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet","Catalog.API.dll"]
