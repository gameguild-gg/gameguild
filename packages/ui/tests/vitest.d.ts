import type { TestingLibraryMatchers } from '@testing-library/jest-dom/matchers';
import 'vitest';

declare module 'vitest' {
  interface Matchers<R extends void | Promise<void> = void | Promise<void>, T = unknown> {
    toHaveAccessibleDescription: TestingLibraryMatchers<T, R>['toHaveAccessibleDescription'];
    toBeInTheDocument: TestingLibraryMatchers<T, R>['toBeInTheDocument'];
  }
}
