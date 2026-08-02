#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
WEB_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
REPO_ROOT="$(cd -- "${WEB_DIR}/../.." && pwd)"
RUNTIME_DIR="${TESTING_LAB_E2E_RUNTIME_DIR:-${REPO_ROOT}/.tmp/testing-lab-browser-e2e-runtime}"
ARTIFACTS_DIR="${TESTING_LAB_E2E_ARTIFACTS:-${REPO_ROOT}/.tmp/testing-lab-browser-e2e}"
POSTGRES_PORT="${TESTING_LAB_E2E_POSTGRES_PORT:-$((43000 + RANDOM % 1000))}"
API_PORT="${TESTING_LAB_E2E_API_PORT:-$((42000 + RANDOM % 1000))}"
WEB_PORT="${TESTING_LAB_E2E_WEB_PORT:-$((44000 + RANDOM % 1000))}"
POSTGRES_USER="gameguild_e2e"
POSTGRES_PASSWORD="gameguild_e2e_password"
POSTGRES_DATABASE="gameguild_e2e"
ADMIN_PASSWORD="${E2E_SYSTEM_ADMIN_PASSWORD:-Admin123!}"
RUN_ID="${TESTING_LAB_E2E_RUN_ID:-$(date +%s)-$$}"
LOCK_DIR="${TESTING_LAB_E2E_LOCK_DIR:-${REPO_ROOT}/.tmp/testing-lab-browser-e2e.lock}"
NEXT_BUILD_DIR="${WEB_DIR}/.next"
LOCK_ACQUIRED="0"
POSTGRES_CONTAINER="gameguild-testing-lab-e2e-postgres-${RUN_ID}"
API_PID=""
WEB_PID=""

mkdir -p "${RUNTIME_DIR}" "${ARTIFACTS_DIR}"

stop_process() {
  local pid="${1:-}"
  if [[ -n "${pid}" ]] && kill -0 "${pid}" >/dev/null 2>&1; then
    if [[ "$(uname -s)" == MINGW* || "$(uname -s)" == CYGWIN* ]]; then
      local win_pid
      win_pid="$(ps -W | awk -v pid="${pid}" 'NR > 1 && $1 == pid { print $4; exit }')"
      taskkill.exe //PID "${win_pid:-${pid}}" //T //F >/dev/null 2>&1 || true
    else
      kill "${pid}" >/dev/null 2>&1 || true
      wait "${pid}" >/dev/null 2>&1 || true
    fi
  fi
}

cleanup() {
  local exit_code=$?
  trap - EXIT INT TERM
  stop_process "${WEB_PID}"
  stop_process "${API_PID}"
  docker rm -f "${POSTGRES_CONTAINER}" >/dev/null 2>&1 || true
  if [[ "${LOCK_ACQUIRED}" == "1" ]]; then
    rmdir "${LOCK_DIR}" >/dev/null 2>&1 || true
  fi
  exit "${exit_code}"
}
trap cleanup EXIT INT TERM

if ! mkdir "${LOCK_DIR}" >/dev/null 2>&1; then
  echo "Another Testing Lab browser E2E run already owns ${LOCK_DIR}." >&2
  exit 1
fi
LOCK_ACQUIRED="1"

case "${NEXT_BUILD_DIR}" in
  "${WEB_DIR}/.next") ;;
  *)
    echo "Refusing to clean unexpected Next build directory: ${NEXT_BUILD_DIR}" >&2
    exit 1
    ;;
esac
rm -rf -- "${NEXT_BUILD_DIR}"

wait_for_http() {
  local url="$1"
  local label="$2"
  local log_file="$3"
  local attempts="${4:-120}"
  local process_pid="${5:-}"
  for ((attempt = 1; attempt <= attempts; attempt += 1)); do
    if [[ -n "${process_pid}" ]] && ! kill -0 "${process_pid}" >/dev/null 2>&1; then
      echo "${label} process ${process_pid} exited before readiness." >&2
      if [[ -f "${log_file}" ]]; then
        tail -n 160 "${log_file}" >&2
      fi
      return 1
    fi
    if curl --fail --silent --show-error "${url}" >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  echo "${label} did not become ready at ${url}." >&2
  if [[ -f "${log_file}" ]]; then
    tail -n 160 "${log_file}" >&2
  fi
  return 1
}

assert_port_available() {
  local port="$1"
  local label="$2"
  node -e 'const net = require("node:net"); const port = Number(process.argv[1]); const label = process.argv[2]; const server = net.createServer(); server.once("error", () => { console.error(label + " port " + port + " is already in use."); process.exit(1); }); server.listen(port, "0.0.0.0", () => server.close());' "${port}" "${label}"
}

assert_port_available "${POSTGRES_PORT}" "PostgreSQL"
assert_port_available "${API_PORT}" "GameGuild API"
assert_port_available "${WEB_PORT}" "GameGuild web"
if [[ "${TESTING_LAB_E2E_SKIP_CLIENT_BUILD:-0}" != "1" ]]; then
  echo "[testing-lab-browser-e2e] building branch-local API client"
  (cd "${REPO_ROOT}" && pnpm --filter @game-guild/client build)
