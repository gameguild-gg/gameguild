import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { Input } from '@game-guild/ui/components/input';

describe('shared Input', () => {
  afterEach(() => vi.restoreAllMocks());

  it('updates a controlled value without changing Base UI control mode', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const onChange = vi.fn();
    const { rerender } = render(
      <Input aria-label="Project name" value="Alpha" onChange={onChange} />,
    );

    rerender(<Input aria-label="Project name" value="Beta" onChange={onChange} />);

    expect(screen.getByRole('textbox', { name: 'Project name' })).toHaveValue('Beta');
    expect(consoleError).not.toHaveBeenCalledWith(
      expect.stringContaining('changing the default value state of an uncontrolled FieldControl'),
    );
  });
});
