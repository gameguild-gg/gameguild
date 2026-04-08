// =============================================================================
// LEARNING MODULE BARREL EXPORT
// =============================================================================
// WARNING: This barrel re-exports server-only query functions (they import auth.ts
// which uses next/headers). Only import from this barrel in Server Components.
// Client components should import types directly:
//   import type { CourseDetails } from '@/lib/learning/queries/course';
// =============================================================================

export * from './queries';
// Note: actions.ts uses 'use server' and must be imported directly
// from '@/lib/learning/actions' to avoid bundling server code in client components.
