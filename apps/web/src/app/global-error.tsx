// Error boundaries must be Client Components.
'use client';

import React from 'react';
import { ErrorProps } from '@/types';
import { ErrorMessage } from '@/components/errors/error-message';

export default function GlobalError({ error, reset }: ErrorProps): React.JSX.Element {
  return (
    // global-error must include html and body tags.
    <html lang="en">
      <body>
        <ErrorMessage error={error} reset={reset} />;
      </body>
    </html>
  );
}
