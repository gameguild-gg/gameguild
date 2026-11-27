import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { BackupCodesReveal } from '../backup-codes-reveal';

const labels = {
  title: 'Your recovery codes',
  description: 'Shown only once.',
  copyLabel: 'Copy all codes',
  copiedLabel: 'Copied',
  copyFailedLabel: 'Copy failed',
  dismissLabel: 'I saved my codes',
};

function renderReveal(onDismiss = vi.fn()) {
  return render(
    <BackupCodesReveal codes={['AAAA-BBBB', 'CCCC-DDDD']} {...labels} onDismiss={onDismiss} testId="codes" />,
  );
}

describe('BackupCodesReveal (issue #1298 J2)', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('lists every code, with the one-time warning, and hands dismissal to the caller', async () => {
    const user = userEvent.setup();
    const onDismiss = vi.fn();
    renderReveal(onDismiss);

    const list = screen.getByTestId('codes-list');
    expect(list).toHaveTextContent('AAAA-BBBB');
    expect(list).toHaveTextContent('CCCC-DDDD');
    expect(screen.getByTestId('codes')).toHaveTextContent('Shown only once.');

    await user.click(screen.getByTestId('codes-dismiss'));
    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it('copies every code, one per line, and says so', async () => {
    const user = userEvent.setup();
    const writeText = vi.fn().mockResolvedValue(undefined);
    vi.stubGlobal('navigator', { ...navigator, clipboard: { writeText } });
    renderReveal();

    await user.click(screen.getByTestId('codes-copy'));

    expect(writeText).toHaveBeenCalledWith('AAAA-BBBB\nCCCC-DDDD');
    expect(screen.getByTestId('codes-copy')).toHaveTextContent('Copied');
  });

  it('never claims a copy that the browser refused', async () => {
    const user = userEvent.setup();
    vi.stubGlobal('navigator', { ...navigator, clipboard: {} });
    renderReveal();

    await user.click(screen.getByTestId('codes-copy'));

    expect(screen.getByRole('alert')).toHaveTextContent('Copy failed');
    expect(screen.getByTestId('codes-copy')).toHaveTextContent('Copy all codes');
  });
});
