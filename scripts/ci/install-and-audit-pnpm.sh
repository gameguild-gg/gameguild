#!/usr/bin/env bash
set -Eeuo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/../.." && pwd)"

# shellcheck source=economy-gate.sh
source "$script_dir/economy-gate.sh"

audit_root="$repository_root/artifacts/test-results/pnpm-audit"
audit_lock="$audit_root/pnpm-lock.yaml"
audit_report="$audit_root/audit.json"
root_lock="$repository_root/pnpm-lock.yaml"
virtual_store_lock="$repository_root/node_modules/.pnpm/lock.yaml"

[[ ! -e "$root_lock" ]] || economy_gate_error "The repository pnpm lockfile must remain absent: $root_lock"

mkdir -p "$audit_root"
rm -f "$audit_lock" "$audit_report"

cd "$repository_root"
export CI=true

printf '> pnpm install --no-lockfile --no-frozen-lockfile\n'
pnpm install --no-lockfile --no-frozen-lockfile
[[ ! -e "$root_lock" ]] || economy_gate_error 'pnpm install unexpectedly created the intentionally absent repository lockfile'
[[ -f "$virtual_store_lock" ]] || economy_gate_error "pnpm install did not produce its virtual-store resolution: $virtual_store_lock"

# pnpm audit requires a lockfile. The freshly resolved virtual-store lock is copied
# only into the asserted artifact tree; the repository root remains lock-free.
cp "$virtual_store_lock" "$audit_lock"
[[ ! -e "$root_lock" ]] || economy_gate_error 'pnpm audit preparation unexpectedly created the repository lockfile'

export npm_config_lockfile_dir="$audit_root"
printf '> pnpm audit --json\n'
set +e
pnpm audit --json >"$audit_report" 2>&1
audit_exit_code=$?
set -e

[[ ! -e "$root_lock" ]] || economy_gate_error 'pnpm audit unexpectedly created the repository lockfile'

advisory_count="$($PYTHON_BIN - "$audit_report" <<'PY'
import json
import sys

path = sys.argv[1]
try:
    with open(path, encoding="utf-8") as handle:
        audit = json.load(handle)
except Exception as error:
    raise SystemExit(f"pnpm audit did not produce valid JSON evidence at '{path}': {error}")

vulnerabilities = audit.get("metadata", {}).get("vulnerabilities", {})
print(sum(int(vulnerabilities.get(level, 0)) for level in ("info", "low", "moderate", "high", "critical")))
PY
)"

if ((advisory_count > 0)); then
  economy_gate_error "pnpm audit found $advisory_count advisories; see $audit_report"
fi
if ((audit_exit_code != 0)); then
  economy_gate_error "pnpm audit failed with exit code $audit_exit_code; see $audit_report"
fi
