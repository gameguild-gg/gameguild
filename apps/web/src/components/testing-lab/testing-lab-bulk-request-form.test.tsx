import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ bulkUpdateTestingRequests: vi.fn() }));
vi.mock('@/lib/testing-lab/actions', () => ({ bulkUpdateTestingRequests: mocks.bulkUpdateTestingRequests }));

import { TestingLabBulkRequestForm } from './testing-lab-bulk-request-form';

describe('TestingLabBulkRequestForm', () => {
  beforeEach(() => vi.clearAllMocks());

  it('requires a selection before opening confirmation', async () => {
    render(
      <TestingLabBulkRequestForm matchingCount={1}>
        <input type="checkbox" name="requestIds" value="request-1" />
      </TestingLabBulkRequestForm>,
    );
    await userEvent.click(screen.getByRole('button', { name: /archive selected/i }));
    expect(await screen.findByText('Select at least one testing request.')).toBeVisible();
    expect(mocks.bulkUpdateTestingRequests).not.toHaveBeenCalled();
  });

  it('confirms and executes a bulk archive with selected ids', async () => {
    mocks.bulkUpdateTestingRequests.mockResolvedValue({ success: true, data: { processed: 1 }, message: '1 requests updated.' });
    render(
      <TestingLabBulkRequestForm matchingCount={1}>
        <input aria-label="Select request" type="checkbox" name="requestIds" value="request-1" />
      </TestingLabBulkRequestForm>,
    );
    await userEvent.click(screen.getByLabelText('Select request'));
    await userEvent.click(screen.getByRole('button', { name: /archive selected/i }));
    expect(screen.getByRole('alertdialog')).toBeVisible();
    await userEvent.click(screen.getByRole('button', { name: 'Archive requests' }));
    await waitFor(() => expect(mocks.bulkUpdateTestingRequests).toHaveBeenCalledOnce());
    const data = mocks.bulkUpdateTestingRequests.mock.calls[0]?.[0] as FormData;
    expect(data.getAll('requestIds')).toEqual(['request-1']);
    expect(data.get('operation')).toBe('archive');
    expect(await screen.findByText('1 requests updated.')).toBeVisible();
  });
});
