import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  begin: vi.fn(),
  execute: vi.fn(),
  verify: vi.fn(),
  refresh: vi.fn(),
  hasTranslation: true,
}));

vi.mock('@/lib/economy/console-actions', () => ({
  beginEconomyConsoleStepUpAction: mocks.begin,
  executeEconomyConsoleAction: mocks.execute,
  verifyEconomyConsoleStepUpAction: mocks.verify,
}));
vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh: mocks.refresh }) }));
vi.mock('next-intl', () => ({
  useTranslations: () => {
    const translate = (key: string) => key;
    translate.has = () => mocks.hasTranslation;
    return translate;
  },
}));

import { EconomyConsoleActions } from './economy-console-actions';

describe('EconomyConsoleActions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.hasTranslation = true;
    mocks.begin.mockResolvedValue({ success: true, message: 'challenge', challengeId: 'challenge-1' });
    mocks.verify.mockResolvedValue({ success: true, message: 'verified', receipt: 'receipt-1' });
    mocks.execute.mockResolvedValue({ success: true, message: 'recorded' });
  });

  it('executes an ordinary operational command and refreshes its records', async () => {
    render(<EconomyConsoleActions surface="ledger" />);
    fireEvent.click(screen.getByRole('button', { name: 'execute' }));
    await waitFor(() => expect(mocks.execute).toHaveBeenCalledWith('ledger.verify', {}));
    expect(await screen.findByText('recorded')).toBeInTheDocument();
    expect(mocks.refresh).toHaveBeenCalled();
  });

  it('binds a critical command to MFA and consumes the opaque receipt immediately', async () => {
    render(<EconomyConsoleActions surface="policies" />);
    fireEvent.change(screen.getByLabelText('selectAction'), { target: { value: 'policy.approve' } });
    fireEvent.change(screen.getByLabelText('policyId'), { target: { value: '11111111-2222-3333-4444-555555555555' } });
    fireEvent.click(screen.getByRole('button', { name: 'beginMfa' }));
    await screen.findByText('mfaTitle');
    fireEvent.change(screen.getByPlaceholderText('mfaCode'), { target: { value: '123456' } });
    fireEvent.click(screen.getByRole('button', { name: 'verifyAndExecute' }));
    await waitFor(() => expect(mocks.verify).toHaveBeenCalledWith('challenge-1', '123456'));
    expect(mocks.execute).toHaveBeenCalledWith(
      'policy.approve',
      expect.objectContaining({ policyId: '11111111-2222-3333-4444-555555555555' }),
      'receipt-1',
    );
  });

  it('renders structured/select/checkbox fields and reports a rejected action', async () => {
    mocks.execute.mockResolvedValueOnce({ success: false, message: 'policy invalid' });
    render(<EconomyConsoleActions surface="policies" />);
    expect(screen.getByLabelText('payload')).toHaveValue('{}');
    fireEvent.change(screen.getByLabelText('payload'), { target: { value: '{"enabled":true}' } });
    fireEvent.change(screen.getByLabelText('capability'), { target: { value: 'PayoutExecution' } });
    fireEvent.click(screen.getByLabelText('providerReady'));
    fireEvent.click(screen.getByRole('button', { name: 'execute' }));
    expect(await screen.findByText('policy invalid')).toBeInTheDocument();
    expect(screen.getByText('rejected')).toBeInTheDocument();
  });

  it('does not render an action panel for a deliberately read-only surface', () => {
    const { container } = render(<EconomyConsoleActions surface="bounties" />);
    expect(container).toBeEmptyDOMElement();
  });

  it('allows cancelling an unconsumed MFA challenge', async () => {
    render(<EconomyConsoleActions surface="policies" />);
    fireEvent.change(screen.getByLabelText('selectAction'), { target: { value: 'policy.approve' } });
    fireEvent.click(screen.getByRole('button', { name: 'beginMfa' }));
    await screen.findByText('mfaTitle');
    await waitFor(() => expect(screen.getByRole('button', { name: 'cancel' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'cancel' }));
    await waitFor(() => expect(screen.queryByText('mfaTitle')).not.toBeInTheDocument());
  });

  it('keeps a successful challenge response without an identifier fail-closed', async () => {
    mocks.begin.mockResolvedValueOnce({ success: true, message: 'challenge unavailable' });
    render(<EconomyConsoleActions surface="policies" />);
    fireEvent.change(screen.getByLabelText('selectAction'), { target: { value: 'policy.approve' } });
    fireEvent.click(screen.getByRole('button', { name: 'beginMfa' }));
    expect(await screen.findByText('challenge unavailable')).toBeInTheDocument();
    expect(screen.queryByText('mfaTitle')).not.toBeInTheDocument();
  });

  it('shows failed MFA verification without executing the protected action', async () => {
    mocks.verify.mockResolvedValueOnce({ success: false, message: 'MFA rejected' });
    render(<EconomyConsoleActions surface="policies" />);
    fireEvent.change(screen.getByLabelText('selectAction'), { target: { value: 'policy.approve' } });
    fireEvent.click(screen.getByRole('button', { name: 'beginMfa' }));
    await screen.findByText('mfaTitle');
    fireEvent.change(screen.getByPlaceholderText('mfaCode'), { target: { value: '000000' } });
    fireEvent.click(screen.getByRole('button', { name: 'verifyAndExecute' }));
    expect(await screen.findByText('MFA rejected')).toBeInTheDocument();
    expect(mocks.execute).not.toHaveBeenCalled();
  });

  it('initializes reserve JSON fields and renders untranslated textarea labels safely', () => {
    const first = render(<EconomyConsoleActions surface="reserves" />);
    fireEvent.change(screen.getByLabelText('selectAction'), { target: { value: 'reserve.propose' } });
    expect(screen.getByLabelText('buffers')).toHaveValue('{}');
    expect(screen.getByLabelText('services')).toHaveValue('[]');
    expect(screen.getByLabelText('custodyObservationIds')).toHaveValue('[]');
    first.unmount();

    mocks.hasTranslation = false;
    render(<EconomyConsoleActions surface="risk-reviews" />);
    expect(screen.getByLabelText('resolution')).toBeInTheDocument();
  });
});
