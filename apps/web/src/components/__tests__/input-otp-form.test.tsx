import { describe, it, expect, vi, beforeEach } from 'vitest';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { InputOTPForm } from '@/components/input-otp-form';

/* ------------------------------------------------------------------ */
/*  Module mocks                                                       */
/* ------------------------------------------------------------------ */

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

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('InputOTPForm', () => {
  const defaultProps = {
    email: 'test@example.com',
    onVerify: vi.fn().mockResolvedValue(undefined),
    onResend: vi.fn().mockResolvedValue(undefined),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  /* ---------- Rendering ---------- */

  it('renders the OTP form with email displayed', () => {
    render(<InputOTPForm {...defaultProps} />);

    expect(screen.getByText('Verify your login')).toBeInTheDocument();
    expect(screen.getByText('test@example.com')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /verify$/i })
    ).toBeInTheDocument();
  });

  it('renders resend button when onResend is provided', () => {
    render(<InputOTPForm {...defaultProps} />);

    expect(
      screen.getByRole('button', { name: /resend code/i })
    ).toBeInTheDocument();
  });

  it('does not render resend button when onResend is not provided', () => {
    render(
      <InputOTPForm
        email="test@example.com"
        onVerify={defaultProps.onVerify}
      />
    );

    expect(
      screen.queryByRole('button', { name: /resend/i })
    ).not.toBeInTheDocument();
  });

  it('renders support links', () => {
    render(<InputOTPForm {...defaultProps} />);

    expect(
      screen.getByText('I no longer have access to this email address.')
    ).toHaveAttribute('href', '/support');
    expect(screen.getByText('Contact support')).toHaveAttribute(
      'href',
      '/support'
    );
  });

  /* ---------- Submit disabled until 6 digits ---------- */

  it('disables verify button when code is not 6 digits', () => {
    render(<InputOTPForm {...defaultProps} />);

    expect(
      screen.getByRole('button', { name: /verify$/i })
    ).toBeDisabled();
  });

  /* ---------- Error when incomplete code submitted ---------- */

  it('shows error for incomplete code on form submit', async () => {
    render(<InputOTPForm {...defaultProps} />);

    // Submit the form directly so the component validation handles the incomplete code path.
    const form = screen.getByRole('button', { name: /verify$/i }).closest('form')!;
    fireEvent.submit(form);

    expect(defaultProps.onVerify).not.toHaveBeenCalled();
    expect(screen.getByText('Please enter the full 6-digit code.')).toBeInTheDocument();
  });

  /* ---------- Verification error display ---------- */

  it('displays error when onVerify throws', async () => {
    const onVerify = vi.fn().mockRejectedValue(new Error('Invalid code'));
    const user = userEvent.setup();
    render(
      <InputOTPForm
        email="test@example.com"
        onVerify={onVerify}
        onResend={defaultProps.onResend}
      />
    );

    // Type 6 digits into the OTP input
    const otpInput = screen.getByLabelText(/verification code/i);
    await user.type(otpInput, '123456');

    const verifyButton = screen.getByRole('button', { name: /verify$/i });
    await user.click(verifyButton);

    const errorMessage = await screen.findByText('Invalid code');
    expect(errorMessage).toBeInTheDocument();
  });

  /* ---------- Resend code ---------- */

  it('calls onResend when resend button is clicked', async () => {
    const user = userEvent.setup();
    render(<InputOTPForm {...defaultProps} />);

    await user.click(
      screen.getByRole('button', { name: /resend code/i })
    );

    expect(defaultProps.onResend).toHaveBeenCalled();
  });

  it('displays error when onResend throws', async () => {
    const onResend = vi
      .fn()
      .mockRejectedValue(new Error('Failed to resend code.'));
    const user = userEvent.setup();
    render(
      <InputOTPForm
        email="test@example.com"
        onVerify={defaultProps.onVerify}
        onResend={onResend}
      />
    );

    await user.click(
      screen.getByRole('button', { name: /resend code/i })
    );

    const errorMessage = await screen.findByText('Failed to resend code.');
    expect(errorMessage).toBeInTheDocument();
  });

  /* ---------- Loading states ---------- */

  it('shows sending text on resend button while resending', async () => {
    // Create a promise we control
    let resolveResend: () => void;
    const onResend = vi.fn(
      () =>
        new Promise<void>((resolve) => {
          resolveResend = resolve;
        })
    );

    const user = userEvent.setup();
    render(
      <InputOTPForm
        email="test@example.com"
        onVerify={defaultProps.onVerify}
        onResend={onResend}
      />
    );

    await user.click(
      screen.getByRole('button', { name: /resend code/i })
    );

    expect(screen.getByText('Sending...')).toBeInTheDocument();

    // Resolve the promise to clean up
    await act(async () => {
      resolveResend!();
    });
    await waitFor(() => expect(screen.getByRole('button', { name: /resend code/i })).toBeEnabled());
  });
});
