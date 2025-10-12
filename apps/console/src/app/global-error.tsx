// Error boundaries must be Client Components.
'use client';

import Link from 'next/link';
import React, { useEffect } from 'react';

interface ErrorProps {
  error: Error & { digest?: string };

  reset: () => void;
}

export default function GlobalError({ error, reset }: ErrorProps): React.JSX.Element {
  useEffect(() => {
    // Log the error to an error reporting service.
    console.error(error);
  }, [error]);

  return (
    <div>
      <h2>Something went wrong!</h2>
      <p>We apologize for the inconvenience. An unexpected error has occurred.</p>
      {/*TODO: encapsulate the process.env.NODE_ENV check in a utility function.*/}
      {process.env.NODE_ENV === 'development' && (
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
  );
}
