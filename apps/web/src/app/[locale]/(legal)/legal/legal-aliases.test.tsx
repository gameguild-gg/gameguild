import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';

import PrivacyPage from './privacy/page';
import TermsOfServicePage from './terms-of-service/page';

describe('public legal compatibility routes', () => {
  afterEach(cleanup);

  it('renders terms at the consent URL', async () => {
    render(await TermsOfServicePage({ params: Promise.resolve({ locale: 'en-US' }) } as never));

    expect(screen.getByRole('heading', { name: 'Terms of Service' })).toBeInTheDocument();
  });

  it('renders privacy at the consent URL', async () => {
    render(await PrivacyPage({ params: Promise.resolve({ locale: 'en-US' }) } as never));

    expect(screen.getByRole('heading', { name: 'Privacy Policy' })).toBeInTheDocument();
  });
});
