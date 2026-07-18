#!/usr/bin/env bash
set -uo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ci_dir="$(cd "$script_dir/.." && pwd)"
repository_root="$(cd "$ci_dir/../.." && pwd)"

if [[ ! -f "$ci_dir/economy-gate.sh" ]]; then
  printf 'FAIL shell gate library is missing: %s\n' "$ci_dir/economy-gate.sh" >&2
  exit 1
fi

# shellcheck source=../economy-gate.sh
source "$ci_dir/economy-gate.sh"

passed=0
failed=0
fixture_root="$(mktemp -d "${TMPDIR:-/tmp}/economy-gate.XXXXXX")"
trap 'rm -rf "$fixture_root"' EXIT

assert_equal() {
  local actual="$1" expected="$2" because="$3"
  [[ "$actual" == "$expected" ]] || {
    printf "Expected '%s' but got '%s': %s\n" "$expected" "$actual" "$because" >&2
    return 1
  }
}

assert_throws() {
  local pattern="$1"
  shift
  local output status
  set +e
  output="$({ "$@"; } 2>&1)"
  status=$?
  set -e
  [[ $status -ne 0 ]] || {
    printf "Expected failure matching '%s', but command succeeded\n" "$pattern" >&2
    return 1
  }
  [[ "$output" =~ $pattern ]] || {
    printf "Expected failure matching '%s', got: %s\n" "$pattern" "$output" >&2
    return 1
  }
}

run_test() {
  local name="$1" function_name="$2"
  if "$function_name"; then
    passed=$((passed + 1))
    printf 'PASS %s\n' "$name"
  else
    failed=$((failed + 1))
    printf 'FAIL %s\n' "$name"
  fi
}

test_shell_only_ci_policy() {
  ! find "$ci_dir" -type f \( -name '*.ps1' -o -name '*.psm1' \) -print -quit | grep -q . || return 1
  ! grep -qiE 'pwsh|powershell|\.ps1|\.psm1' "$repository_root/.github/workflows/main.yml" || return 1
  grep -q '"ci:dependencies": "bash scripts/ci/install-and-audit-pnpm.sh"' "$repository_root/package.json" || return 1
  grep -q '"ci:economy": "bash scripts/ci/verify-economy.sh"' "$repository_root/package.json" || return 1
  grep -q 'pnpm install --no-lockfile --no-frozen-lockfile' "$ci_dir/install-and-audit-pnpm.sh" || return 1
  grep -q 'pnpm audit --json' "$ci_dir/install-and-audit-pnpm.sh" || return 1
  [[ ! -e "$repository_root/pnpm-lock.yaml" ]]
}

test_web_vitest_uses_direct_exec_for_json_evidence() {
  grep -q 'pnpm --filter @game-guild/web exec vitest run --reporter=json' "$ci_dir/verify-economy.sh" || return 1
  ! grep -q 'pnpm --filter @game-guild/web run test --' "$ci_dir/verify-economy.sh"
}

test_web_server_uses_direct_node_process_for_cleanup() {
  grep -Fq 'node "$repository_root/apps/web/node_modules/next/dist/bin/next" start "$repository_root/apps/web"' "$ci_dir/verify-economy.sh" || return 1
  ! grep -q 'pnpm --filter @game-guild/web exec next start' "$ci_dir/verify-economy.sh"
}

test_local_api_readiness_enables_simulation_explicitly() {
  local gate="$ci_dir/verify-economy.sh"
  local enabled_line simulation_line launch_line
  enabled_line="$(grep -n 'export PaymentGateways__Stripe__IsEnabled=true' "$gate" | cut -d: -f1)"
  simulation_line="$(grep -n 'export PaymentGateways__Stripe__UseSimulation=true' "$gate" | cut -d: -f1)"
  launch_line="$(grep -n 'dotnet "$publish_directory/GameGuild.API.dll"' "$gate" | cut -d: -f1)"

  [[ -n "$enabled_line" && -n "$simulation_line" && -n "$launch_line" ]] || return 1
  [[ "$enabled_line" -lt "$launch_line" && "$simulation_line" -lt "$launch_line" ]]
}

