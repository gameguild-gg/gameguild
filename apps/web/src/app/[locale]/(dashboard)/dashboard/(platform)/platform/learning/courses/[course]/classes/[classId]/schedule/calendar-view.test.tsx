import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { scheduleFixture } from './schedule-test-fixtures';
import { CalendarView } from './calendar-view';

describe('CalendarView', () => {
  it('places releases, meetings, and due dates on their calendar days', () => {
    render(<CalendarView schedule={scheduleFixture} />);

    expect(screen.getByRole('group', { name: 'Wednesday, August 12, 2026' })).toHaveTextContent('Foundations');
    expect(screen.getByRole('group', { name: 'Wednesday, August 12, 2026' })).toHaveTextContent('Foundations studio');
    expect(screen.getByRole('group', { name: 'Tuesday, August 18, 2026' })).toHaveTextContent('Foundations quiz due');
  });
});
