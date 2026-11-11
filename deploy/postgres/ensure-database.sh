#!/usr/bin/env bash
set -euo pipefail

: "${TARGET_DATABASE:?TARGET_DATABASE is required}"
: "${TARGET_USER:?TARGET_USER is required}"
: "${TARGET_PASSWORD:?TARGET_PASSWORD is required}"

case "$TARGET_DATABASE" in
    postgres|template0|template1)
        echo "A service must use its own database, not a PostgreSQL system database." >&2
        exit 1
        ;;
esac

psql --no-psqlrc --set=ON_ERROR_STOP=1 <<'SQL'
\getenv target_database TARGET_DATABASE
\getenv target_user TARGET_USER
\getenv target_password TARGET_PASSWORD

-- Never reuse an administrator or an existing privileged role for a service.
SELECT EXISTS (
    SELECT FROM pg_roles
    WHERE rolname = :'target_user'
      AND (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)
) OR :'target_user' = current_user AS privileged_role
\gset
\if :privileged_role
    DO $$ BEGIN
        RAISE EXCEPTION 'The service role must be separate from the administrator and have no elevated privileges.';
    END $$;
\endif

SELECT format('CREATE ROLE %I LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS', :'target_user')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = :'target_user')
\gexec

SELECT format('ALTER ROLE %I LOGIN PASSWORD %L', :'target_user', :'target_password')
\gexec

SELECT format('CREATE DATABASE %I OWNER %I', :'target_database', :'target_user')
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = :'target_database')
\gexec

SELECT format('ALTER DATABASE %I OWNER TO %I', :'target_database', :'target_user')
\gexec
SELECT format('REVOKE ALL ON DATABASE %I FROM PUBLIC', :'target_database')
\gexec
SQL

psql --no-psqlrc --set=ON_ERROR_STOP=1 --dbname="$TARGET_DATABASE" <<'SQL'
\getenv target_user TARGET_USER

BEGIN;
SELECT format('ALTER SCHEMA public OWNER TO %I', :'target_user')
\gexec
REVOKE CREATE ON SCHEMA public FROM PUBLIC;

COMMIT;
SQL
