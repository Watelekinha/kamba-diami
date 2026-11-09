#!/bin/sh
set -e

# Os ficheiros enviados pelos utilizadores têm de sobreviver a cada novo deploy, e o Railway
# só permite um volume por serviço. Monta-se o volume em DATA_DIR e as duas pastas que a API
# usa (fotografias públicas e contratos privados) passam a ser ligações para dentro dele.
DATA_DIR="${DATA_DIR:-/data}"

mkdir -p "$DATA_DIR/uploads/photos" "$DATA_DIR/App_Data/private" /app/wwwroot
rm -rf /app/wwwroot/uploads /app/App_Data
ln -s "$DATA_DIR/uploads" /app/wwwroot/uploads
ln -s "$DATA_DIR/App_Data" /app/App_Data

# O Railway indica a porta na variável PORT.
export ASPNETCORE_URLS="http://+:${PORT:-8080}"

exec dotnet KambaDiami.Api.dll
