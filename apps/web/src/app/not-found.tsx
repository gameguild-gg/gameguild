import React from 'react';
import { PageNotFound } from '@/components/errors/page-not-found';

export default function NotFound() {
  return (
    // root not-found must include html and body tags.
    <html lang="en">
      <body>
        <PageNotFound />
      </body>
    </html>
  );
}
