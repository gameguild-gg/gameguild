CI revival update:

- Rebased onto current `develop` (merge commit; branch now contains develop, satisfying the up-to-date requirement) and retargeted the PR base `main` -> `develop`, matching where all current integration PRs land.
- Root cause of the red lanes (Sign-in rendering x4, Hot-path load smoke, Session state browser): `Unmitigated dependency advisory: GHSA-474x-hrwr-fgx4` — this branch's lockfile predated the advisory pin that develop already carries in `pnpm-workspace.yaml` overrides. The merge brings the pinned lockfile; no dependency edits were needed here.
- The stuck/queued `Classify release surfaces` + `Repository policy` lanes were runner starvation on the previous head (24h timeouts); the new head triggers a fresh run.
- The merge also retired the main-lineage strict step-up/MFA surfaces that develop superseded with the 423 `IStepUpRequiredResponder` pattern (#1361): the Identity.Authentication module and its unit tests now match develop exactly; this PR's own changes (remember-me defaults, 30d refresh lifetime, last-week renewal, sign-in redirects, and their tests) are preserved on top.
- Codacy/GitGuardian/pr_agent noise is out of scope per standing directive.

Solution builds clean locally (0 warnings, 0 errors) via the build gate.
