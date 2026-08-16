import type React from 'react';
import type { ReactNode } from 'react';

/**
 * The single page-container standard for every app shell (AppShell sections,
 * ConsoleShell, WorkspaceShell, LegalShell). All layouts align their content
 * column to the same width and gutters.
 */
export const CONTAINER_CLASS = 'mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8';

export function Container({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}): React.JSX.Element {
  return <div className={className ? `${CONTAINER_CLASS} ${className}` : CONTAINER_CLASS}>{children}</div>;
}
