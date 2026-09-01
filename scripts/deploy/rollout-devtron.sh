#!/usr/bin/env bash

set -euo pipefail

require_env() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "$name is required" >&2
    exit 1
  fi
}

for name in RELEASE_SHA TREE_SHA RELEASED_AT DEVTRON_BASE_URL DEVTRON_API_TOKEN GITHUB_REPOSITORY; do
  require_env "$name"
done

RELEASE_DIR="${RELEASE_DIR:-artifacts/release}"
PROMOTED_SERVICES="$RELEASE_DIR/promoted-services.json"
PREVIOUS_MANIFEST="$RELEASE_DIR/previous-release-manifest.json"

if [[ ! -s "$PROMOTED_SERVICES" || ! -s "$PREVIOUS_MANIFEST" ]]; then
  echo "Release promotion artifacts are incomplete." >&2
  exit 1
fi

triggered_services=()

service_value() {
  local prefix="$1"
  local service="$2"
  local variable="${prefix}_${service^^}"
  printf '%s' "${!variable:-}"
}

trigger_devtron() {
  local service="$1"
  local image="$2"
  local tag="$3"
  local digest="$4"
  local release_sha="$5"
  local external_ci_id
  external_ci_id=$(service_value DEVTRON_EXTERNAL_CI_ID "$service")
  if [[ -z "$external_ci_id" ]]; then
    echo "DEVTRON_EXTERNAL_CI_ID_${service^^} is required." >&2
    return 1
  fi

  local payload="$RELEASE_DIR/devtron-${service}.json"
  node scripts/deploy/devtron-payload.mjs \
    --image "$image" \
    --tag "$tag" \
    --digest "$digest" \
    --release-sha "$release_sha" \
    --repository "https://github.com/${GITHUB_REPOSITORY}.git" \
    --commit-time "$RELEASED_AT" \
    --branch main \
    --message "release ${release_sha}" \
    --author "GitHub Actions" \
    --output "$payload"

  curl --fail --silent --show-error \
    --retry 3 --retry-all-errors --connect-timeout 10 --max-time 30 \
    -H "api-token: $DEVTRON_API_TOKEN" \
    -H 'Content-Type: application/json' \
    --data-binary "@$payload" \
    "${DEVTRON_BASE_URL%/}/orchestrator/webhook/ext-ci/${external_ci_id}"
}

verify_service() {
  local service="$1"
  local release_sha="$2"
  local tree_sha="$3"
  local digest="$4"
  local variable="GAMEGUILD_${service^^}_URL"
  local public_url="${!variable:-}"
  if [[ -z "$public_url" ]]; then
    echo "$variable is required." >&2
    return 1
  fi

  node scripts/deploy/verify-release.mjs \
    --service "$service" \
    --url "$public_url" \
    --release-sha "$release_sha" \
    --tree-sha "$tree_sha" \
    --digest "$digest" \
    --timeout-ms "${ROLLOUT_TIMEOUT_MS:-300000}"
}

rollback_service() {
  local service="$1"
  local previous
  previous=$(jq -c --arg service "$service" '.services[] | select(.service == $service)' "$PREVIOUS_MANIFEST")
  if [[ -z "$previous" ]]; then
    echo "No previous immutable release exists for $service; automatic rollback is impossible." >&2
    return 1
  fi

  local image digest previous_release previous_tree
  image=$(jq -r '.image' <<<"$previous")
  digest=$(jq -r '.imageDigest' <<<"$previous")
  previous_release=$(jq -r '.releaseSha' <<<"$previous")
  previous_tree=$(jq -r '.treeSha' <<<"$previous")
  echo "Rolling $service back to $image@$digest ($previous_release)." >&2
  trigger_devtron "$service" "$image" "release-${previous_release}" "$digest" "$previous_release"
  verify_service "$service" "$previous_release" "$previous_tree" "$digest"
}

rollback_triggered() {
  local rollback_failed=0
  for ((index=${#triggered_services[@]} - 1; index >= 0; index -= 1)); do
    rollback_service "${triggered_services[$index]}" || rollback_failed=1
  done
  return "$rollback_failed"
}

on_failure() {
  local status=$?
  trap - ERR
  if ((${#triggered_services[@]} > 0)); then
    echo "Release failed; restoring every service already triggered." >&2
    rollback_triggered || echo "CRITICAL: one or more automatic rollbacks failed." >&2
  fi
  exit "$status"
}
trap on_failure ERR

if [[ "${MIGRATION_REQUIRED:-false}" == 'true' && "${DEVTRON_API_PREDEPLOY_MIGRATIONS:-false}" != 'true' ]]; then
  echo 'This release contains migrations but the Devtron API pre-deploy migration job is not confirmed.' >&2
  exit 1
fi

for service in api web learning; do
  current=$(jq -c --arg service "$service" '.[] | select(.service == $service)' "$PROMOTED_SERVICES")
  if [[ -z "$current" ]]; then
    continue
  fi
  if ! jq -e --arg service "$service" '.services[] | select(.service == $service)' "$PREVIOUS_MANIFEST" >/dev/null; then
    echo "Previous release state has no $service digest; seed staging before enabling production." >&2
    exit 1
  fi
done

for service in api web learning; do
  current=$(jq -c --arg service "$service" '.[] | select(.service == $service)' "$PROMOTED_SERVICES")
  if [[ -z "$current" ]]; then
    continue
  fi
  image=$(jq -r '.image' <<<"$current")
  digest=$(jq -r '.imageDigest' <<<"$current")
  trigger_devtron "$service" "$image" "release-${RELEASE_SHA}" "$digest" "$RELEASE_SHA"
  triggered_services+=("$service")
  verify_service "$service" "$RELEASE_SHA" "$TREE_SHA" "$digest"
done

node scripts/deploy/production-smoke.mjs
trap - ERR
echo "Devtron rollout and production smoke completed for ${#triggered_services[@]} service(s)."
