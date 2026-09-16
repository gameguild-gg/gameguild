import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ bulk: vi.fn() }));
vi.mock('@/lib/testing-lab/actions', () => ({ bulkUpdateTestingRequests: mocks.bulk }));

import { TestingLabBulkRequestForm } from './testing-lab-bulk-request-form';

function renderRequests() {
  return render(
    <TestingLabBulkRequestForm matchingCount={2}>
      <input aria-label="First request" type="checkbox" name="requestIds" value="request-1" />
      <input aria-label="Second request" type="checkbox" name="requestIds" value="request-2" />
    </TestingLabBulkRequestForm>,
  );
}

describe('TestingLabBulkRequestForm extended behavior', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('cancels a plural restore confirmation without executing it', async () => {
    const user = userEvent.setup();
    const { container } = renderRequests();
    fireEvent.submit(container.querySelector('form')!);
    await user.click(screen.getByLabelText('First request'));
    await user.click(screen.getByLabelText('Second request'));
    await user.click(screen.getByRole('button', { name: 'Restore selected' }));
    expect(screen.getByText(/2 requests will be returned to active operations/)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    expect(mocks.bulk).not.toHaveBeenCalled();
  });

  it('executes a restore and clears all selections after success', async () => {
    const user = userEvent.setup();
    mocks.bulk.mockResolvedValueOnce({ success: true, message: 'Requests restored.' });
    renderRequests();
    await user.click(screen.getByLabelText('First request'));
    await user.click(screen.getByLabelText('Second request'));
    await user.click(screen.getByRole('button', { name: 'Restore selected' }));
    await user.click(screen.getByRole('button', { name: 'Restore requests' }));

    await waitFor(() => expect(mocks.bulk).toHaveBeenCalledOnce());
    const data = mocks.bulk.mock.calls[0]![0] as FormData;
    expect(data.getAll('requestIds')).toEqual(['request-1', 'request-2']);
    expect(data.get('operation')).toBe('restore');
    expect(await screen.findByText('Requests restored.')).toBeInTheDocument();
    expect(screen.getByLabelText('First request')).not.toBeChecked();
    expect(screen.getByLabelText('Second request')).not.toBeChecked();
  });

  it('keeps selections after an unsuccessful operation', async () => {
    const user = userEvent.setup();
    mocks.bulk.mockResolvedValueOnce({ success: false, error: 'Bulk update denied.' });
    renderRequests();
    await user.click(screen.getByLabelText('First request'));
    await user.click(screen.getByRole('button', { name: 'Archive selected' }));
    await user.click(screen.getByRole('button', { name: 'Archive requests' }));

    expect(await screen.findByText('Bulk update denied.')).toBeInTheDocument();
    expect(screen.getByLabelText('First request')).toBeChecked();
  });

  it.each([
    [new Error('Bulk service offline'), 'Bulk service offline'],
    ['failure', 'The Testing Lab operation failed.'],
  ])('turns thrown bulk-operation failures into visible errors', async (failure, message) => {
    const user = userEvent.setup();
    mocks.bulk.mockRejectedValueOnce(failure);
    renderRequests();
    await user.click(screen.getByLabelText('First request'));
    await user.click(screen.getByRole('button', { name: 'Archive selected' }));
    await user.click(screen.getByRole('button', { name: 'Archive requests' }));
    expect(await screen.findByText(message)).toBeInTheDocument();
  });
});
