#!/bin/sh
# Runs once, when the data volume is first initialised.
# POSTGRES_USER owns the schema (used by migrations). app_runtime may only read/write data.
set -eu

psql -v ON_ERROR_STOP=1 -v app_password="$APP_DB_PASSWORD" \
     --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<'EOSQL'
CREATE ROLE app_runtime LOGIN PASSWORD :'app_password';
GRANT CONNECT ON DATABASE :"DBNAME" TO app_runtime;
GRANT USAGE ON SCHEMA public TO app_runtime;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO app_runtime;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO app_runtime;
EOSQL
