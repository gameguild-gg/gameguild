#!/usr/bin/env bash

set -euo pipefail

repository="${GITHUB_REPOSITORY:-}"
ruleset_file="${RULESET_FILE:-.github/rulesets/main.json}"
apply="${APPLY_MAIN_RULESET:-false}"

if [[ -z "$repository" ]]; then
  repository=$(gh repo view --json nameWithOwner --jq .nameWithOwner)
fi

if [[ ! -f "$ruleset_file" ]]; then
  echo "Ruleset file not found: $ruleset_file" >&2
  exit 1
fi

jq -e '
  .name == "main-production" and
  .enforcement == "active" and
  (.conditions.ref_name.include | index("refs/heads/main") != null) and
  (any(.rules[]; .type == "pull_request" and .parameters.required_approving_review_count >= 1)) and
  (any(.rules[]; .type == "required_status_checks" and
    .parameters.strict_required_status_checks_policy == true and
    any(.parameters.required_status_checks[]; .context == "PR Required Gate")))
' "$ruleset_file" >/dev/null

if [[ "$apply" != 'true' ]]; then
  echo "Validated $ruleset_file for $repository. Set APPLY_MAIN_RULESET=true to apply it."
  exit 0
fi

check_seen=$(gh api "/repos/$repository/commits/main/check-runs" \
  -H 'Accept: application/vnd.github+json' \
  --jq '[.check_runs[] | select(.name == "PR Required Gate")] | length')
if [[ "$check_seen" == '0' ]]; then
  echo 'PR Required Gate has not run on main yet. Merge the pipeline PR before activating the ruleset.' >&2
  exit 1
fi

existing_id=$(gh api "/repos/$repository/rulesets" \
  --jq '.[] | select(.name == "main-production") | .id' | head -n 1)
if [[ -n "$existing_id" ]]; then
  gh api --method PUT "/repos/$repository/rulesets/$existing_id" --input "$ruleset_file" >/dev/null
  echo "Updated main-production ruleset $existing_id."
else
  gh api --method POST "/repos/$repository/rulesets" --input "$ruleset_file" >/dev/null
  echo 'Created main-production ruleset.'
fi

gh api --method PATCH "/repos/$repository" \
  -F allow_squash_merge=true \
  -F allow_merge_commit=false \
  -F allow_rebase_merge=false \
  -F delete_branch_on_merge=true >/dev/null

echo 'main now requires a current PR, one approval, resolved conversations, and PR Required Gate.'
