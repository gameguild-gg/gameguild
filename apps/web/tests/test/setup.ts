import '@testing-library/jest-dom';
import { cleanup } from '@testing-library/react';
import { afterEach, beforeAll, jest } from '@jest/globals';

// Clean up after each test
afterEach(() => {
  cleanup();
});

// Mock Next.js modules
beforeAll(() => {
  // Mock next/navigation
  jest.mock('next/navigation', () => ({
    useRouter: () => ({
      push: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
      back: jest.fn(),
      forward: jest.fn(),
      refresh: jest.fn(),
    }),
    usePathname: () => '/test-path',
    useSearchParams: () => new URLSearchParams(),
    useParams: () => ({}),
    notFound: jest.fn(),
    redirect: jest.fn(),
  }));

  // Mock next/image
  jest.mock('next/image', () => ({
    __esModule: true,
    default: (props: Record<string, unknown>) => props,
  }));

  // Mock next/link
  jest.mock('next/link', () => ({
    __esModule: true,
    default: ({ children, href, ...props }: { children: React.ReactNode; href: string; [key: string]: unknown }) => ({
      ...props,
      href,
      children,
    }),
  }));

  // Mock authentication
  jest.mock('@/auth.ts', () => ({
    auth: jest.fn(() => Promise.resolve(null)),
    signIn: jest.fn(),
    signOut: jest.fn(),
  }));

  // Mock environment variables
  process.env.NEXT_PUBLIC_API_URL = 'http://localhost:5000';
  process.env.NEXTAUTH_SECRET = 'test-secret';

  // Mock window.matchMedia
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: jest.fn().mockImplementation((query) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: jest.fn(), // deprecated
      removeListener: jest.fn(), // deprecated
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
      dispatchEvent: jest.fn(),
    })),
  });

  // Mock IntersectionObserver
  const mockIntersectionObserver = class {
    observe = jest.fn();
    unobserve = jest.fn();
    disconnect = jest.fn();
  };

  Object.defineProperty(window, 'IntersectionObserver', {
    writable: true,
    configurable: true,
    value: mockIntersectionObserver,
  });

  Object.defineProperty(global, 'IntersectionObserver', {
    writable: true,
    configurable: true,
    value: mockIntersectionObserver,
  });

  // Mock ResizeObserver
  const mockResizeObserver = class {
    observe = jest.fn();
    unobserve = jest.fn();
    disconnect = jest.fn();
  };

  Object.defineProperty(window, 'ResizeObserver', {
    writable: true,
    configurable: true,
    value: mockResizeObserver,
  });

  Object.defineProperty(global, 'ResizeObserver', {
    writable: true,
    configurable: true,
    value: mockResizeObserver,
  });
});
