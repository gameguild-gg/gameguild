import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { scheduleFixture } from './schedule-test-fixtures';
import { TimelineView } from './timeline-view';

describe('TimelineView', () => {
  it('renders schedule events in chronological order', () => {
    render(<TimelineView schedule={scheduleFixture} />);

    const entries = screen.getAllByTestId('timeline-entry');
    expect(entries[0]).toHaveTextContent('Foundations');
    expect(entries[1]).toHaveTextContent('Foundations quiz');
    expect(entries[2]).toHaveTextContent('Foundations studio');
    expect(entries.at(-1)).toHaveTextContent('Decision systems');
  });
});
