## Description

<!-- What does this PR do, and why? Describe the problem being solved or the
     value being added. Link any relevant discussion. -->

## Type of Change

<!-- Check the one that matches your commit message type (conventional
     commits: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`). -->

- [ ] `feat:` — new feature
- [ ] `fix:` — bug fix
- [ ] `docs:` — documentation only
- [ ] `test:` — tests only
- [ ] `refactor:` — no functional changes
- [ ] `chore:` — tooling, deps, CI
- [ ] ⚠️ Breaking change (fix or feature that breaks existing behavior)

## Related Issues

<!-- `Closes #N` resolves an issue; `References #N` is related context.
     Note: `Closes #N` does not auto-close against `develop`; the
     orchestrator closes issues manually with evidence. -->

- Closes #
- References #

## Changes Summary

<!-- Bullet points of what changed, grouped by area (api / web / packages /
     infra). Keep it scannable; the diff carries the details. -->

-

## Testing

<!-- How did you verify this change? Check all that apply and describe what
     you ran. CI executes the full test suites; local runs are compile-only
     under the current INC-PG hold. -->

- [ ] Unit tests
- [ ] Integration tests
- [ ] Manual verification (describe below)

### Test notes

-

## Checklist

<!-- PRs are not merged with failing required checks — the PR Required Gate
     (classify, secret-scan, repository-policy, web, api, testing-lab,
     openapi, migration, economy) must be green and cla-signed must pass. -->

- [ ] Commits follow conventional commits (`feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`)
- [ ] All checks pass, including the PR Required Gate and `cla-signed`
- [ ] Tests added or updated for the change; existing tests pass
- [ ] Documentation updated (docs/, README, ADRs) if behavior or API changed
- [ ] No breaking changes, or breaking change is flagged above
- [ ] If the API surface changed: OpenAPI client regenerated through the real pipeline (never hand-spliced)
- [ ] If the database schema changed: EF migration added (timestamped) and docs/schema.dbml regenerated

## Screenshots

<!-- If this PR changes the UI, attach before/after screenshots or a short
     video. Skip this section otherwise. -->
