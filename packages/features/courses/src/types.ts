import type { ComponentType, ReactNode } from 'react';

/**
 * Minimal shape required by the catalog UI shell. Host apps may pass any
 * richer course type that satisfies this interface.
 */
export interface CourseSummary {
  id?: string;
  slug?: string;
  title?: string;
  description?: string;
  thumbnail?: string;
  [key: string]: unknown;
}

export interface CourseCatalogProps<TCourse extends CourseSummary = CourseSummary> {
  /** Async loader for the catalog list. Defaults to an empty list. */
  loadCourses?: () => Promise<TCourse[]>;
  /** Provider that supplies catalog state to descendants. Defaults to a pass-through. */
  Provider?: ComponentType<{ initialCourses: TCourse[]; children: ReactNode }>;
  /** Grid component rendered when courses are ready. Defaults to a minimal list. */
  Grid?: ComponentType<{ courses: TCourse[] }>;
  /** Boundary used to catch render errors. Defaults to a pass-through. */
  ErrorBoundary?: ComponentType<{ children: ReactNode }>;
  /** Element rendered while courses are loading. Defaults to a simple message. */
  loadingFallback?: ReactNode;
  /** Component used when the loader throws. Defaults to an inline error. */
  ErrorComponent?: ComponentType<{ message: string }>;
  /** Optional heading override. */
  title?: string;
  /** Optional className applied to the outer wrapper. */
  className?: string;
}
