import React from 'react';
import { NotFound } from '@/components/errors/not-found';

export default function NotFound() {
  return (
    // root not-found must include html and body tags.
    <html lang="en">
      <body>
        <NotFound />
      </body>
    </html>
  );
}
