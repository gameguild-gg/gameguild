import { renderToString } from 'react-dom/server';
import { NextIntlClientProvider } from 'next-intl';
import { describe, expect, it } from 'vitest';

import { TestingLabOperationsNavigation } from './testing-lab-page-header';

describe('TestingLabOperationsNavigation', () => {
  it('renders through the server without falling back to client rendering', () => {
    expect(() =>
      renderToString(
        <NextIntlClientProvider locale="en-US" messages={{}}>
          <TestingLabOperationsNavigation />
        </NextIntlClientProvider>,
      ),
    ).not.toThrow();
  });
});
