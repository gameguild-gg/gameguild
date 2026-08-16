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

vi.mock('@game-guild/ui/components/input-otp', () => ({
  InputOTP: ({
    id,
    value,
    onChange,
    disabled,
    maxLength,
  }: {
    id?: string;
    value?: string;
    onChange?: (value: string) => void;
    disabled?: boolean;
    maxLength?: number;
  }) => (
    <input
      id={id}
      data-input-otp
      value={value ?? ''}
      disabled={disabled}
      maxLength={maxLength}
      onChange={(event) => onChange?.(event.target.value)}
    />
  ),
  InputOTPGroup: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  InputOTPSeparator: () => <span>-</span>,
  InputOTPSlot: ({ index }: { index: number }) => <span data-slot-index={index} />,
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

const { VerifyPageContent } = await import('@/components/auth/verify-page-content');

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
