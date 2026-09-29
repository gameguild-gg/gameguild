import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Link from 'next/link';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { Button, buttonVariants } from '@game-guild/ui/components/button';

describe('Button', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('styles navigation without overriding native link semantics', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <Link href="/workspace/projects" className={buttonVariants()}>
        Open projects
      </Link>,
    );

    expect(screen.getByRole('link', { name: 'Open projects' })).toHaveAttribute(
      'href',
      '/workspace/projects',
    );
    expect(consoleError).not.toHaveBeenCalledWith(
      expect.stringContaining('nativeButton'),
    );
  });

  it('keeps actions as native buttons with keyboard activation', async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();
    render(<Button onClick={onClick}>Save</Button>);
    await user.tab();
    await user.keyboard('{Enter}');
    expect(screen.getByRole('button', { name: 'Save' }).tagName).toBe('BUTTON');
    expect(onClick).toHaveBeenCalledOnce();
  });

  it('does not activate disabled actions', async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();
    render(<Button disabled onClick={onClick}>Save</Button>);
    await user.click(screen.getByRole('button', { name: 'Save' }));
    expect(onClick).not.toHaveBeenCalled();
  });
});
