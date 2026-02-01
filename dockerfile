# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Build image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["VmPortal.Web/VmPortal.Web.csproj", "VmPortal.Web/"]
COPY ["VmPortal.Infrastructure/VmPortal.Infrastructure.csproj", "VmPortal.Infrastructure/"]
COPY ["VmPortal.Application/VmPortal.Application.csproj", "VmPortal.Application/"]
COPY ["VmPortal.Domain/VmPortal.Domain.csproj", "VmPortal.Domain/"]

RUN dotnet restore "VmPortal.Web/VmPortal.Web.csproj"

COPY . .

WORKDIR "/src/VmPortal.Web"
RUN dotnet build "VmPortal.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VmPortal.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Keep working dir /app so Data Source=./data/vmportal.db works
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "VmPortal.Web.dll"]