# ============================================================
# STAGE 1 - BUILD
# ============================================================

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src


# ============================================================
# COPIA TODO O PROJETO
# ============================================================

COPY . .


# ============================================================
# RESTORE
# ============================================================

RUN dotnet restore SystemOrder.sln


# ============================================================
# BUILD
# ============================================================

RUN dotnet build SystemOrder.sln \
    --configuration Release \
    --no-restore


# ============================================================
# TESTES UNITÁRIOS
# ============================================================

RUN dotnet test tests/SystemOrder.UnitTests/SystemOrder.UnitTests.csproj \
    --configuration Release \
    --no-build \
    --no-restore


# ============================================================
# PUBLISH
# ============================================================

RUN dotnet publish src/SystemOrder.Api/SystemOrder.Api.csproj \
    --configuration Release \
    --no-build \
    --no-restore \
    --output /app/publish


# ============================================================
# STAGE 2 - RUNTIME
# ============================================================

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .


# ============================================================
# CONFIGURAÇÃO
# ============================================================

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080


# ============================================================
# START
# ============================================================

ENTRYPOINT ["dotnet", "SystemOrder.Api.dll"]