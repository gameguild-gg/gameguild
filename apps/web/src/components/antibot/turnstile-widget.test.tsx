import { afterEach, describe, expect, it, vi } from 'vitest';
import { render } from '@testing-library/react';

import { TurnstileWidget } from './turnstile-widget';

describe('TurnstileWidget', () => {
  afterEach(() => {
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
});
