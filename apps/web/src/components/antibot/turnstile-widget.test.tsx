import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render } from '@testing-library/react';

import { TurnstileWidget } from './turnstile-widget';

type RenderCall = { container: HTMLElement; options: Record<string, unknown> };

function stubTurnstile() {
  const calls: RenderCall[] = [];
  const removed: string[] = [];
  let nextId = 0;
  const turnstile = {
    render: (container: HTMLElement, options: Record<string, unknown>) => {
      calls.push({ container, options });
      return `widget-${nextId++}`;
    },
    remove: (widgetId: string) => {
      removed.push(widgetId);
    },
  };
  Object.defineProperty(window, 'turnstile', {
    value: turnstile,
    configurable: true,
    writable: true,
  });
  return { calls, removed };
}

describe('TurnstileWidget', () => {
  beforeEach(() => {
    document.getElementById('cf-turnstile-script')?.remove();
  });

  afterEach(() => {
    delete (window as { turnstile?: unknown }).turnstile;
    vi.unstubAllEnvs();
  });

  it('renders nothing and reports a null token exactly once when the site key is unset', () => {
    vi.stubEnv('NEXT_PUBLIC_TURNSTILE_SITE_KEY', '');

    const onToken = vi.fn();
    const { container } = render(<TurnstileWidget onToken={onToken} />);

    expect(container).toBeEmptyDOMElement();
    expect(onToken).toHaveBeenCalledTimes(1);
    expect(onToken).toHaveBeenCalledWith(null);
  });

  it('renders the explicit-mode challenge and wires success/error/expire callbacks', () => {
    vi.stubEnv('NEXT_PUBLIC_TURNSTILE_SITE_KEY', 'site-key-123');
    const { calls } = stubTurnstile();

    const onToken = vi.fn();
    const { container, unmount } = render(
      <TurnstileWidget onToken={onToken} action="sign-in" id="sign-in-turnstile" />,
    );

    expect(container.querySelector('#sign-in-turnstile-container')).not.toBeNull();
    expect(calls).toHaveLength(1);
    expect(calls[0].container.id).toBe('sign-in-turnstile-container');
    expect(calls[0].options.sitekey).toBe('site-key-123');
    expect(calls[0].options.action).toBe('sign-in');

    const options = calls[0].options as {
      callback: (token: string) => void;
      'error-callback': () => void;
      'expired-callback': () => void;
    };
    options.callback('tok');
    expect(onToken).toHaveBeenLastCalledWith('tok');
    options['error-callback']();
    expect(onToken).toHaveBeenLastCalledWith(null);
    options['expired-callback']();
    expect(onToken).toHaveBeenLastCalledWith(null);
    expect(onToken).toHaveBeenCalledTimes(3);

    unmount();
  });

  it('appends the Turnstile script once and removes the widget on unmount', () => {
    vi.stubEnv('NEXT_PUBLIC_TURNSTILE_SITE_KEY', 'site-key-123');
    const { removed } = stubTurnstile();

    const { unmount } = render(<TurnstileWidget onToken={vi.fn()} />);
    const script = document.getElementById('cf-turnstile-script');
    expect(script).not.toBeNull();
    expect(script?.getAttribute('src')).toBe(
      'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit',
    );

    unmount();
    expect(removed).toHaveLength(1);
    expect(removed[0]).toBe('widget-0');
  });
});
