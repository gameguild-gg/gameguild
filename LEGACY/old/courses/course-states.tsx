'use client';

import React from 'react';

export function CourseLoading() {
  return (
    <div className="container mx-auto px-4 py-8">
      <div className="animate-pulse">
        <div className="h-8 bg-gray-200 rounded w-1/3 mx-auto mb-4"></div>
        <div className="h-4 bg-gray-200 rounded w-1/2 mx-auto mb-12"></div>
        <div className="flex gap-2 mb-8">
          <div className="h-10 bg-gray-200 rounded w-[180px]"></div>
          <div className="h-10 bg-gray-200 rounded w-[180px]"></div>
          <div className="h-10 bg-gray-200 rounded w-[180px]"></div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {Array(6)
            .fill(0)
            .map((_, i) => (
              <div key={i} className="h-64 bg-gray-200 rounded"></div>
            ))}
        </div>
      </div>
    </div>
  );
}

interface CourseErrorProps {
  message: string;
  onRetry?: () => void;
}

export function CourseError({ message, onRetry }: CourseErrorProps) {
  return (
    <div className="container mx-auto px-4 py-8">
      <div className="text-center">
        <div className="mb-4">
          <svg className="w-16 h-16 text-red-500 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"
            />
          </svg>
        </div>
        <h2 className="text-2xl font-bold text-gray-900 mb-2">Something went wrong</h2>
        <p className="text-gray-600 mb-6">{message}</p>
        {onRetry && (
          <button
            onClick={onRetry}
            className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
          >
            Try again
          </button>
        )}
      </div>
    </div>
  );
}

export function EmptyCourseState() {
  return (
    <div className="text-center py-10 text-muted-foreground">
      <div className="mb-4">
        <svg className="w-16 h-16 text-gray-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M9.172 16.172a4 4 0 015.656 0M9 12h6m-6-4h6m2 5.291A7.962 7.962 0 0112 15c-2.34 0-4.5-1.01-6-2.709M15 3H9L4 8v8a2 2 0 002 2h12a2 2 0 002-2V8l-5-5z"
          />
        </svg>
      </div>
      <h3 className="text-lg font-medium text-gray-900 mb-2">No courses found</h3>
      <p className="text-gray-500">Try adjusting your filters to see more results.</p>
    </div>
  );
}

// Export CourseStates object for easier access
export const CourseStates = {
  Loading: CourseLoading,
  Error: CourseError,
  Empty: EmptyCourseState,
};
