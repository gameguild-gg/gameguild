Evidence notes for the gate:

- The earlier OpenAPI client consistency failure was drift absorbed from develop, not from this PR's worker fixes: PR #1275 (commit 1e7b8d8cd) changed EnrollUserCommand and the course/user enrollment list queries to Result-wrapped returns, whose response schemas ApiExplorer can no longer infer. The client regenerated from this PR's own verify-run evidence artifact in 7128b7c85 (pipeline regeneration, never hand-spliced). Follow-up worth filing: restoring typed 200s for those three enrollment endpoints on develop.
- Codacy is the known always-ACTION_REQUIRED non-required lane.
- Local verification through the serialized build gate: full API build 0 errors/0 warnings; Notifications 530/530, Assessments 537/537, Features 869/869, Payments fraud suite 7/7, Core FeatureFlagSetupTests 1/1. The Payments full suite shows 4 machine-locale/line-ending flakes (CSV CRLF splits, pt-BR decimal formatting) verified identical on the pre-change tree; they do not touch changed code.
- Migration compatibility passed on the first CI run (snapshot clean, clean-DB apply, dbml agree).
