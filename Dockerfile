FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RetradeBE/RetradeBE.csproj", "RetradeBE/"]
RUN dotnet restore "RetradeBE/RetradeBE.csproj"
COPY . .
WORKDIR "/src/RetradeBE"
RUN dotnet build "RetradeBE.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RetradeBE.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RetradeBE.dll"]