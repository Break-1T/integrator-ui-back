# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# ������� ����� ��� ������� ����������
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# ����������� ������ ����������
ARG VERSION
ENV APP_VERSION=$VERSION

# ���� ������
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION  # ���������� VERSION �� ����� ������
WORKDIR /src
COPY ["integrator_ui_back/integrator_ui_back.csproj", "integrator_ui_back/"]
RUN dotnet restore "./integrator_ui_back/integrator_ui_back.csproj"
COPY . .
WORKDIR "/src/integrator_ui_back"
RUN dotnet build "./integrator_ui_back.csproj" -c $BUILD_CONFIGURATION -p:Version=$VERSION -o /app/build

# ���� ����������
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG VERSION  # ���������� VERSION �� ����� ����������
RUN dotnet publish "./integrator_ui_back.csproj" -c $BUILD_CONFIGURATION -p:Version=$VERSION -o /app/publish /p:UseAppHost=false

# ��������� �����
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# ������������� APP_VERSION ��� ������� � ����������
ENV APP_VERSION=$VERSION
ENTRYPOINT ["dotnet", "integrator_ui_back.dll"]
