# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Базовый образ для запуска приложения
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Определение версии приложения
ARG VERSION
ENV APP_VERSION=$VERSION

# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION  # Используем VERSION на этапе сборки
WORKDIR /src
COPY ["integrator_ui_back/integrator_ui_back.csproj", "integrator_ui_back/"]
RUN dotnet restore "./integrator_ui_back/integrator_ui_back.csproj"
COPY . .
WORKDIR "/src/integrator_ui_back"
RUN dotnet build "./integrator_ui_back.csproj" -c $BUILD_CONFIGURATION -p:Version=$VERSION -o /app/build

# Этап публикации
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG VERSION  # Используем VERSION на этапе публикации
RUN dotnet publish "./integrator_ui_back.csproj" -c $BUILD_CONFIGURATION -p:Version=$VERSION -o /app/publish /p:UseAppHost=false

# Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Устанавливаем APP_VERSION для доступа в приложении
ENV APP_VERSION=$VERSION
ENTRYPOINT ["dotnet", "integrator_ui_back.dll"]
