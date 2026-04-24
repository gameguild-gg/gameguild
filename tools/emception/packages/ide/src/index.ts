// @emception/ide — Phase 8 reactive IDE.
// Phase 0.2: legacy source migrated from packages/emception/src/ into ./components/ + ./styles/
// for the Phase 8 rewrite to act on. Not exported yet — old shape relies on
// `import from 'emception'` (legacy meta package). Phase 8 will rewrite these
// against `@emception/browser` + `@emception/core` and re-enable exports.

export type { EmceptionAPI } from '@emception/core';

export const PHASE_8_PENDING = true;