fi

echo "[testing-lab-browser-e2e] starting disposable PostgreSQL"
docker run --detach --rm --name "${POSTGRES_CONTAINER}" --publish "127.0.0.1:${POSTGRES_PORT}:5432" --env "POSTGRES_USER=${POSTGRES_USER}" --env "POSTGRES_PASSWORD=${POSTGRES_PASSWORD}" --env "POSTGRES_DB=${POSTGRES_DATABASE}" postgres:16-alpine >/dev/null
for ((attempt = 1; attempt <= 60; attempt += 1)); do
  if docker exec "${POSTGRES_CONTAINER}" pg_isready --username "${POSTGRES_USER}" --dbname "${POSTGRES_DATABASE}" >/dev/null 2>&1; then
    break
  fi
  if [[ "${attempt}" -eq 60 ]]; then
    docker logs "${POSTGRES_CONTAINER}" >&2 || true
    exit 1
  fi
  sleep 1
done

CONNECTION_STRING="Host=127.0.0.1;Port=${POSTGRES_PORT};Database=${POSTGRES_DATABASE};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};Include Error Detail=true"
API_LOG="${RUNTIME_DIR}/api.log"
WEB_LOG="${RUNTIME_DIR}/web.log"
API_ENV=(
  "ASPNETCORE_ENVIRONMENT=Development"
  "ASPNETCORE_URLS=http://127.0.0.1:${API_PORT}"
  "ConnectionStrings__DefaultConnection=${CONNECTION_STRING}"
  "ConnectionStrings__AuthenticationDb=${CONNECTION_STRING}"
  "ConnectionStrings__MigrationConnection=${CONNECTION_STRING}"
  "POSTGRES_HOST=127.0.0.1"
  "POSTGRES_PORT=${POSTGRES_PORT}"
  "POSTGRES_DB=${POSTGRES_DATABASE}"
  "POSTGRES_USER=${POSTGRES_USER}"
  "POSTGRES_PASSWORD=${POSTGRES_PASSWORD}"
  "Database__RunStartupInitialization=true"
  "Database__FailStartupOnMigrationFailure=true"
  "Database__FailStartupOnSeedFailure=true"
  "Redis__Enabled=false"
  "SeedData__ImportSnapshotCourses=false"
  "Seed__AdminPassword=${ADMIN_PASSWORD}"
  "Jwt__SecretKey=testing-lab-e2e-jwt-secret-key-at-least-32-characters"
  "Authentication__JwtSecretKey=testing-lab-e2e-jwt-secret-key-at-least-32-characters"
)

echo "[testing-lab-browser-e2e] starting API on ${API_PORT}"
(
  cd "${REPO_ROOT}"
  env "${API_ENV[@]}" dotnet run --no-launch-profile --project apps/api/Source/GameGuild.API/GameGuild.API.csproj --urls "http://127.0.0.1:${API_PORT}"
) >"${API_LOG}" 2>&1 &
API_PID=$!
wait_for_http "http://127.0.0.1:${API_PORT}/ready" "GameGuild API" "${API_LOG}" 180 "${API_PID}"

WEB_ENV=(
  "NODE_ENV=development"
  "API_URL=http://127.0.0.1:${API_PORT}"
  "NEXT_PUBLIC_API_URL=http://127.0.0.1:${API_PORT}"
  "GAMEGUILD_DISABLE_WEBPACK_CACHE=1"
  "NEXT_PUBLIC_APP_URL=http://127.0.0.1:${WEB_PORT}"
  "NEXTAUTH_URL=http://127.0.0.1:${WEB_PORT}"
  "AUTH_SECRET=testing-lab-e2e-auth-secret-at-least-32-characters"
  "NEXTAUTH_SECRET=testing-lab-e2e-auth-secret-at-least-32-characters"
  "AUTH_TRUST_HOST=true"
)

echo "[testing-lab-browser-e2e] starting web on ${WEB_PORT}"
(
  cd "${WEB_DIR}"
  env "${WEB_ENV[@]}" pnpm exec next dev --webpack --hostname 127.0.0.1 --port "${WEB_PORT}"
) >"${WEB_LOG}" 2>&1 &
WEB_PID=$!
wait_for_http "http://127.0.0.1:${WEB_PORT}/api/health" "GameGuild web" "${WEB_LOG}" 180 "${WEB_PID}"

BROWSER_ENV=(
  "TESTING_LAB_E2E_DATABASE_MODE=disposable"
  "API_BASE_URL=http://127.0.0.1:${API_PORT}"
  "TESTING_LAB_E2E_BASE_URL=http://127.0.0.1:${WEB_PORT}"
  "TESTING_LAB_E2E_ARTIFACTS=${ARTIFACTS_DIR}"
  "E2E_SYSTEM_ADMIN_EMAIL=admin@game-guild.com"
  "E2E_SYSTEM_ADMIN_PASSWORD=${ADMIN_PASSWORD}"
)

echo "[testing-lab-browser-e2e] running browser journeys"
env "${BROWSER_ENV[@]}" node "${SCRIPT_DIR}/testing-lab-browser-e2e.mjs"
