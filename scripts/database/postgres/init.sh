#!/bin/bash

# This script is used to create the databases, schemas, and users in the PostgreSQL.
# It will exit immediately if a command exits with a non-zero status.

set -e

# Check required environment variables
if [[ -z "$POSTGRES_DB" ]]; then
  echo "The POSTGRES_DB environment variable is required."
  exit 1
fi

if [[ -z "$POSTGRES_USER" ]]; then
  echo "The POSTGRES_USER environment variable is required."
  exit 1
fi

if [[ -z "$POSTGRES_SCHEMA" ]]; then
  echo "The POSTGRES_SCHEMA environment variable is required."
  exit 1
fi

echo "Initializing PostgreSQL with:"
echo "  Database: $POSTGRES_DB"
echo "  User: $POSTGRES_USER"
echo "  Schema: $POSTGRES_SCHEMA"

# Create the database if it does not exist
echo "Creating database: $POSTGRES_DB"
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
  DO \$\$
    BEGIN
      IF NOT EXISTS (SELECT FROM pg_database WHERE datname = '$POSTGRES_DB') THEN
        CREATE DATABASE "$POSTGRES_DB";
        RAISE NOTICE 'The "$POSTGRES_DB" database has been created successfully!';
      ELSE
        RAISE NOTICE 'The "$POSTGRES_DB" database already exists.';
      END IF;
  END \$\$;
EOSQL

# Create the schema if it does not exist
echo "Creating schema: $POSTGRES_SCHEMA in database: $POSTGRES_DB"
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  DO \$\$
    BEGIN
      IF NOT EXISTS (SELECT schema_name FROM information_schema.schemata WHERE schema_name = '$POSTGRES_SCHEMA') THEN
        CREATE SCHEMA "$POSTGRES_SCHEMA";
        RAISE NOTICE 'Schema "$POSTGRES_SCHEMA" has been created successfully!';
      ELSE
        RAISE NOTICE 'Schema "$POSTGRES_SCHEMA" already exists.';
      END IF;
  END \$\$;
EOSQL

echo "Database initialization completed successfully!"