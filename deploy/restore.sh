#!/usr/bin/env bash
# Restaura um backup no banco da instalação. Substitui TODOS os dados atuais: exige --yes.
# Uso: deploy/restore.sh --yes deploy/backups/organizali-AAAAMMDDTHHMMSSZ.sql.gz[.enc]
set -euo pipefail
if [ "${1:-}" != "--yes" ] || [ -z "${2:-}" ]; then echo "Uso: restore.sh --yes <arquivo de backup>" >&2; exit 2; fi
file="$(realpath "$2")"
cd "$(dirname "$0")"
ENV_FILE="${ENV_FILE:-.env.production}"
compose() { docker compose -f compose.production.yml --env-file "$ENV_FILE" "$@"; }
passphrase="$(grep -E '^ORGANIZALI_BACKUP_PASSPHRASE=' "$ENV_FILE" | cut -d= -f2- || true)"
source_stream() {
  if [[ "$file" == *.enc ]]; then BACKUP_PASSPHRASE="$passphrase" openssl enc -d -aes-256-cbc -pbkdf2 -pass env:BACKUP_PASSPHRASE < "$file" | gunzip
  else gunzip < "$file"; fi
}
# Confere que o arquivo abre (senha correta e gzip íntegro) antes de apagar qualquer dado.
source_stream > /dev/null
compose stop api frontend
compose exec -T database sh -c 'exec mariadb -uroot -p"$MARIADB_ROOT_PASSWORD" -e "DROP DATABASE IF EXISTS organizali; CREATE DATABASE organizali CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci; GRANT ALL ON organizali.* TO \"organizali\"@\"%\";"'
source_stream | compose exec -T database sh -c 'exec mariadb -uroot -p"$MARIADB_ROOT_PASSWORD" organizali'
compose up -d api frontend
echo "Restaurado: $file"
