FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Shared/MiniProject.Shared/MiniProject.Shared.csproj", "src/Shared/MiniProject.Shared/"]
COPY ["src/CatalogService/CatalogService.csproj", "src/CatalogService/"]
RUN dotnet restore "src/CatalogService/CatalogService.csproj"

COPY . .
WORKDIR "/src/src/CatalogService"
RUN dotnet publish "CatalogService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002
ENTRYPOINT ["dotnet", "CatalogService.dll"]
