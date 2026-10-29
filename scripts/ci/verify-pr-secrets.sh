#!/usr/bin/env bash
set -Eeuo pipefail

# Run on the hosted runner, including large PRs skipped by the external scanner.
# The upstream CLI is MIT licensed and needs no dashboard API key or action license.
root="$(git -c core.commitGraph=false rev-parse --show-toplevel)"
cd "$root"
base="${SOURCE_BASE_SHA:?SOURCE_BASE_SHA is required}"
head="${SOURCE_HEAD_SHA:?SOURCE_HEAD_SHA is required}"
[[ "$base" =~ ^[a-f0-9]{40}$ && "$head" =~ ^[a-f0-9]{40}$ ]] || exit 2
git -c core.commitGraph=false cat-file -e "$base^{commit}"
git -c core.commitGraph=false merge-base --is-ancestor "$head" HEAD

evidence="$root/artifacts/secret-scan"
mkdir -p "$evidence"
checkout="$(git -c core.commitGraph=false rev-parse HEAD)"
git -c core.commitGraph=false show -s --format='%H%n%T%n%P' HEAD > "$evidence/checkout.txt"
printf '%s\n%s\n' "$base" "$head" > "$evidence/source.txt"

scanner_dir="$(mktemp -d)"
trap 'rm -rf -- "$scanner_dir"' EXIT
version=8.30.1
archive="gitleaks_${version}_linux_x64.tar.gz"
digest=551f6fc83ea457d62a0d98237cbad105af8d557003051f41f3e7ca7b3f2470eb
curl --fail --silent --show-error --location --retry 3 \
  "https://github.com/gitleaks/gitleaks/releases/download/v${version}/${archive}" \
  --output "$scanner_dir/$archive"
printf '%s  %s\n' "$digest" "$scanner_dir/$archive" | sha256sum --check --strict
tar -xzf "$scanner_dir/$archive" -C "$scanner_dir" gitleaks
printf '%s\n%s\n' "$version" "$digest" > "$evidence/scanner.txt"

# Do not inherit repository exclusions or inline allow comments. Review findings
# individually; no blanket test, generated-source or historical baseline exemption.
printf '[extend]\nuseDefault = true\n' > "$scanner_dir/config.toml"
touch "$scanner_dir/empty.ignore"
export GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.commitGraph GIT_CONFIG_VALUE_0=false
# Verify the scanner fails on a generated synthetic credential and accepts a
# harmless control. No reusable secret is stored in source, logs or artifacts.
set +e
printf 'STRIPE_KEY=sk_live_%s\n' "$(openssl rand -hex 24)" |
  "$scanner_dir/gitleaks" stdin --config "$scanner_dir/config.toml" \
    --gitleaks-ignore-path "$scanner_dir/empty.ignore" --redact=100 --no-banner --no-color
control_status=$?
set -e
if [ "$control_status" -ne 1 ]; then
  printf 'Secret scanner did not reject its synthetic positive control (exit %s).\n' "$control_status" >&2
  exit 2
fi
printf 'const applicationName = "GameGuild";\n' |
  "$scanner_dir/gitleaks" stdin --config "$scanner_dir/config.toml" \
    --gitleaks-ignore-path "$scanner_dir/empty.ignore" --redact=100 --no-banner --no-color
set +e
"$scanner_dir/gitleaks" git "$root" \
  --config "$scanner_dir/config.toml" \
  --gitleaks-ignore-path "$scanner_dir/empty.ignore" \
  --ignore-gitleaks-allow \
  --log-opts="--full-history --diff-merges=first-parent ${base}..${checkout}" \
  --redact=100 --no-banner --no-color --timeout 600 \
  --report-format json --report-path "$scanner_dir/findings.json"
scan_status=$?
set -e
# Keep only locations and detector identity in the uploaded report. Raw matches
# and secret values stay in the temporary directory even if CLI redaction changes.
node --max-old-space-size=128 - "$scanner_dir/findings.json" "$evidence/findings.json" <<'NODE'
const fs = require('node:fs');
const [input, output] = process.argv.slice(2);
if (!fs.existsSync(input)) process.exit(0);
const safe = JSON.parse(fs.readFileSync(input, 'utf8')).map(finding => ({
  RuleID: finding.RuleID, File: finding.File, StartLine: finding.StartLine,
  EndLine: finding.EndLine, Commit: finding.Commit, Fingerprint: finding.Fingerprint,
}));
fs.writeFileSync(output, JSON.stringify(safe, null, 2) + '\n');
NODE
printf '%s\n' "$scan_status" > "$evidence/exit-code.txt"
exit "$scan_status"
