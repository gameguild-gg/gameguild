#!/usr/bin/env bash

set -euo pipefail

readonly max_inline_bytes=$((5 * 1024 * 1024))
failed=0

report() {
  printf 'repository-hygiene: %s\n' "$*" >&2
  failed=1
}

while IFS= read -r path; do
  case "${path}" in
    .next/*|*/.next/*|node_modules/*|*/node_modules/*|.turbo/*|*/.turbo/*)
      report "generated cache is tracked: ${path}"
      ;;
    TestResults/*|*/TestResults/*|coverage/*|*/coverage/*|test-results/*|*/test-results/*|playwright-report/*|*/playwright-report/*)
      report "generated test output is tracked: ${path}"
      ;;
    temp/*|LEGACY/*|old/*)
      report "legacy snapshot is tracked: ${path}"
      ;;
    package-lock.json|*/package-lock.json|pnpm-lock.yaml|*/pnpm-lock.yaml|yarn.lock|*/yarn.lock)
      report "ignored package-manager lock file is tracked: ${path}"
      ;;
    contributors/gource.txt|contributors/gource.mp4|contributors/gource.gif)
      report "generated Gource output is tracked: ${path}"
      ;;
    apps/web/public/wasm/*|tools/emception/public/cdn/*|tools/emception/public/c2w-net-proxy.wasm|tools/emception/logs)
      report "generated browser runtime output is tracked: ${path}"
      ;;
    *.wasm|*.wasm.gz|*.tar|*.tar.gz|*.zip|*.mp4)
      report "generated binary artifact is tracked: ${path}"
      ;;
    *.ps1|*.psm1)
      report "PowerShell automation is not allowed; use a shell script: ${path}"
      ;;
  esac
done < <(git ls-files)

while IFS=' ' read -r blob_size path; do
  if (( blob_size > max_inline_bytes )); then
    report "large files are not allowed in Git (${blob_size} bytes): ${path}"
  fi
done < <(
  git ls-files -s \
    | sed -E 's/^[0-9]+ ([0-9a-f]+) [0-9]+\t/\1 /' \
    | git cat-file --batch-check='%(objectsize) %(rest)'
)

if (( failed != 0 )); then
  exit 1
fi

printf 'repository-hygiene: tracked files satisfy repository policy\n'