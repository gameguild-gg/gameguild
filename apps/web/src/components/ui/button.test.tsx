import { render, screen } from '@testing-library/react';
import Link from 'next/link';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { Button } from '@game-guild/ui/components/button';

describe('Button', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders an asChild link without Base UI native button errors', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <Button asChild>
        <Link href="/workspace/projects">Open projects</Link>
      </Button>,
    );

    expect(screen.getByRole('link', { name: 'Open projects' })).toHaveAttribute(
      'href',
      '/workspace/projects',
    );
    expect(consoleError).not.toHaveBeenCalledWith(
      expect.stringContaining('nativeButton'),
    );
  });
});
