#!/usr/bin/env bash
# Native processes are fixed here; the Python companion only verifies evidence.
set -Eeuo pipefail

if [[ $# != 1 || ! "$1" =~ ^[0-9a-f]{40}$ ]]; then
  printf 'Usage: credential-stepup-evidence.sh EXACT_HEAD_SHA\n' >&2
  exit 2
fi
candidate_head="$1"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$script_dir/../.."
evidence_python="${CREDENTIAL_EVIDENCE_PYTHON:-python3}"
helper="$script_dir/credential-stepup-evidence.py"
output="$PWD/artifacts/test-results/credential-stepup"
if [[ -e "$output" ]]; then
  printf 'Preserve existing credential evidence: %s\n' "$output" >&2
  exit 1
fi
if [[ "$(git rev-parse HEAD)" != "$candidate_head" || -n "$(git status --porcelain --untracked-files=no)" ]]; then
  printf 'Expected a clean exact-head checkout.\n' >&2
  exit 1
fi
if [[ -n "${ECONOMY_POSTGRES_CONNECTION:-}" || -n "${ECONOMY_POSTGRES_TEMPLATE_DATABASE:-}" ]]; then
  printf 'External PostgreSQL configuration is forbidden in this disposable test lane.\n' >&2
  exit 1
fi
mkdir -p "$output"
date -u +%FT%TZ > "$output/started-utc.txt"
git ls-files -z > "$output/tracked-files.z"
git rev-parse HEAD 'HEAD^{tree}' > "$output/checkout-before.txt"
git status --porcelain --untracked-files=no > "$output/status-before.txt"
export DOTNET_GCConserveMemory=9 DOTNET_gcServer=0
export GAMEGUILD_CREDENTIAL_STEPUP_OPENAPI_CAPTURE="$output/credential-openapi.json"
export GAMEGUILD_STEPUP_OPENAPI_CAPTURE="$output/trusted-openapi.json"
export GAMEGUILD_ORCHESTRATED_OPENAPI_CAPTURE="$output/orchestrated-openapi.json"

finish() {
  local actual_exit=$?
  trap - EXIT
  set +e
  git rev-parse HEAD 'HEAD^{tree}' > "$output/checkout-after.txt"
  git status --porcelain --untracked-files=no > "$output/status-after.txt"
  "$evidence_python" -B "$helper" source --output "$output" --label after
  "$evidence_python" -B "$helper" runtime --output "$output" --label after
  "$evidence_python" -B "$helper" finalize --output "$output" --head "$candidate_head" --exit-code "$actual_exit"
  local evidence_exit=$?
  if [[ "$actual_exit" != 0 ]]; then
    exit "$actual_exit"
  fi
  exit "$evidence_exit"
}
trap finish EXIT

run_phase() {
  local name=$1
  shift
  local started child actual_exit=0 metadata_exit=0
  started="$(date -u +%FT%TZ)"
  "$@" > "$output/$name.log" 2>&1 &
  child=$!
  "$evidence_python" -B "$helper" phase-start --output "$output" --name "$name" \
    --pid "$child" --started "$started" --command "$@" || metadata_exit=$?
  # Even a metadata error must wait for this same child; never orphan/duplicate it.
  wait "$child" || actual_exit=$?
  "$evidence_python" -B "$helper" phase-end --output "$output" --name "$name" \
    --exit-code "$actual_exit" || metadata_exit=$?
  printf '%s: exit %s\n' "$name" "$actual_exit"
  if [[ "$actual_exit" != 0 ]]; then
    return "$actual_exit"
  fi
  return "$metadata_exit"
}

"$evidence_python" -B "$helper" source --output "$output" --label before
# Retain text diagnostics, not an MSBuild binary log containing environment values.
run_phase solution-build dotnet build apps/api/GameGuild.sln -c Release --nologo -p:TreatWarningsAsErrors=true -m:2
"$evidence_python" -B "$helper" runtime --output "$output" --label before

pg_classes=(
  StepUpVerificationBudgetPostgreSqlTests
  StepUpBudgetMigrationPostgreSqlTests StepUpVerificationTransactionPostgreSqlTests
  StepUpWebAuthnBindingPostgreSqlTests StepUpWebAuthnMigrationPostgreSqlTests
  SmsSessionEvidenceMigrationPostgreSqlTests MfaRecoveryLifecyclePostgreSqlTests
  MfaSettingsEnrollmentPostgreSqlTests MfaRecoveryRateLimitPostgreSqlTests
  OrchestratedAuthenticationPostgreSqlHttpTests ApiKeyCreationStepUpPostgreSqlHttpTests
  ServiceAccountSecretStepUpPostgreSqlHttpTests
)
pg_filter=""
for class in "${pg_classes[@]}"; do
  pg_filter+="${pg_filter:+|}FullyQualifiedName~$class"
done
api_filter='FullyQualifiedName~Architecture|FullyQualifiedName~Security|FullyQualifiedName~OpenApi|FullyQualifiedName~JwtBearerValidationHttpTests|FullyQualifiedName~RateLimit'
suites=(GameGuild.Identity.Authentication.UnitTests GameGuild.Identity.Authorization.UnitTests GameGuild.API.UnitTests GameGuild.API.IntegrationTests)
failed=0
for suite in "${suites[@]}"; do
  filter=()
  case "$suite" in
    GameGuild.API.UnitTests) filter=(--filter "$api_filter");;
    GameGuild.API.IntegrationTests) filter=(--filter "$pg_filter");;
    GameGuild.Identity.Authentication.UnitTests|GameGuild.Identity.Authorization.UnitTests) filter=();;
    *) printf 'Unknown native suite: %s\n' "$suite" >&2; exit 1;;
  esac
  mkdir -p "$output/$suite"
  if run_phase "$suite" dotnet test "apps/api/tests/$suite/bin/Release/net10.0/$suite.dll" \
    --nologo --blame-hang-timeout 5m --logger 'trx;LogFileName=tests.trx' \
    --results-directory "$output/$suite" "${filter[@]}"; then
    :
  else
    failed=1
  fi
done
exit "$failed"
