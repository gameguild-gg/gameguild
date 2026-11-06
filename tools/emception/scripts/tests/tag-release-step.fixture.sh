#!/usr/bin/env bash
# Local harness for the Emception "Tag the published Emception release" step.
# Extracts the release-step script from .github/workflows/emception.yml, then
# replays it against controlled remote-tag states in a throwaway repo.
set -euo pipefail

ROOT="$(mktemp -d)"
trap 'rm -rf "$ROOT"' EXIT
ORIGIN="$ROOT/origin.git"
WORK="$ROOT/work"
BIN="$ROOT/bin"

mkdir -p "$BIN" "$WORK/tools/emception/packages/core"
# Version-echo stub: the step resolves VERSION through node reading
# tools/emception/packages/core/package.json, so encode the version there.
# Version stub replacing node: the step resolves VERSION via
# `node -p ...package.json` and parses stdin JSON; emit the version field.
cat > "$BIN/node" <<'EOF'
#!/bin/sh
if [ -p /dev/stdin ]; then
  sed -n 's/.*"version": *"\([^"]*\)".*/\1/p'
else
  sed -n 's/.*"version": *"\([^"]*\)".*/\1/p' tools/emception/packages/core/package.json
fi
EOF
printf '#!/usr/bin/env bash\nexit 0\n' > "$BIN/gh"
chmod +x "$BIN/node" "$BIN/gh"

git init --bare -q "$ORIGIN"
git init -q -b main "$WORK"
git -C "$WORK" remote add origin "$ORIGIN"

set_version() { # set_version <x.y.z>
  printf '{"name":"@gameguild/emception-core","version":"%s"}\n' "$1" \
    > "$WORK/tools/emception/packages/core/package.json"
  git -C "$WORK" add tools/emception/packages/core/package.json
}
commit() { git -C "$WORK" -c user.name=t -c user.email=t@t commit --allow-empty -qm "$1"; }

set_version 1.0.0; commit "c1"
git -C "$WORK" push -q origin main
C1="$(git -C "$WORK" rev-parse HEAD)"
set_version 2.0.0; commit "c2-ships-2.0.0"
C2="$(git -C "$WORK" rev-parse HEAD)"
commit "c3-no-version-bump"
C3="$(git -C "$WORK" rev-parse HEAD)"

# Extract the step's script body: lines indented 10 spaces under 'run: |'.
REPO_ROOT="$(cd "$(dirname "$0")/../../../.." && pwd)"
STEP_FILE="$ROOT/step.sh"
awk '
  /- name: Tag the published Emception release/ {f=1}
  f && /run: \|/ {g=1; next}
  g && (/^ {10}/ || /^$/) {print substr($0, 11); next}
  g {exit}
' "$REPO_ROOT/.github/workflows/emception.yml" > "$STEP_FILE"

run_step() { # run_step <head-sha> <version>
  (cd "$WORK"
   git checkout -q --detach "$1" 2>/dev/null || true
   PATH="$BIN:$PATH" GITHUB_REPOSITORY="x/y" bash "$STEP_FILE" 2>&1) \
   | sed "s/$C1/C1/g; s/$C2/C2/g; s/$C3/C3/g"
}

core_version_at() { # core_version_at <tag> <commitish>
  git -C "$WORK" tag -f -a "$1" -m t "$2" >/dev/null
  git -C "$WORK" push -q -f origin "$1"
}

echo "== Case 1: no remote tag -> tag created at HEAD and pushed"
run_step "$C2" 2.0.0
git -C "$WORK" ls-remote origin "refs/tags/v2.0.0" | grep -q "$C2" && echo "PASS tag v2.0.0 at C2"

echo "== Case 2: same-version remote tag on older commit -> skip (routine no-changeset push)"
core_version_at v2.0.0 "$C2"
run_step "$C3" 2.0.0

echo "== Case 3: remote tag exists but its commit carries a different Emception version (e.g. reused platform v-tag) -> hard fail"
core_version_at v2.0.0 "$C1"
if run_step "$C3" 2.0.0; then echo "FAIL collision accepted"; exit 1; else echo "PASS collision rejected"; fi

echo "== Case 4: annotated remote tag for the same version, absent locally -> skip"
core_version_at v2.0.0 "$C2"
git -C "$WORK" tag -d v2.0.0 >/dev/null
run_step "$C3" 2.0.0

echo "ALL CASES PASS"
