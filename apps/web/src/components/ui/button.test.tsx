import { render, screen } from '@testing-library/react';
import Link from 'next/link';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { Button } from '@game-guild/ui/components/button';

describe('Button', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders a render-prop link without Base UI native button errors', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <Button nativeButton={false} render={<Link href="/workspace/projects" />}>
        Open projects
      </Button>,
    );

    expect(screen.getByRole('button', { name: 'Open projects' })).toHaveAttribute(
      'href',
      '/workspace/projects',
    );
    expect(consoleError).not.toHaveBeenCalledWith(
      expect.stringContaining('nativeButton'),
    );
  });
});
