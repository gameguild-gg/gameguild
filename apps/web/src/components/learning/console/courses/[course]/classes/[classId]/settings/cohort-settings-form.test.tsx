import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ComponentProps, ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ update: vi.fn(), updateStatus: vi.fn(), refresh: vi.fn() }));
vi.mock('@/lib/learning/actions/cohorts', () => ({ updateCohort: mocks.update, updateCohortStatus: mocks.updateStatus }));
vi.mock('@/i18n/navigation', () => ({ useRouter: () => ({ refresh: mocks.refresh }) }));
vi.mock('@game-guild/ui/components/button', () => ({
  Button: ({ children, ...props }: ComponentProps<'button'> & { children: ReactNode }) => <button {...props}>{children}</button>,
}));
vi.mock('@game-guild/ui/components/input', () => ({ Input: (props: ComponentProps<'input'>) => <input {...props} /> }));
vi.mock('@game-guild/ui/components/textarea', () => ({ Textarea: (props: ComponentProps<'textarea'>) => <textarea {...props} /> }));
vi.mock('@game-guild/ui/components/label', () => ({ Label: (props: ComponentProps<'label'>) => <label {...props} /> }));
vi.mock('@game-guild/ui/components/badge', () => ({ Badge: ({ children }: { children: ReactNode }) => <span>{children}</span> }));
vi.mock('@game-guild/ui/components/alert-dialog', () => ({
  AlertDialog: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  AlertDialogAction: ({ children, ...props }: ComponentProps<'button'>) => <button {...props}>{children}</button>,
  AlertDialogCancel: ({ children }: { children: ReactNode }) => <button type="button">{children}</button>,
  AlertDialogContent: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  AlertDialogDescription: ({ children }: { children: ReactNode }) => <p>{children}</p>,
  AlertDialogFooter: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  AlertDialogHeader: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  AlertDialogTitle: ({ children }: { children: ReactNode }) => <h3>{children}</h3>,
  AlertDialogTrigger: ({ children }: { children: ReactNode }) => children,
}));

import { CohortSettingsForm } from './cohort-settings-form';

function cohort(overrides: Record<string, unknown> = {}) {
  return {
    id: 'cohort-1', name: 'Evening class', description: 'Cohort description', status: 'active', isOpen: true,
    period: { startsAt: '2026-08-01T00:00:00Z', endsAt: '2026-12-01T00:00:00Z' },
    enrollment: { capacity: 24, current: 8 }, meetingPattern: 'Tue/Thu - 19:00', ...overrides,
  } as never;
}

function renderForm(overrides: Record<string, unknown> = {}) {
  return render(<CohortSettingsForm courseId="course-1" cohort={cohort(overrides)} />);
}

describe('CohortSettingsForm', () => {
  beforeEach(() => vi.clearAllMocks());

  it('persists every class setting and refreshes on success', async () => {
    mocks.update.mockResolvedValue({ success: true });
    renderForm();
    fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Advanced AI' } });
    fireEvent.change(screen.getByLabelText('Description'), { target: { value: 'Updated' } });
    fireEvent.change(screen.getByLabelText('Start date'), { target: { value: '2026-09-01' } });
    fireEvent.change(screen.getByLabelText('End date'), { target: { value: '2026-11-30' } });
    fireEvent.change(screen.getByLabelText('Capacity'), { target: { value: '32' } });
    fireEvent.change(screen.getByLabelText('Meeting pattern'), { target: { value: 'Mon/Wed - 18:00' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save settings' }));

    expect(await screen.findByRole('status')).toHaveTextContent('Class settings saved.');
    expect(mocks.update).toHaveBeenCalledWith({
      courseId: 'course-1', cohortId: 'cohort-1', name: 'Advanced AI', description: 'Updated',
      startDate: '2026-09-01T00:00:00', endDate: '2026-11-30T23:59:59', maxCapacity: 32,
      meetingSchedule: 'Mon/Wed - 18:00',
    });
    expect(mocks.refresh).toHaveBeenCalledOnce();
  });

  it('uses empty form fallbacks and renders default capacity and meeting pattern', async () => {
    mocks.update.mockResolvedValue({ success: false, error: 'Invalid class settings.' });
    const { container } = renderForm({ enrollment: { capacity: null, current: 0 }, meetingPattern: null });
    expect(screen.getByLabelText('Capacity')).toHaveValue(24);
    expect(screen.getByLabelText('Meeting pattern')).toHaveValue('');
    for (const name of ['name', 'description', 'startDate', 'endDate', 'capacity', 'meetingPattern']) {
      container.querySelector(`[name="${name}"]`)?.remove();
    }
    fireEvent.submit(container.querySelector('form')!);
    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid class settings.');
    expect(mocks.update).toHaveBeenCalledWith(expect.objectContaining({
      name: '', description: '', startDate: 'T00:00:00', endDate: 'T23:59:59',
      maxCapacity: Number.NaN, meetingSchedule: '',
    }));
    expect(mocks.refresh).not.toHaveBeenCalled();
  });

  it.each([
    [new Error('Save API unavailable.'), 'Save API unavailable.'],
    ['offline', 'Unable to save class settings.'],
  ])('normalizes unexpected save failures', async (error, expected) => {
    mocks.update.mockRejectedValue(error);
    renderForm();
    fireEvent.click(screen.getByRole('button', { name: 'Save settings' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(expected);
  });

  it('closes enrollment and completes or cancels the class', async () => {
    mocks.updateStatus.mockResolvedValue({ success: true });
    renderForm();
    fireEvent.click(screen.getByRole('button', { name: 'Close enrollment' }));
    await waitFor(() => expect(mocks.updateStatus).toHaveBeenCalledWith('course-1', 'cohort-1', 'close'));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Mark complete' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'Mark complete' }));
    await waitFor(() => expect(mocks.updateStatus).toHaveBeenCalledWith('course-1', 'cohort-1', 'complete'));
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'Cancel class' }).at(-1)).toBeEnabled());
    fireEvent.click(screen.getAllByRole('button', { name: 'Cancel class' }).at(-1)!);
    await waitFor(() => expect(mocks.updateStatus).toHaveBeenCalledWith('course-1', 'cohort-1', 'cancel'));
    expect(await screen.findByRole('status')).toHaveTextContent('Class status updated.');
  });

  it('opens enrollment and surfaces a lifecycle API failure', async () => {
    mocks.updateStatus.mockResolvedValue({ success: false, error: 'Cannot reopen this class.' });
    renderForm({ isOpen: false });
    fireEvent.click(screen.getByRole('button', { name: 'Open enrollment' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Cannot reopen this class.');
    expect(mocks.updateStatus).toHaveBeenCalledWith('course-1', 'cohort-1', 'open');
  });

  it.each([
    [new Error('Status API unavailable.'), 'Status API unavailable.'],
    ['offline', 'Unable to update class status.'],
  ])('normalizes unexpected lifecycle failures', async (error, expected) => {
    mocks.updateStatus.mockRejectedValue(error);
    renderForm();
    fireEvent.click(screen.getByRole('button', { name: 'Close enrollment' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(expected);
  });

  it('disables terminal lifecycle actions', () => {
    const { rerender } = render(<CohortSettingsForm courseId="course-1" cohort={cohort({ status: 'completed' })} />);
    expect(screen.getByRole('button', { name: 'Mark complete' })).toBeDisabled();
    rerender(<CohortSettingsForm courseId="course-1" cohort={cohort({ status: 'cancelled' })} />);
    expect(screen.getAllByRole('button', { name: 'Cancel class' })[0]).toBeDisabled();
  });
});
