# Frozen Code grading toolchains

An adapter version keeps its exact executable identity in frozen assessment
projections. A package release does not rewrite previously frozen identities.

| Adapter | Artifact | Runtime ABI | Worker asset configuration |
| --- | --- | --- | --- |
| 1 | 4.4.0 | emception-browser-v1 | Version1RuntimeDirectory / Version1CdnDirectory |
| 2 (historical snapshots) | 4.4.1, 4.5.0, 4.5.1, 4.5.2 | emception-browser-v1 | Exact retained release profile; 4.5.2 shares the identical current artifact |
| 3 (current authoring) | 4.5.2 | emception-browser-v1 | RuntimeDirectory / CdnDirectory |

The identities also bind the reviewed toolchain lock hash. Git history shows
that adapter 2 was rebound to all four listed releases. Its frozen snapshots
therefore retain their own exact artifact identity; they are never rewritten
to the canonical 4.5.1 binding. Unknown releases, changed locks and changed
ABIs remain rejected. Adapter 3 requires the exact 4.5.2 identity.

`HistoricalToolchains` maps a retained artifact version to its deployment-owned
`RuntimeDirectory` and `CdnDirectory`. The complete identity is validated before
looking up this configuration, so adding an unknown map entry cannot authorize
an unsupported compiler. Existing Version1 directory options remain supported;
Version2RuntimeDirectory / Version2CdnDirectory select the retained 4.5.1 assets.
The historically issued V2/4.5.2 identity equals V3's current executable identity
and can use those exact current assets; this is identity equality, not fallback.
The review handler still requires the frozen adapter version from the snapshot.

Deployments must retain each historical version's runtime bundle and CDN and
configure its own absolute directories before upgrading the current runtime.
Missing historical assets fail closed; a historical request never falls back
to the current compiler or becomes a zero grade. Registering these directory
options does not establish that historical assets have been deployed.

Hosted API contracts cover old frozen projections, versioned adapter and review
handler resolution, separate asset selection, and rejection of incompatible
identities. Native assessment execution is required for accepting the current
runtime. Validation output does not automatically promote the canonical Quiz
and Grading capability snapshot.
