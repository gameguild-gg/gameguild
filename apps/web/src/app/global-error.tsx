// Error boundaries must be Client Components.
'use client';

import React, { useEffect } from 'react';
import { ErrorProps } from '@/types';
import Link from 'next/link';

export default function GlobalError({ error, reset }: ErrorProps): React.JSX.Element {
  const isDevelopment = process.env.NODE_ENV === 'development';

  useEffect(() => {
    // Log the error to an error reporting service.
    console.error(error);
  }, [error]);

  return (
    // global-error must include HTML and body tags.
    <html lang="en">
      <body>
        <div>
          <h2>Something went wrong!</h2>
          <p>We apologize for the inconvenience. An unexpected error has occurred.</p>
          {isDevelopment && (
            <div>
              <h3>Error Details:</h3>
              <pre>{error.message}</pre>
              <pre>{error.stack}</pre>
            </div>
          )}
          <div>
            {/* Attempt to recover by trying to re-render the segment */}
            <button onClick={() => reset()}>Try again</button>
            <button onClick={() => (window.location.href = '/')}>Go Home</button>
            <button onClick={() => window.history.back()}>Go Back</button>
          </div>
          <div>
            <p>If the problem persists, please contact support.</p>
            <Link href="/contact">Contact Support</Link>
            <p>Thank you for your patience!</p>
          </div>
        </div>
      </body>
    </html>
  );
}
