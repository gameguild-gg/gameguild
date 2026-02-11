import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';

/* ------------------------------------------------------------------ */
/*  Module mocks                                                       */
/* ------------------------------------------------------------------ */

const mockReplace = vi.fn();
const mockSearchParams = new URLSearchParams();

vi.mock('next/navigation', () => ({
  useSearchParams: () => mockSearchParams,
  useRouter: () => ({
    replace: mockReplace,
    push: vi.fn(),
    back: vi.fn(),
    forward: vi.fn(),
    refresh: vi.fn(),
    prefetch: vi.fn(),
  }),
}));

vi.mock('next/link', () => ({
  default: ({
    children,
    href,
    ...rest
  }: {
    children: React.ReactNode;
    href: string;
  }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

// Mock useAuth for the verify page content (it imports InputOTPForm which doesn't use auth)
vi.mock('@game-guild/client/react', () => ({
  useAuth: () => ({
    signIn: vi.fn(),
    signUp: vi.fn(),
    signOut: vi.fn(),
    isLoading: false,
    error: null,
    clearError: vi.fn(),
  }),
}));

const { VerifyPageContent } = await import(
  '@/app/[locale]/(auth)/verify/verify-page-content'
);

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('VerifyPageContent', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Reset search params
    mockSearchParams.delete('email');
  });

  it('redirects to /sign-in when email param is missing', () => {
    render(<VerifyPageContent />);

    expect(mockReplace).toHaveBeenCalledWith('/sign-in');
  });

  it('renders OTP form when email param is present', () => {
    mockSearchParams.set('email', 'test@example.com');
    render(<VerifyPageContent />);

    expect(screen.getByText('Verify your login')).toBeInTheDocument();
    expect(screen.getByText('test@example.com')).toBeInTheDocument();
  });

  it('renders the resend button', () => {
    mockSearchParams.set('email', 'test@example.com');
    render(<VerifyPageContent />);

    expect(
      screen.getByRole('button', { name: /resend code/i })
    ).toBeInTheDocument();
  });
});
