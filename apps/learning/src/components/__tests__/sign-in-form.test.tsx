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

const { SignInForm } = await import('@/components/sign-in-form');

describe('SignInForm', () => {
  beforeEach(() => {
    mockAuth = createMockUseAuth();
  });

  it('submits credentials with the provided redirect target', async () => {
    const { user } = renderWithUser(<SignInForm redirectTo="/courses/game-guild/content" />);

    expect(screen.getByRole('heading', { level: 1, name: 'Student sign in' })).toBeInTheDocument();

    await waitFor(() => expect(screen.getByRole('button', { name: /continue to classroom/i })).toBeEnabled());
    await user.type(screen.getByLabelText('Email'), 'student@example.com');
    await user.type(screen.getByLabelText('Password'), 'password123');
    await user.click(screen.getByRole('button', { name: /continue to classroom/i }));

    expect(mockAuth.clearError).toHaveBeenCalled();
    expect(mockAuth.signIn).toHaveBeenCalledWith('credentials', {
      email: 'student@example.com',
      password: 'password123',
      redirectTo: '/courses/game-guild/content',
    });
  });

  it('shows field validation errors before calling signIn', async () => {
    const { user } = renderWithUser(<SignInForm redirectTo="/" />);

    await user.click(screen.getByRole('button', { name: /continue to classroom/i }));

    expect(screen.getByText('Email is required.')).toBeInTheDocument();
    expect(mockAuth.signIn).not.toHaveBeenCalled();
  });
});
