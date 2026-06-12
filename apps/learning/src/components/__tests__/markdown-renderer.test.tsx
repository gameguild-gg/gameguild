import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import MarkdownRenderer from '../markdown-renderer';

describe('MarkdownRenderer activities', () => {
  it('renders interactive quiz and code activities as usable learning blocks', () => {
    render(
      <MarkdownRenderer
        content={`!!! quiz
What should a playtester report after a session?
!!!

!!! code
console.log("ship it")
!!!`}
      />,
    );

    expect(screen.getByText(/knowledge check/i)).toBeInTheDocument();
    expect(screen.getByText(/what should a playtester report/i)).toBeInTheDocument();
    expect(screen.getByText(/code activity/i)).toBeInTheDocument();
    expect(screen.getByText(/console\.log/)).toBeInTheDocument();
    expect(screen.queryByText(/placeholders will move here/i)).not.toBeInTheDocument();
  });
});
