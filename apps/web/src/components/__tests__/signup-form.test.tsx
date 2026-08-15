import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import {
  createMockUseAuth,
  renderWithUser,
  type MockUseAuthReturn,
} from '@/test/auth-test-helpers';

/* ------------------------------------------------------------------ */
/*  Module mocks                                                       */
/* ------------------------------------------------------------------ */

let mockAuth: MockUseAuthReturn;

vi.mock('@game-guild/client/react', () => ({
  useAuth: () => mockAuth,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({
    children,
    href,
    locale,
    ...rest
  }: {
    children: React.ReactNode;
    href: string;
    locale?: string;
  }) => (
    <a href={locale ? `/${locale}${href}` : href} data-locale={locale} {...rest}>
      {children}
    </a>
  ),
}));

vi.mock('next-intl', () => ({
  useLocale: () => 'en-US',
}));

const { SignupForm } = await import('@/components/signup-form');

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('SignupForm', () => {
  beforeEach(() => {
    mockAuth = createMockUseAuth();
  });

  /* ---------- Rendering ---------- */

  it('renders the signup form with all required elements', () => {
    renderWithUser(<SignupForm />);

    expect(screen.getByText('Create your GameGuild account')).toBeInTheDocument();
    expect(screen.getByLabelText('Full Name')).toBeInTheDocument();
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
    expect(screen.getByLabelText('Confirm Password')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /create account$/i })
    ).toBeInTheDocument();
  });

  it('renders navigation links', () => {
    renderWithUser(<SignupForm />);

    expect(screen.getByText('Sign in')).toHaveAttribute('href', '/en-US/sign-in?redirectTo=%2Fmy');
    expect(screen.getByText('Terms of Service')).toHaveAttribute('href', '/en-US/terms-of-service');
    expect(screen.getByText('Privacy Policy')).toHaveAttribute('href', '/en-US/polices/privacy');
  });

  /* ---------- Client-side validation ---------- */

  it('shows error when name is empty on submit', async () => {
    const { user } = renderWithUser(<SignupForm />);

    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(screen.getByText('Full name is required.')).toBeInTheDocument();
    expect(mockAuth.signUp).not.toHaveBeenCalled();
  });

  it('shows error when email is empty on submit', async () => {
    const { user } = renderWithUser(<SignupForm />);

    await user.type(screen.getByLabelText('Full Name'), 'Jo');
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(screen.getByText('Email is required.')).toBeInTheDocument();
    expect(mockAuth.signUp).not.toHaveBeenCalled();
  });

  it('shows error when password is empty on submit', async () => {
    const { user } = renderWithUser(<SignupForm />);

    await user.type(screen.getByLabelText('Full Name'), 'Jo');
    await user.type(screen.getByLabelText('Email'), 'j@e.co');
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(screen.getByText('Password is required.')).toBeInTheDocument();
  });

  it('shows error when password is less than 8 characters', async () => {
    const { user } = renderWithUser(<SignupForm />);

    await user.type(screen.getByLabelText('Full Name'), 'Jo');
    await user.type(screen.getByLabelText('Email'), 'j@e.co');
    await user.type(screen.getByLabelText('Password'), 'short');
    await user.type(screen.getByLabelText('Confirm Password'), 'short');
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(
      screen.getByText('Password must be at least 8 characters.')
    ).toBeInTheDocument();
  });

  it('shows error when confirm password is empty', async () => {
    const { user } = renderWithUser(<SignupForm />);

    await user.type(screen.getByLabelText('Full Name'), 'Jo');
    await user.type(screen.getByLabelText('Email'), 'j@e.co');
    await user.type(screen.getByLabelText('Password'), '12345678');
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(
      screen.getByText('Please confirm your password.')
    ).toBeInTheDocument();
  });

  it('shows error when passwords do not match', async () => {
    const { user } = renderWithUser(<SignupForm />);

    await user.type(screen.getByLabelText('Full Name'), 'Jo');
    await user.type(screen.getByLabelText('Email'), 'j@e.co');
    await user.type(screen.getByLabelText('Password'), '12345678');
    await user.type(screen.getByLabelText('Confirm Password'), '12345679');
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(screen.getByText('Passwords do not match.')).toBeInTheDocument();
  }, 10000);

  it('shows multiple field errors simultaneously', async () => {
    const { user } = renderWithUser(<SignupForm />);

    // Submit with all fields empty
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(screen.getByText('Full name is required.')).toBeInTheDocument();
    expect(screen.getByText('Email is required.')).toBeInTheDocument();
    expect(screen.getByText('Password is required.')).toBeInTheDocument();
  });

  it('clears individual field error when user starts typing', async () => {
    const { user } = renderWithUser(<SignupForm />);

    // Trigger errors
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );
    expect(screen.getByText('Full name is required.')).toBeInTheDocument();

    // Typing in name clears only name error
    await user.type(screen.getByLabelText('Full Name'), 'J');
    expect(
      screen.queryByText('Full name is required.')
    ).not.toBeInTheDocument();
    // Email error still present
    expect(screen.getByText('Email is required.')).toBeInTheDocument();
  });

  /* ---------- Successful submission ---------- */

  it('calls signUp with correct data on valid submission', async () => {
    const { user } = renderWithUser(<SignupForm />);

    await user.type(screen.getByLabelText('Full Name'), 'Jo Do');
    await user.type(screen.getByLabelText('Email'), 'jo@e.co');
    await user.type(screen.getByLabelText('Password'), '12345678');
    await user.type(screen.getByLabelText('Confirm Password'), '12345678');
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(mockAuth.clearError).toHaveBeenCalled();
    expect(mockAuth.signUp).toHaveBeenCalledWith({
      username: 'jo',
      email: 'jo@e.co',
      password: '12345678',
      firstName: 'Jo',
      lastName: 'Do',
      redirectTo: '/my',
    });
  }, 10000);

  it('derives username from email prefix', async () => {
    const { user } = renderWithUser(<SignupForm />);

    await user.type(screen.getByLabelText('Full Name'), 'Jan');
    await user.type(
      screen.getByLabelText('Email'),
      'j.s@e.co'
    );
    await user.type(screen.getByLabelText('Password'), '12345678');
    await user.type(screen.getByLabelText('Confirm Password'), '12345678');
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(mockAuth.signUp).toHaveBeenCalledWith(
      expect.objectContaining({
        username: 'j.s',
      })
    );
  }, 10000);

  it('handles single name (no last name)', async () => {
    const { user } = renderWithUser(<SignupForm />);

    await user.type(screen.getByLabelText('Full Name'), 'Mdn');
    await user.type(screen.getByLabelText('Email'), 'm@t.co');
    await user.type(screen.getByLabelText('Password'), '12345678');
    await user.type(screen.getByLabelText('Confirm Password'), '12345678');
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(mockAuth.signUp).toHaveBeenCalledWith(
      expect.objectContaining({
        firstName: 'Mdn',
        lastName: undefined,
      })
    );
  }, 10000);

  /* ---------- Loading state ---------- */

  it('shows loading text and disables inputs while loading', () => {
    mockAuth = createMockUseAuth({ isLoading: true });
    renderWithUser(<SignupForm />);

    expect(
      screen.getByRole('button', { name: /creating account/i })
    ).toBeDisabled();
    expect(screen.getByLabelText('Full Name')).toBeDisabled();
    expect(screen.getByLabelText('Email')).toBeDisabled();
    expect(screen.getByLabelText('Password')).toBeDisabled();
    expect(screen.getByLabelText('Confirm Password')).toBeDisabled();
  });

  /* ---------- Error display ---------- */

  it('displays API error from useAuth', () => {
    mockAuth = createMockUseAuth({
      error: new Error('Email already registered'),
    });
    renderWithUser(<SignupForm />);

    expect(screen.getByText('Email already registered')).toBeInTheDocument();
  });

  /* ---------- Handles signUp rejection ---------- */

  it('handles signUp rejection gracefully', async () => {
    mockAuth = createMockUseAuth({
      signUp: vi.fn().mockRejectedValue(new Error('Server error')),
    });
    const { user } = renderWithUser(<SignupForm />);

    await user.type(screen.getByLabelText('Full Name'), 'Jo');
    await user.type(screen.getByLabelText('Email'), 'j@e.co');
    await user.type(screen.getByLabelText('Password'), '12345678');
    await user.type(screen.getByLabelText('Confirm Password'), '12345678');

    // Should not throw
    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );
  }, 10000);

  /* ---------- Accessibility ---------- */

  it('sets aria-invalid on fields when validation fails', async () => {
    const { user } = renderWithUser(<SignupForm />);

    await user.click(
      screen.getByRole('button', { name: /create account$/i })
    );

    expect(screen.getByLabelText('Full Name')).toHaveAttribute(
      'aria-invalid',
      'true'
    );
    expect(screen.getByLabelText('Email')).toHaveAttribute(
      'aria-invalid',
      'true'
    );
  });

  /* ---------- className forwarding ---------- */

  it('forwards className prop', () => {
    const { container } = renderWithUser(
      <SignupForm className="custom-class" />
    );

    expect(container.firstChild).toHaveClass('custom-class');
  });
});
