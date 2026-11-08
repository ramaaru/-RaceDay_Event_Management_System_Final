# Multi-stage build for the ASP.NET Core MVC frontend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RaceDay.MvC.csproj", "./"]
RUN dotnet restore "RaceDay.MvC.csproj"
COPY . .
RUN dotnet publish "RaceDay.MvC.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.MvC.dll"]