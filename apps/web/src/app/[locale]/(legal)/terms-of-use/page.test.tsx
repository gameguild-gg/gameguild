import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';

import Page from './page';

describe('/terms-of-use', () => {
  afterEach(cleanup);

  it('renders the policy page', async () => {
    render(await Page({ params: Promise.resolve({ locale: 'en-US' }) } as never));
    expect(screen.getByRole('heading')).toBeInTheDocument();
  });
});
