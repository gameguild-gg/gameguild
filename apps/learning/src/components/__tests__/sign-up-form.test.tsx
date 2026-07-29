import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { createMockUseAuth, renderWithUser, type MockUseAuthReturn } from '@/test/auth-test-helpers';

let mockAuth: MockUseAuthReturn;

vi.mock('@game-guild/client/react', () => ({
  useAuth: () => mockAuth,
}));

vi.mock('@game-guild/ui/components/button', () => ({
  Button: ({ children, ...props }: React.ComponentProps<'button'>) => <button {...props}>{children}</button>,
}));

vi.mock('@game-guild/ui/components/card', () => ({
  Card: ({ children, ...props }: React.ComponentProps<'div'>) => <div {...props}>{children}</div>,
  CardHeader: ({ children, ...props }: React.ComponentProps<'div'>) => <div {...props}>{children}</div>,
  CardTitle: ({ children, ...props }: React.ComponentProps<'div'>) => <div {...props}>{children}</div>,
  CardDescription: ({ children, ...props }: React.ComponentProps<'div'>) => <div {...props}>{children}</div>,
  CardContent: ({ children, ...props }: React.ComponentProps<'div'>) => <div {...props}>{children}</div>,
}));

vi.mock('@game-guild/ui/components/field', () => ({
  FieldGroup: ({ children, ...props }: React.ComponentProps<'div'>) => <div {...props}>{children}</div>,
  Field: ({ children, ...props }: React.ComponentProps<'div'>) => <div {...props}>{children}</div>,
  FieldLabel: ({ children, ...props }: React.ComponentProps<'label'>) => <label {...props}>{children}</label>,
  FieldDescription: ({ children, ...props }: React.ComponentProps<'div'>) => <div {...props}>{children}</div>,
  FieldError: ({ children, ...props }: React.ComponentProps<'div'>) => <div {...props}>{children}</div>,
}));

vi.mock('@game-guild/ui/components/input', () => ({
  Input: (props: React.ComponentProps<'input'>) => <input {...props} />,
}));

vi.mock('next/link', () => ({
  default: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

const { SignUpForm } = await import('@/components/sign-up-form');

describe('SignUpForm', () => {
  beforeEach(() => {
    mockAuth = createMockUseAuth();
  });

  it('submits learner registration with a split full name and redirect target', async () => {
    const { user } = renderWithUser(<SignUpForm redirectTo="/courses/game-guild/content" />);

    expect(screen.getByRole('heading', { level: 1, name: 'Create your learner account' })).toBeInTheDocument();

    await waitFor(() => expect(screen.getByRole('button', { name: /create account/i })).toBeEnabled());
    await user.type(screen.getByLabelText('Full name'), 'Ada Lovelace');
    await user.type(screen.getByLabelText('Email'), 'ada@example.com');
    await user.type(screen.getByLabelText('Password'), 'password123');
    await user.type(screen.getByLabelText('Confirm password'), 'password123');
    await user.click(screen.getByRole('button', { name: /create account/i }));

    expect(mockAuth.clearError).toHaveBeenCalled();
    expect(mockAuth.signUp).toHaveBeenCalledWith({
      username: 'ada',
      email: 'ada@example.com',
      password: 'password123',
      firstName: 'Ada',
      lastName: 'Lovelace',
      redirectTo: '/courses/game-guild/content',
    });
  });

  it('shows a password mismatch error before calling signUp', async () => {
    const { user } = renderWithUser(<SignUpForm redirectTo="/" />);

    await waitFor(() => expect(screen.getByRole('button', { name: /create account/i })).toBeEnabled());
    await user.type(screen.getByLabelText('Full name'), 'Ada Lovelace');
    await user.type(screen.getByLabelText('Email'), 'ada@example.com');
    await user.type(screen.getByLabelText('Password'), 'password123');
    await user.type(screen.getByLabelText('Confirm password'), 'different123');
    await user.click(screen.getByRole('button', { name: /create account/i }));

    expect(screen.getByText('Passwords do not match.')).toBeInTheDocument();
    expect(mockAuth.signUp).not.toHaveBeenCalled();
  });
});
