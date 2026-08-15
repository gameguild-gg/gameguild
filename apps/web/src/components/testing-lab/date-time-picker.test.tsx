import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { DateTimePicker } from '@/components/ui/date-time-picker';

describe('DateTimePicker', () => {
  it('commits the selected wall-clock value to the named form field', () => {
    const onValueChange = vi.fn();
    const { container } = render(
      <form>
        <label htmlFor="event-start">Event starts</label>
        <DateTimePicker id="event-start" name="startsAt" defaultValue="2026-08-20T14:35" onValueChange={onValueChange} />
      </form>,
    );

    expect(screen.getByRole('button', { name: /event starts/i })).toHaveTextContent('UTC');
    expect(container.querySelector('input[type="datetime-local"]')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /event starts/i }));
    fireEvent.change(screen.getByLabelText('Hour'), {
      target: { value: '16' },
    });
    fireEvent.change(screen.getByLabelText('Minute'), {
      target: { value: '07' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Apply date and time' }));

    const field = container.querySelector<HTMLInputElement>('input[name="startsAt"]');
    expect(field?.value).toBe('2026-08-20T16:07');
    expect(new FormData(container.querySelector('form')!).get('startsAt')).toBe('2026-08-20T16:07');
    expect(onValueChange).toHaveBeenCalledWith('2026-08-20T16:07');
  });

  it('cancels drafts and clears optional values without clearing required values', () => {
    const { rerender } = render(
      <div>
        <label htmlFor="optional-date">Optional date</label>
        <DateTimePicker id="optional-date" name="optionalDate" defaultValue="2026-08-20T14:35" />
      </div>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Optional date' }));
    fireEvent.change(screen.getByLabelText('Hour'), {
      target: { value: '22' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Cancel date and time changes' }));

    fireEvent.click(screen.getByRole('button', { name: 'Optional date' }));
    expect(screen.getByLabelText('Hour')).toHaveValue(14);
    fireEvent.click(screen.getByRole('button', { name: 'Clear date and time' }));
    expect(document.querySelector<HTMLInputElement>('input[name="optionalDate"]')?.value).toBe('');

    rerender(
      <div>
        <label htmlFor="required-date">Required date</label>
        <DateTimePicker id="required-date" name="requiredDate" required defaultValue="2026-08-20T14:35" />
      </div>,
    );
    fireEvent.click(screen.getByRole('button', { name: 'Required date' }));
    expect(screen.queryByRole('button', { name: 'Clear date and time' })).not.toBeInTheDocument();
  });
});
