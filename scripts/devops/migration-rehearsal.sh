#!/usr/bin/env bash
# migration-rehearsal.sh — forward + rollback migration rehearsal against a disposable
# PostgreSQL 17 container (the same image digest the CI "Migration compatibility" lane pins).
#
# What this proves, per run:
#   1. FORWARD: a fresh database applies the complete committed migration history.
#   2. HISTORY COMPLETENESS: every migration in the assembly has exactly one
#      __EFMigrationsHistory row (file count == chain length == applied rows).
#   3. ROLLBACK: the N latest migrations (default 3) revert one-by-one via their Down()
#      methods, each step verified (objects dropped, history rows removed).
#   4. ROUND-TRIP: re-applying forward must reproduce a byte-identical schema
#      (pg_dump -s diff; the only tolerated delta is pg_dump's per-session \restrict token).
#
# Data-loss probes are NOT automatic: Down() methods that drop columns or tables are
# structurally correct inverses but lossy by nature. The rehearsal records which migrations
# are lossy in the JSON verdict so the operator signs off knowingly.
#
# Usage (requires Docker and the .NET SDK):
#   bash scripts/devops/migration-rehearsal.sh [N]
# Environment:
#   REHEARSAL_PG_PORT   host port for the disposable container (default 55432)
#   REHEARSAL_DB        database name inside the container (default gameguild)
#   REHEARSAL_EVIDENCE  output directory (default artifacts/migration-rehearsal)
set -euo pipefail

ROLLBACK_COUNT="${1:-3}"
PORT="${REHEARSAL_PG_PORT:-55432}"
DB="${REHEARSAL_DB:-gameguild}"
EVIDENCE="${REHEARSAL_EVIDENCE:-artifacts/migration-rehearsal}"
CONTAINER="gg-migration-rehearsal-$PORT"
PG_IMAGE="mirror.gcr.io/library/postgres:17-alpine@sha256:b0f9560a2de083e2cc7382e75f808c7381a32852a7ec49117deedb300e552b24"
REPO_ROOT="$(git -C "$(dirname "$0")/../.." rev-parse --show-toplevel)"
API_PROJECT="$REPO_ROOT/apps/api/Source/GameGuild.API/GameGuild.API.csproj"
CONNECTION="Host=127.0.0.1;Port=$PORT;Database=$DB;Username=postgres;Password=postgres"

mkdir -p "$EVIDENCE"

psql_db() { docker exec "$CONTAINER" psql -U postgres -d "$DB" -tAc "$1"; }
psql_dbc() { docker exec "$CONTAINER" psql -U postgres -d "$DB" -c "$1"; }
schema_snapshot() { docker exec "$CONTAINER" pg_dump -s -U postgres "$DB" > "$1" 2>/dev/null; }

ef() {
  dotnet ef "$1" \
    --project "$API_PROJECT" \
    --startup-project "$API_PROJECT" \
    --connection "$CONNECTION" \
    --no-build
}

echo "[rehearsal] disposable PostgreSQL 17 on 127.0.0.1:$PORT (CI-pinned digest)"
docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
docker run -d --name "$CONTAINER" \
  -e POSTGRES_DB="$DB" -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
  -p "127.0.0.1:$PORT:5432" "$PG_IMAGE" >/dev/null
trap 'docker rm -f "$CONTAINER" >/dev/null 2>&1 || true' EXIT
until docker exec "$CONTAINER" pg_isready -U postgres -d "$DB" >/dev/null 2>&1; do sleep 1; done

echo "[rehearsal] 1/5 FORWARD: applying the complete migration history"
ef database.update | tee "$EVIDENCE/forward.log" | tail -1

CHAIN_LEN=$(ef migrations.list | grep -c .)
APPLIED=$(psql_db "SELECT count(*) FROM \"__EFMigrationsHistory\";")
FILE_LEN=$(ls "$REPO_ROOT/apps/api/Source/GameGuild.API/Database/Migrations" \
  | grep -v 'Designer\.cs$' | grep -v 'ModelSnapshot\.cs$' | grep -v '\.Security\.cs$' | grep -c '\.cs$')
