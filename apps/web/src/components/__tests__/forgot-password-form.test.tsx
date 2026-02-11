import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithUser } from '@/test/auth-test-helpers';

/* ------------------------------------------------------------------ */
/*  Module mocks                                                       */
/* ------------------------------------------------------------------ */

const mockFetch = vi.fn();

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

const { ForgotPasswordForm } = await import(
  '@/components/forgot-password-form'
);

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('ForgotPasswordForm', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', mockFetch);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  /* ---------- Rendering ---------- */

  it('renders the forgot password form', () => {
    renderWithUser(<ForgotPasswordForm />);

    expect(screen.getByText('Reset your password')).toBeInTheDocument();
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /send reset link/i })
    ).toBeInTheDocument();
  });

  it('renders sign in link', () => {
    renderWithUser(<ForgotPasswordForm />);

    expect(screen.getByText('Sign in')).toHaveAttribute('href', '/sign-in');
  });

  /* ---------- Validation ---------- */

  it('shows error when email is empty', async () => {
    const { user } = renderWithUser(<ForgotPasswordForm />);

    await user.click(
      screen.getByRole('button', { name: /send reset link/i })
    );

    expect(screen.getByText('Email is required.')).toBeInTheDocument();
    expect(mockFetch).not.toHaveBeenCalled();
  });

  it('clears error when user types', async () => {
    const { user } = renderWithUser(<ForgotPasswordForm />);

    await user.click(
      screen.getByRole('button', { name: /send reset link/i })
    );
    expect(screen.getByText('Email is required.')).toBeInTheDocument();

    await user.type(screen.getByLabelText('Email'), 'a');
    expect(
      screen.queryByText('Email is required.')
    ).not.toBeInTheDocument();
  });

  /* ---------- Successful submission ---------- */

  it('shows success message after successful submission', async () => {
    mockFetch.mockResolvedValueOnce({ ok: true });
    const { user } = renderWithUser(<ForgotPasswordForm />);

    await user.type(screen.getByLabelText('Email'), 'test@example.com');
    await user.click(
      screen.getByRole('button', { name: /send reset link/i })
    );

    await waitFor(() => {
      expect(screen.getByText('Check your email')).toBeInTheDocument();
    });
    expect(screen.getByText('test@example.com')).toBeInTheDocument();
    expect(screen.getByText('Back to Sign In')).toHaveAttribute(
      'href',
      '/sign-in'
    );
  });

  it('allows retrying after success', async () => {
    mockFetch.mockResolvedValueOnce({ ok: true });
    const { user } = renderWithUser(<ForgotPasswordForm />);

    await user.type(screen.getByLabelText('Email'), 'test@example.com');
    await user.click(
      screen.getByRole('button', { name: /send reset link/i })
    );

    await waitFor(() => {
      expect(screen.getByText('Check your email')).toBeInTheDocument();
    });

    await user.click(screen.getByText('Try again'));
    expect(screen.getByText('Reset your password')).toBeInTheDocument();
  });

  /* ---------- API error handling ---------- */

  it('displays API error message', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: () =>
        Promise.resolve({ message: 'Rate limit exceeded' }),
    });
    const { user } = renderWithUser(<ForgotPasswordForm />);

    await user.type(screen.getByLabelText('Email'), 'test@example.com');
    await user.click(
      screen.getByRole('button', { name: /send reset link/i })
    );

    await waitFor(() => {
      expect(screen.getByText('Rate limit exceeded')).toBeInTheDocument();
    });
  });

  /* ---------- Loading state ---------- */

  it('shows loading text while submitting', async () => {
    let resolveRequest: (value: unknown) => void;
    mockFetch.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          resolveRequest = resolve;
        })
    );

    const { user } = renderWithUser(<ForgotPasswordForm />);

    await user.type(screen.getByLabelText('Email'), 'test@example.com');
    await user.click(
      screen.getByRole('button', { name: /send reset link/i })
    );

    expect(screen.getByText('Sending...')).toBeInTheDocument();
    expect(screen.getByLabelText('Email')).toBeDisabled();

    // Clean up
    resolveRequest!({ ok: true });
  });
});