test_coolify_compose_forwards_stripe_gateway_identity() {
  local compose="$repository_root/compose.coolify.yaml"
  local deployment_docs="$repository_root/docs/deployment-smoke.md"

  grep -Fq -- '- PaymentGateways__Stripe__AccountId=${PaymentGateways__Stripe__AccountId:?set PaymentGateways__Stripe__AccountId}' "$compose" || return 1
  grep -Fq -- '- Billing__Stripe__AccountId=${PaymentGateways__Stripe__AccountId:?set PaymentGateways__Stripe__AccountId}' "$compose" || return 1
  grep -Fq -- '- PaymentGateways__Stripe__ConnectedAccountId=${Billing__Stripe__ConnectedAccountId:-}' "$compose" || return 1
  grep -Fq -- '- PaymentGateways__Stripe__LiveMode=${Billing__Stripe__LiveMode:?set Billing__Stripe__LiveMode to false for Staging or true for Production}' "$compose" || return 1
  grep -Fq 'PaymentGateways__Stripe__AccountId=acct_' "$deployment_docs" || return 1
  grep -Fq 'Billing__Stripe__LiveMode=false' "$deployment_docs"
}

test_published_api_uses_published_content_root() {
  grep -Fq 'dotnet "$publish_directory/GameGuild.API.dll" --contentRoot "$publish_directory"' \
    "$ci_dir/verify-economy.sh"
}

test_manifest_rejects_undeclared_project() {
  local root="$fixture_root/manifest-invalid"
  mkdir -p "$root/apps/api/Source/Modules/GameGuild.Economy"
  printf '<Project />\n' > "$root/apps/api/Source/Modules/GameGuild.Economy/GameGuild.Economy.csproj"
  printf '{"schemaVersion":1,"projects":[]}\n' > "$root/manifest.json"
  assert_throws 'not declared' assert_economy_manifest "$root" "$root/manifest.json"
}

test_manifest_accepts_declared_projects() {
  local root="$fixture_root/manifest-valid"
  local production='apps/api/Source/Modules/GameGuild.Economy/GameGuild.Economy.csproj'
  local unit='apps/api/tests/GameGuild.Economy.UnitTests/GameGuild.Economy.UnitTests.csproj'
  local integration='apps/api/tests/GameGuild.Economy.IntegrationTests/GameGuild.Economy.IntegrationTests.csproj'
  mkdir -p "$root/$(dirname "$production")" "$root/$(dirname "$unit")" "$root/$(dirname "$integration")"
  printf '<Project />\n' > "$root/$production"
  printf '<Project />\n' > "$root/$unit"
  printf '<Project />\n' > "$root/$integration"
  printf '{"schemaVersion":1,"projects":[{"productionProject":"%s","testProjects":["%s","%s"],"coverageAssemblies":["GameGuild.Economy"]}]}\n' \
    "$production" "$unit" "$integration" > "$root/manifest.json"
  assert_economy_manifest "$root" "$root/manifest.json" >/dev/null
}

test_warning_scope_finds_commerce_projects() {
  local root="$fixture_root/warning-scope"
  local production='apps/api/Source/Modules/GameGuild.Commerce.Payments/GameGuild.Commerce.Payments.csproj'
  local tests='apps/api/tests/GameGuild.Commerce.Payments.UnitTests/GameGuild.Commerce.Payments.UnitTests.csproj'
  mkdir -p "$root/$(dirname "$production")" "$root/$(dirname "$tests")"
  printf '<Project />\n' > "$root/$production"
  printf '<Project />\n' > "$root/$tests"
  local output
  output="$(get_touched_commerce_projects "$root" \
    'apps/api/Source/Modules/GameGuild.Commerce.Payments/Services/PaymentService.cs' \
    'apps/api/tests/GameGuild.Commerce.Payments.UnitTests/PaymentServiceTests.cs' \
    'apps/api/Source/Modules/GameGuild.Assets/Asset.cs')"
  grep -qx "$production" <<< "$output" || return 1
  grep -qx "$tests" <<< "$output" || return 1
  assert_equal "$(wc -l <<< "$output" | tr -d ' ')" '2' 'only owning Commerce projects are returned'
}

