import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Input } from '@game-guild/ui/components/input';

describe('shared Input', () => {
  it('updates a controlled value on rerender', () => {
    const { rerender } = render(
      <Input aria-label="Project name" value="Alpha" onChange={() => undefined} />,
    );

    rerender(<Input aria-label="Project name" value="Beta" onChange={() => undefined} />);

    expect(screen.getByRole('textbox', { name: 'Project name' })).toHaveValue('Beta');
  });
});
