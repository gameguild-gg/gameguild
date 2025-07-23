// Error boundaries must be Client Components.
'use client';

import React from 'react';
import { ErrorProps } from '@/types';
import { Error } from '@/components/errors/error';

export default function GlobalError({ error, reset }: ErrorProps): React.JSX.Element {
  return (
    // global-error must include html and body tags.
    <html lang="en">
      <body>
        <Error error={error} reset={reset} />;
      </body>
    </html>
  );
}
