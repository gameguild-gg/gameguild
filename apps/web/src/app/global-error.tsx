'use client';

import React, {useEffect} from 'react';
import Link from 'next/link';
import {ErrorProps} from '@/lib/types';

export default function GlobalError({error, reset}: ErrorProps): React.JSX.Element {
  const isDevelopment = process.env.NODE_ENV === 'development';

  useEffect(() => {
    // Log the error to an error reporting service.
    console.error(error);
  }, [error]);

  return (
    <html lang="en">
    <body>
    <div className="min-h-screen bg-gray-950 text-white flex items-center justify-center">
      <div className="max-w-md mx-auto text-center p-6">
        <h2 className="text-2xl font-bold text-red-400 mb-4">Something went wrong!</h2>
        <p className="text-gray-300 mb-6">We apologize for the inconvenience. An unexpected error has occurred.</p>
        {isDevelopment && (
          <div className="mb-6 text-left">
            <h3 className="text-lg font-semibold mb-2">Error Details:</h3>
            <pre className="text-sm bg-gray-800 p-3 rounded overflow-auto">{error.message}</pre>
            <pre className="text-sm bg-gray-800 p-3 rounded overflow-auto mt-2">{error.stack}</pre>
          </div>
        )}
        <div className="space-y-3">
          {/* Attempt to recover by trying to re-render the segment */}
          <button
            onClick={() => reset()}
            className="w-full bg-blue-600 hover:bg-blue-700 text-white py-2 px-4 rounded"
          >
            Try again
          </button>
          <button
            onClick={() => (window.location.href = '/')}
            className="w-full bg-gray-600 hover:bg-gray-700 text-white py-2 px-4 rounded"
          >
            Go Home
          </button>
          <button
            onClick={() => window.history.back()}
            className="w-full bg-gray-600 hover:bg-gray-700 text-white py-2 px-4 rounded"
          >
            Go Back
          </button>
        </div>
        <div className="mt-6">
          <p className="text-gray-300 mb-2">If the problem persists, please contact support.</p>
          <Link href="/contact" className="text-blue-400 hover:text-blue-300">Contact Support</Link>
          <p className="text-gray-300 mt-2">Thank you for your patience!</p>
        </div>
      </div>
    </div>
    </body>
    </html>
  );
}
