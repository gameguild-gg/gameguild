#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/../../.." && pwd)"
fixture_root="$(mktemp -d "${TMPDIR:-/tmp}/affected-api-tests.XXXXXX")"
trap 'rm -rf "$fixture_root"' EXIT
mkdir -p "$fixture_root/bin"

cat > "$fixture_root/bin/docker" <<'SH'
#!/usr/bin/env bash
set -euo pipefail
printf 'docker %s\n' "$*" >> "$MOCK_LOG"
case "$1" in
  run) printf 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n' ;;
  port) printf '127.0.0.1:54329\n' ;;
esac
SH
cat > "$fixture_root/bin/dotnet" <<'SH'
#!/usr/bin/env bash
set -euo pipefail
printf 'dotnet %s | template=%s\n' "$*" "${ECONOMY_POSTGRES_TEMPLATE_DATABASE:-}" >> "$MOCK_LOG"
if [[ "$1" == ef && "${MOCK_FAILURE:-}" == migration ]]; then exit 41; fi
if [[ "$1" == test ]]; then
  [[ "${MOCK_FAILURE:-}" != test ]] || exit 42
  test_name="$(basename "${2%.csproj}")"
  previous=''
  for argument in "$@"; do
    if [[ "$previous" == '--results-directory' ]]; then
      mkdir -p "$argument"
      printf '<TestRun><ResultSummary><Counters total="1" passed="1" failed="0"/></ResultSummary></TestRun>\n' \
        > "$argument/$test_name.trx"
    fi
    previous="$argument"
  done
fi
SH
chmod +x "$fixture_root/bin/docker" "$fixture_root/bin/dotnet"
export PATH="$fixture_root/bin:$PATH"
export MOCK_LOG="$fixture_root/commands.log"
export AFFECTED_DOTNET_TEST_ARTIFACTS="$fixture_root/results"
unset ECONOMY_POSTGRES_CONNECTION ECONOMY_POSTGRES_TEMPLATE_DATABASE

run_case() {
  local name="$1" expected_status="$2" failure="$3"
  shift 3
  : > "$MOCK_LOG"
  printf '%s\n' "$@" > "$fixture_root/changed-files.txt"
  export MOCK_FAILURE="$failure"
  set +e
  bash "$repository_root/scripts/ci/run-affected-dotnet-tests.sh" \
    --files-from "$fixture_root/changed-files.txt" > "$fixture_root/$name.log" 2>&1
  local status=$?
  set -e
  [[ "$status" == "$expected_status" ]] || {
    cat "$fixture_root/$name.log" >&2
    printf '%s: expected status %s, got %s\n' "$name" "$expected_status" "$status" >&2
    exit 1
  }
}

economy_source='apps/api/Source/Modules/GameGuild.Finance.Economy/Ledger/Journal.cs'
api_source='apps/api/Source/GameGuild.API/Program.cs'
run_case migrated 0 '' "$economy_source"
grep -Fq -- 'postgres:17-alpine -c max_locks_per_transaction=512' "$MOCK_LOG"
grep -Fq -- '--context ApplicationDbContext --configuration Release --no-build' "$MOCK_LOG"
grep -Eq '^dotnet test .*Finance.Economy.UnitTests.* \| template=economy_tests_template$' "$MOCK_LOG"
[[ "$(grep -c '^docker rm --force aaaa' "$MOCK_LOG")" == 1 ]]
printf 'PASS affected Economy tests use the complete migrated template and owned cleanup\n'

run_case mixed 0 '' "$api_source" "$economy_source"
grep -Eq '^dotnet test .*GameGuild.API.UnitTests.* \| template=$' "$MOCK_LOG"
grep -Eq '^dotnet test .*SharedKernel.UnitTests' "$MOCK_LOG"
[[ "$(grep -c '^docker run ' "$MOCK_LOG")" == 1 ]]
[[ "$(grep -c '^dotnet test ' "$MOCK_LOG")" == 3 ]]
printf 'PASS migration-sensitive API tests retain empty databases and every selected suite runs\n'

run_case core 0 '' "$api_source"
! grep -q '^docker ' "$MOCK_LOG"
[[ "$(grep -c '^dotnet test ' "$MOCK_LOG")" == 2 ]]
printf 'PASS ordinary core selection does not start Economy infrastructure\n'

run_case migration_failure 41 migration "$economy_source"
! grep -q '^dotnet test ' "$MOCK_LOG"
[[ "$(grep -c '^docker rm --force aaaa' "$MOCK_LOG")" == 1 ]]
printf 'PASS migration failure fails closed and cleans its owned container\n'

run_case test_failure 42 test "$economy_source"
[[ "$(grep -c '^docker rm --force aaaa' "$MOCK_LOG")" == 1 ]]
printf 'PASS test failure propagates and cleans its owned container\n'

run_case fallback 0 '' 'unused.txt'
! grep -q '^docker ' "$MOCK_LOG"
[[ "$(grep -c '^dotnet test ' "$MOCK_LOG")" == 1 ]]
printf 'PASS empty selection retains the existing API test fallback\n'
# The real selector rejects an invalid option before creating any infrastructure.
set +e
: > "$MOCK_LOG"
bash "$repository_root/scripts/ci/run-affected-dotnet-tests.sh" --invalid-option \
  > "$fixture_root/invalid-option.log" 2>&1
selector_status=$?
set -e
[[ "$selector_status" != 0 ]]
[[ ! -s "$MOCK_LOG" ]]
printf 'PASS selector errors fail before infrastructure creation\n'
