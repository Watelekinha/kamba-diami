# Imagem única: a API (.NET 9) serve também o frontend Angular, no mesmo domínio.
# Contexto de compilação: a raiz do repositório (a pasta que contém backend/ e frontend/).

FROM node:20-slim AS frontend
WORKDIR /web
COPY frontend/kamba-diami-web/package.json frontend/kamba-diami-web/package-lock.json ./
RUN npm ci
COPY frontend/kamba-diami-web/ ./
RUN npx ng build --configuration production

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Primeiro só os .csproj, para que o restauro dos pacotes fique em cache entre compilações.
COPY backend/src/KambaDiami.Api/KambaDiami.Api.csproj src/KambaDiami.Api/
COPY backend/src/KambaDiami.Application/KambaDiami.Application.csproj src/KambaDiami.Application/
COPY backend/src/KambaDiami.Domain/KambaDiami.Domain.csproj src/KambaDiami.Domain/
COPY backend/src/KambaDiami.Infrastructure/KambaDiami.Infrastructure.csproj src/KambaDiami.Infrastructure/
COPY backend/src/KambaDiami.Shared/KambaDiami.Shared.csproj src/KambaDiami.Shared/
RUN dotnet restore src/KambaDiami.Api/KambaDiami.Api.csproj

COPY backend/src ./src
RUN dotnet publish src/KambaDiami.Api/KambaDiami.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# O QuestPDF precisa do fontconfig. O contrato usa "Times New Roman", que não existe em Linux;
# o fonts-liberation traz o Liberation Serif, que o fontconfig usa como equivalente.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
COPY --from=frontend /web/dist/kamba-diami-web/browser ./wwwroot

COPY backend/docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN sed -i 's/\r$//' /usr/local/bin/docker-entrypoint.sh \
    && chmod +x /usr/local/bin/docker-entrypoint.sh

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Corre como root de propósito: os volumes do Railway são montados como root.
ENTRYPOINT ["docker-entrypoint.sh"]