test_readiness_requires_consecutive_successes() {
  local attempts_file="$fixture_root/readiness-attempts"
  printf '0\n' > "$attempts_file"
  readiness_probe() {
    local attempt
    attempt="$(<"$attempts_file")"
    attempt=$((attempt + 1))
    printf '%s\n' "$attempt" > "$attempts_file"
    [[ $attempt -eq 1 || $attempt -ge 3 ]]
  }
  wait_for_consecutive_successes readiness_probe 2 4 0
  assert_equal "$(<"$attempts_file")" '4' 'a transient success must not count as stable readiness'
}

test_process_cleanup_stops_background_process() {
  sleep 30 &
  local process_id=$!

  stop_process_tree "$process_id"
  if kill -0 "$process_id" >/dev/null 2>&1; then
    kill -KILL "$process_id" >/dev/null 2>&1 || true
    wait "$process_id" >/dev/null 2>&1 || true
    return 1
  fi
}

test_trx_rejects_skips_and_empty_suites() {
  local skipped="$fixture_root/xunit-skipped.trx"
  cat > "$skipped" <<'XML'
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results><UnitTestResult testName="Skipped" outcome="NotExecuted" /></Results><ResultSummary outcome="Completed"><Counters total="2" executed="1" passed="1" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" pending="0" /></ResultSummary></TestRun>
XML
  assert_throws 'skipped or not executed' assert_trx_evidence "$skipped" || return 1

  local empty="$fixture_root/empty.trx"
  printf '<TestRun><ResultSummary><Counters total="0" executed="0" passed="0" failed="0" /></ResultSummary></TestRun>\n' > "$empty"
  assert_throws 'zero tests' assert_trx_evidence "$empty"
}

test_whole_solution_allows_only_source_empty_scaffolds() {
  local root="$fixture_root/whole-solution"
  local project='apps/api/tests/GameGuild.Localization.IntegrationTests/GameGuild.Localization.IntegrationTests.csproj'
  local project_directory="$root/$(dirname "$project")"
  local trx="$root/localization-empty.trx"
  local log="$root/dotnet-test.log"
  mkdir -p "$project_directory"
  printf '<Project Sdk="Microsoft.NET.Sdk" />\n' > "$root/$project"
  printf '<TestRun><ResultSummary><Counters total="0" executed="0" passed="0" failed="0" /></ResultSummary></TestRun>\n' > "$trx"
  printf 'Test run for %s/bin/Release/net10.0/GameGuild.Localization.IntegrationTests.dll (.NETCoreApp,Version=v10.0)\nResults File: %s\n' \
    "$project_directory" "$trx" > "$log"

  bash "$ci_dir/verify-economy.sh" --validate-whole-solution-evidence "$root" "$log" "$trx" >/dev/null || return 1

  sed -i 's/GameGuild\.Localization\.IntegrationTests/GameGuild.Unlisted.IntegrationTests/g' "$log"
  assert_throws 'zero tests' bash "$ci_dir/verify-economy.sh" \
    --validate-whole-solution-evidence "$root" "$log" "$trx" || return 1

  sed -i 's/GameGuild\.Unlisted\.IntegrationTests/GameGuild.Localization.IntegrationTests/g' "$log"
  printf 'public sealed class Tests {}\n' > "$project_directory/Tests.cs"
  assert_throws 'contains C# source' bash "$ci_dir/verify-economy.sh" \
    --validate-whole-solution-evidence "$root" "$log" "$trx"
}

