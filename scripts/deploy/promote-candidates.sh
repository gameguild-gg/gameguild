#!/usr/bin/env bash

set -euo pipefail

require_env() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "$name is required" >&2
    exit 1
  fi
}

for name in RELEASE_SHA TREE_SHA SERVICES_JSON REGISTRY_HOST REGISTRY_NAMESPACE VERIFICATION_RUN_ID RELEASED_AT; do
  require_env "$name"
done

EVIDENCE_DIR="${EVIDENCE_DIR:-candidate-evidence}"
OUTPUT_DIR="${OUTPUT_DIR:-artifacts/release}"
PREVIOUS_MANIFEST="${PREVIOUS_MANIFEST:-previous-release-manifest.json}"
MIGRATION_REQUIRED="${MIGRATION_REQUIRED:-false}"

mkdir -p "$OUTPUT_DIR"
promoted_lines="$OUTPUT_DIR/promoted-services.ndjson"
: > "$promoted_lines"

inspect_digest() {
  local reference="$1"
  docker buildx imagetools inspect "$reference" | awk '/^Digest:/ { print $2; exit }'
}

for service in api web learning; do
  if ! jq -e --arg service "$service" 'index($service) != null' <<<"$SERVICES_JSON" >/dev/null; then
    continue
  fi

  evidence_file=$(find "$EVIDENCE_DIR" -type f -name "candidate-${service}.json" -print -quit)
  if [[ -z "$evidence_file" ]]; then
    echo "Candidate evidence for $service is missing from verification run $VERIFICATION_RUN_ID." >&2
    exit 1
  fi

  image="$REGISTRY_HOST/$REGISTRY_NAMESPACE/gameguild-$service"
  candidate_tag="candidate-${TREE_SHA}-${service}"
  release_tag="release-${RELEASE_SHA}"
  candidate_reference="$image:$candidate_tag"
  release_reference="$image:$release_tag"

  jq -e \
    --arg service "$service" \
    --arg image "$image" \
    --arg treeSha "$TREE_SHA" \
    '.service == $service and .image == $image and .treeSha == $treeSha' \
    "$evidence_file" >/dev/null || {
      echo "Candidate evidence for $service does not match the classified release tree." >&2
      exit 1
    }

  evidence_digest=$(jq -r '.imageDigest' "$evidence_file")
  candidate_digest=$(inspect_digest "$candidate_reference")
  if [[ -z "$candidate_digest" || "$candidate_digest" != "$evidence_digest" ]]; then
    echo "Registry candidate digest for $service does not match verified evidence." >&2
    exit 1
  fi

  docker buildx imagetools create --tag "$release_reference" "$image@$candidate_digest"
  promoted_digest=$(inspect_digest "$release_reference")
  if [[ "$promoted_digest" != "$candidate_digest" ]]; then
    echo "Promoted digest for $service changed from $candidate_digest to $promoted_digest." >&2
    exit 1
  fi

  jq -n \
    --arg service "$service" \
    --arg image "$image" \
    --arg imageDigest "$candidate_digest" \
    --arg sourceSha "$(jq -r '.sourceSha' "$evidence_file")" \
    --arg releaseSha "$RELEASE_SHA" \
    --arg treeSha "$TREE_SHA" \
    '{service:$service,image:$image,imageDigest:$imageDigest,sourceSha:$sourceSha,releaseSha:$releaseSha,treeSha:$treeSha}' \
    >> "$promoted_lines"
done

jq -s '.' "$promoted_lines" > "$OUTPUT_DIR/promoted-services.json"

if [[ -f "$PREVIOUS_MANIFEST" ]]; then
  cp "$PREVIOUS_MANIFEST" "$OUTPUT_DIR/previous-release-manifest.json"
else
  printf '{"services":[]}\n' > "$OUTPUT_DIR/previous-release-manifest.json"
fi

jq -s '
  reduce (((.[0].services // []) + .[1])[]) as $item ({}; .[$item.service] = $item)
  | [.api, .web, .learning]
  | map(select(. != null))
' "$OUTPUT_DIR/previous-release-manifest.json" "$OUTPUT_DIR/promoted-services.json" \
  > "$OUTPUT_DIR/active-services.json"

node scripts/deploy/release-manifest.mjs \
  --release-sha "$RELEASE_SHA" \
  --tree-sha "$TREE_SHA" \
  --released-at "$RELEASED_AT" \
  --migration-required "$MIGRATION_REQUIRED" \
  --verification-run-ids "$VERIFICATION_RUN_ID" \
  --services-file "$OUTPUT_DIR/active-services.json" \
  --output "$OUTPUT_DIR/release-manifest.json"

echo "Promoted $(jq 'length' "$OUTPUT_DIR/promoted-services.json") immutable service candidate(s)."
