FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Shared/MiniProject.Shared/MiniProject.Shared.csproj", "src/Shared/MiniProject.Shared/"]
COPY ["src/OrderingService/OrderingService.csproj", "src/OrderingService/"]
RUN dotnet restore "src/OrderingService/OrderingService.csproj"

COPY . .
WORKDIR "/src/src/OrderingService"
RUN dotnet publish "OrderingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5003
ENV ASPNETCORE_URLS=http://+:5003
ENTRYPOINT ["dotnet", "OrderingService.dll"]
