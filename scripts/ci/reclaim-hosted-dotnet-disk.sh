#!/usr/bin/env bash
set -euo pipefail

# The full solution, NuGet cache and coverage outputs share the hosted VM's
# disk with preinstalled mobile/native SDKs. Keep .NET, Node, Docker, browsers,
# package caches and every build/test artifact; this job uses none of these
# Android, Swift or Haskell toolchains. Never clean a persistent/local runner.
if [[ "${GITHUB_ACTIONS:-}" != true || "${RUNNER_ENVIRONMENT:-}" != github-hosted || "${RUNNER_OS:-}" != Linux ]]; then
  printf 'Skipping disk reclamation outside GitHub-hosted Linux.\n'
  exit 0
fi

directories=(/usr/local/lib/android /usr/share/swift /usr/local/.ghcup /opt/ghc)
# Validate every target before the first deletion, including symlinked parents.
for directory in "${directories[@]}"; do
  if [[ "$(realpath -m -- "$directory")" != "$directory" ]]; then
    printf 'Refusing unexpected toolchain path: %s\n' "$directory" >&2
    exit 1
  fi
done

printf 'Hosted runner disk before toolchain cleanup:\n'
df -h /
for directory in "${directories[@]}"; do
  sudo rm -rf --one-file-system -- "$directory"
done
printf 'Hosted runner disk after toolchain cleanup:\n'
df -h /