test_cobertura_requires_full_method_coverage() {
  local coverage="$fixture_root/coverage.cobertura.xml"
  cat > "$coverage" <<'XML'
<coverage><packages><package name="GameGuild.Economy" line-rate="1" branch-rate="1"><classes><class name="A"><methods><method name="Covered"><lines><line number="1" hits="1" /></lines></method><method name="Missed"><lines><line number="2" hits="0" /></lines></method></methods></class></classes></package></packages></coverage>
XML
  assert_throws 'method coverage' assert_cobertura_coverage "$coverage" 'GameGuild.Economy' || return 1
  sed -i 's/number="2" hits="0"/number="2" hits="1"/' "$coverage"
  assert_cobertura_coverage "$coverage" 'GameGuild.Economy' >/dev/null
}

test_json_evidence_rejects_pending_and_skipped() {
  local vitest="$fixture_root/vitest.json" playwright="$fixture_root/playwright.json"
  printf '{"numTotalTests":3,"numPassedTests":2,"numFailedTests":0,"numPendingTests":1,"testResults":[]}\n' > "$vitest"
  printf '{"stats":{"expected":2,"unexpected":0,"skipped":1},"suites":[]}\n' > "$playwright"
  assert_throws 'pending' assert_vitest_evidence "$vitest" || return 1
  assert_throws 'skipped' assert_playwright_evidence "$playwright"
}

test_canonical_json_preserves_arrays() {
  local first="$fixture_root/first.json" second="$fixture_root/second.json"
  local first_out="$fixture_root/first.out.json" second_out="$fixture_root/second.out.json"
  printf '{"tags":["Economy"],"paths":{"/b":{},"/a":{}},"servers":[{"url":"https://example.test"}]}\n' > "$first"
  printf '{"servers":[{"url":"https://example.test"}],"paths":{"/a":{},"/b":{}},"tags":["Economy"]}\n' > "$second"
  canonicalize_json "$first" "$first_out"
  canonicalize_json "$second" "$second_out"
  cmp -s "$first_out" "$second_out" || return 1
  grep -q '"tags":\["Economy"\]' "$first_out"
}

run_test 'CI policy contains only shell scripts' test_shell_only_ci_policy
run_test 'web Vitest uses direct exec for JSON evidence' test_web_vitest_uses_direct_exec_for_json_evidence
run_test 'web server uses a directly managed Node process' test_web_server_uses_direct_node_process_for_cleanup
run_test 'local API readiness enables payment simulation explicitly' test_local_api_readiness_enables_simulation_explicitly
run_test 'Coolify forwards Stripe gateway identity and mode' test_coolify_compose_forwards_stripe_gateway_identity
run_test 'published API uses its published content root' test_published_api_uses_published_content_root
run_test 'manifest rejects undeclared Economy projects' test_manifest_rejects_undeclared_project
run_test 'manifest accepts declared Economy projects and tests' test_manifest_accepts_declared_projects
run_test 'warning scope resolves touched Commerce projects' test_warning_scope_finds_commerce_projects
run_test 'readiness requires consecutive successful probes' test_readiness_requires_consecutive_successes
run_test 'process cleanup terminates Bash background processes' test_process_cleanup_stops_background_process
run_test 'TRX evidence rejects skipped and zero-test suites' test_trx_rejects_skips_and_empty_suites
run_test 'whole-solution evidence allows only named source-empty scaffolds' test_whole_solution_allows_only_source_empty_scaffolds
run_test 'Cobertura enforces line, branch, and method coverage' test_cobertura_requires_full_method_coverage
run_test 'Vitest and Playwright reject pending or skipped tests' test_json_evidence_rejects_pending_and_skipped
run_test 'canonical JSON is deterministic and preserves arrays' test_canonical_json_preserves_arrays

printf 'Shell gate tests: %s passed, %s failed\n' "$passed" "$failed"
((failed == 0))
