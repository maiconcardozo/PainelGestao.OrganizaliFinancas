#!/usr/bin/env bash
# Gera as imagens da versão atual (tag = commit e latest) e, se ORGANIZALI_REGISTRY estiver definido, envia ao registro.
# Uso: deploy/publish.sh   — ex.: ORGANIZALI_REGISTRY=ghcr.io/usuario/ deploy/publish.sh
set -euo pipefail
cd "$(dirname "$0")/.."
version="${ORGANIZALI_VERSION:-$(git rev-parse --short HEAD)}"
if [ -z "${ORGANIZALI_VERSION:-}" ] && [ -n "$(git status --porcelain)" ]; then
  echo "Há alterações não commitadas; a imagem não corresponderia ao commit $version." >&2; exit 1
fi
docker build -f src/PainelGestao.OrganizaliFinancas.Api/Dockerfile -t "organizali-api:$version" -t organizali-api:latest .
docker build -t "organizali-frontend:$version" -t organizali-frontend:latest frontend
if [ -n "${ORGANIZALI_REGISTRY:-}" ]; then
  for image in organizali-api organizali-frontend; do
    docker tag "$image:$version" "${ORGANIZALI_REGISTRY}$image:$version"
    docker push "${ORGANIZALI_REGISTRY}$image:$version"
  done
fi
echo "Imagens organizali-api:$version e organizali-frontend:$version prontas."
