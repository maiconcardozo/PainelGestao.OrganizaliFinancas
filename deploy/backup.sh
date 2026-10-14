#!/usr/bin/env bash
# Backup consistente do MariaDB (single-transaction), compactado e, se ORGANIZALI_BACKUP_PASSPHRASE existir, cifrado.
# Uso: deploy/backup.sh            (ENV_FILE=deploy/.env.production por padrão; BACKUP_DIR=deploy/backups)
set -euo pipefail
cd "$(dirname "$0")"
ENV_FILE="${ENV_FILE:-.env.production}"
BACKUP_DIR="${BACKUP_DIR:-backups}"
compose() { docker compose -f compose.production.yml --env-file "$ENV_FILE" "$@"; }
passphrase="$(grep -E '^ORGANIZALI_BACKUP_PASSPHRASE=' "$ENV_FILE" | cut -d= -f2- || true)"
mkdir -p "$BACKUP_DIR"
file="$BACKUP_DIR/organizali-$(date -u +%Y%m%dT%H%M%SZ).sql.gz"
dump() { compose exec -T database sh -c 'exec mariadb-dump --single-transaction --routines --triggers --hex-blob -uroot -p"$MARIADB_ROOT_PASSWORD" organizali'; }
if [ -n "$passphrase" ]; then
  file="$file.enc"
  dump | gzip | BACKUP_PASSPHRASE="$passphrase" openssl enc -aes-256-cbc -pbkdf2 -salt -pass env:BACKUP_PASSPHRASE > "$file"
  BACKUP_PASSPHRASE="$passphrase" openssl enc -d -aes-256-cbc -pbkdf2 -pass env:BACKUP_PASSPHRASE < "$file" | gzip -t
else
  dump | gzip > "$file"
  gzip -t "$file"
fi
# Comprovantes (volume privado da API): um tar por backup, cifrado com a mesma senha quando houver.
attachments="$BACKUP_DIR/organizali-attachments-$(date -u +%Y%m%dT%H%M%SZ).tar.gz"
tar_volume() { docker run --rm -v "$(compose config --format json | python3 -c 'import json,sys;print(json.load(sys.stdin)["name"])' 2>/dev/null || echo organizali)_organizali_attachments:/data:ro" alpine tar -czf - -C /data .; }
if [ -n "$passphrase" ]; then
  attachments="$attachments.enc"
  tar_volume | BACKUP_PASSPHRASE="$passphrase" openssl enc -aes-256-cbc -pbkdf2 -salt -pass env:BACKUP_PASSPHRASE > "$attachments" || true
else
  tar_volume > "$attachments" || true
fi
echo "$file"
echo "$attachments"
