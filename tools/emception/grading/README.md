# Frozen Code grading toolchains

An adapter version keeps its exact executable identity in frozen assessment
projections. A package release does not rewrite previously frozen identities.

| Adapter | Artifact | Runtime ABI | Worker asset configuration |
| --- | --- | --- | --- |
| 1 | 4.4.0 | emception-browser-v1 | Version1RuntimeDirectory / Version1CdnDirectory |
| 2 | 4.5.1 | emception-browser-v1 | Version2RuntimeDirectory / Version2CdnDirectory |
| 3 (current authoring) | 4.5.2 | emception-browser-v1 | RuntimeDirectory / CdnDirectory |

The identities also bind the reviewed toolchain lock hash. Versions 2 and 3
share that hash and ABI while retaining distinct artifact versions. The worker
rejects cross-version manifests before executing submitted code.

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
