'use client';

import React from 'react';
import type { CourseCatalogProps, CourseSummary } from '../types';

function PassThrough({ children }: { children: React.ReactNode }) {
  return <>{children}</>;
}

function DefaultGrid<TCourse extends CourseSummary>({ courses }: { courses: TCourse[] }) {
  if (!courses.length) {
    return <p className="text-slate-300">No courses available.</p>;
  }
  return (
    <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {courses.map((course, index) => (
        <li
          key={course.id ?? course.slug ?? index}
          className="rounded-lg border border-slate-700 bg-slate-800/50 p-4 text-white"
        >
          <h2 className="text-lg font-semibold">{course.title ?? 'Untitled course'}</h2>
          {course.description ? <p className="mt-2 text-sm text-slate-300">{course.description}</p> : null}
        </li>
      ))}
    </ul>
  );
}

function DefaultError({ message }: { message: string }) {
  return (
    <div className="container mx-auto px-4 py-8">
      <p className="rounded-md border border-red-500/40 bg-red-500/10 p-4 text-red-200">{message}</p>
    </div>
  );
}

const DEFAULT_LOADING = <p className="text-slate-300">Loading courses…</p>;

/**
 * Headless catalog shell. Host apps may inject the data loader, provider,
 * grid and error UI to integrate with app-specific state, or render the
 * component bare to use the built-in defaults.
 */
export function CourseCatalog<TCourse extends CourseSummary = CourseSummary>({
  initialCourses,
  loadCourses,
  Provider = PassThrough,
  Grid,
  ErrorBoundary = PassThrough,
  loadingFallback = DEFAULT_LOADING,
  ErrorComponent = DefaultError,
  title = 'Courses',
  className = 'container mx-auto px-4 py-8',
}: CourseCatalogProps<TCourse> = {}) {
  const [courses, setCourses] = React.useState<TCourse[]>(() => initialCourses ?? []);
  const [loading, setLoading] = React.useState(Boolean(loadCourses) && !(initialCourses?.length));
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (!initialCourses) {
      return;
    }

    setCourses(initialCourses);

    if (!loadCourses) {
      setLoading(false);
    }
  }, [initialCourses, loadCourses]);

  React.useEffect(() => {
    if (!loadCourses) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        const data = await loadCourses();
        if (!cancelled) setCourses(data);
      } catch (err) {
        if (!cancelled) {
          console.error('Error loading courses:', err);
          setError(err instanceof Error ? err.message : 'Failed to load courses');
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [loadCourses]);

  if (error) {
    return <ErrorComponent message={error} />;
  }

  const GridComponent = Grid ?? (DefaultGrid as React.ComponentType<{ courses: TCourse[] }>);

  return (
    <Provider initialCourses={courses}>
      <ErrorBoundary>
        <div className={className}>
          <h1 className="text-3xl font-bold mb-8 text-white">{title}</h1>
          {loading ? loadingFallback : <GridComponent courses={courses} />}
        </div>
      </ErrorBoundary>
    </Provider>
  );
}