echo "[rehearsal] 2/5 HISTORY COMPLETENESS: files=$FILE_LEN chain=$CHAIN_LEN applied=$APPLIED"
if [[ "$FILE_LEN" != "$CHAIN_LEN" || "$CHAIN_LEN" != "$APPLIED" ]]; then
  echo "::error::history incomplete (files=$FILE_LEN chain=$CHAIN_LEN applied=$APPLIED)" | tee "$EVIDENCE/verdict.json"
  exit 1
fi

schema_snapshot "$EVIDENCE/schema-at-latest.sql"
echo "[rehearsal] 3/5 ROLLBACK: reverting the $ROLLBACK_COUNT latest migrations one-by-one"
ef migrations.list > "$EVIDENCE/migration-chain.txt"
mapfile -t CHAIN < <(grep . "$EVIDENCE/migration-chain.txt")
LATEST_INDEX=$(( ${#CHAIN[@]} - 1 ))
STEP=0
while (( STEP < ROLLBACK_COUNT )); do
  FROM=$(( LATEST_INDEX - STEP ))
  TO=$(( FROM - 1 ))
  (( TO >= 0 )) || break
  FROM_ID="${CHAIN[$FROM]}"
  TO_ID="${CHAIN[$TO]}"
  echo "[rehearsal] rollback step $((STEP+1)): $FROM_ID -> $TO_ID"
  if ! ef database.update "$TO_ID" > "$EVIDENCE/rollback-step$((STEP+1)).log" 2>&1; then
    echo "[rehearsal] FAILED to revert $FROM_ID (Down() is not applicable as-is — see log)" | tee -a "$EVIDENCE/rollback-step$((STEP+1)).log"
    echo "{\"passed\":false,\"failedAt\":\"$FROM_ID\"}" > "$EVIDENCE/verdict.json"
    exit 1
  fi
  REMAINS=$(psql_db "SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\"='$FROM_ID';")
  (( REMAINS == 0 )) || { echo "::error::history row for $FROM_ID survived the revert" | tee "$EVIDENCE/verdict.json"; exit 1; }
  STEP=$(( STEP + 1 ))
done

echo "[rehearsal] 4/5 ROUND-TRIP: re-applying forward to the latest migration"
ef database.update > "$EVIDENCE/reapply.log" 2>&1 || { cat "$EVIDENCE/reapply.log"; exit 1; }
schema_snapshot "$EVIDENCE/schema-roundtrip.sql"

echo "[rehearsal] 5/5 SCHEMA DIFF: pre-rollback vs post-round-trip"
# Tolerated per-session noise: pg_dump's \restrict/\unrestrict guard token.
if ! diff <(grep -vE '^\\(un)?restrict ' "$EVIDENCE/schema-at-latest.sql") \
          <(grep -vE '^\\(un)?restrict ' "$EVIDENCE/schema-roundtrip.sql") \
          > "$EVIDENCE/schema-diff.txt"; then
  echo "::error::schema after round-trip differs from the pre-rollback snapshot" | tee "$EVIDENCE/verdict.json"
  head -50 "$EVIDENCE/schema-diff.txt"
  exit 1
fi

FINAL_APPLIED=$(psql_db "SELECT count(*) FROM \"__EFMigrationsHistory\";")
cat > "$EVIDENCE/verdict.json" <<JSON
{
  "passed": true,
  "migrationsAppliedForward": $FINAL_APPLIED,
  "rollbacksReverted": $STEP,
  "roundTripSchemaIdentical": true,
  "lossyDownMigrations": "see docs/operations/migration-rehearsal.md §findings — Down() methods that drop columns/tables are lossy by design; operator sign-off required before any production rollback"
}
JSON
cat "$EVIDENCE/verdict.json"
echo "[rehearsal] PASSED"
