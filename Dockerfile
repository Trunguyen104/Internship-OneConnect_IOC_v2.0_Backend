# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["IOCv2.API/IOCv2.API.csproj", "IOCv2.API/"]
COPY ["IOCv2.Application/IOCv2.Application.csproj", "IOCv2.Application/"]
COPY ["IOCv2.Domain/IOCv2.Domain.csproj", "IOCv2.Domain/"]
COPY ["IOCv2.Infrastructure/IOCv2.Infrastructure.csproj", "IOCv2.Infrastructure/"]

RUN dotnet restore "IOCv2.API/IOCv2.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/IOCv2.API"
RUN dotnet build "IOCv2.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "IOCv2.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "IOCv2.API.dll"]