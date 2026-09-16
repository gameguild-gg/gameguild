#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/../.." && pwd)"
artifact_root="${OPENAPI_ARTIFACT_ROOT:-$repository_root/artifacts/test-results/openapi}"
publish_directory="$artifact_root/publish/api"
raw_openapi="$artifact_root/openapi.raw.json"
captured_openapi="$artifact_root/openapi.json"
api_pid=''

cleanup() {
  local status=$?
  trap - EXIT INT TERM
  if [[ -n "$api_pid" ]] && kill -0 "$api_pid" 2>/dev/null; then
    kill "$api_pid" 2>/dev/null || true
    wait "$api_pid" 2>/dev/null || true
  fi
  exit "$status"
}
trap cleanup EXIT INT TERM

mkdir -p "$publish_directory"
cd "$repository_root"

dotnet publish apps/api/Source/GameGuild.API/GameGuild.API.csproj \
  -c Release \
  --nologo \
  --output "$publish_directory" \
  -p:TreatWarningsAsErrors=true

api_port="$({ node -e "const server=require('node:net').createServer(); server.listen(0,'127.0.0.1',()=>{console.log(server.address().port);server.close();});"; } | tail -n 1)"
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://127.0.0.1:$api_port"
export API_URL="http://127.0.0.1:$api_port"
export PaymentGateways__Stripe__IsEnabled=true
export PaymentGateways__Stripe__UseSimulation=true

dotnet "$publish_directory/GameGuild.API.dll" --contentRoot "$publish_directory" \
  >"$artifact_root/api.stdout.log" \
  2>"$artifact_root/api.stderr.log" &
api_pid=$!

for attempt in {1..90}; do
  if ! kill -0 "$api_pid" 2>/dev/null; then
    printf 'API exited before OpenAPI capture.\n' >&2
    cat "$artifact_root/api.stderr.log" >&2
    exit 1
  fi
  if curl --fail --silent "http://127.0.0.1:$api_port/live" >/dev/null; then
    break
  fi
  if ((attempt == 90)); then
    printf 'API did not become live before the OpenAPI timeout.\n' >&2
    exit 1
  fi
  sleep 1
done

curl --fail --silent --show-error \
  "http://127.0.0.1:$api_port/swagger/v1/swagger.json" \
  --output "$raw_openapi"
jq --sort-keys . "$raw_openapi" > "$captured_openapi"
bash "$script_dir/verify-openapi-client.sh" "$captured_openapi"
