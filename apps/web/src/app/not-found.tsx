import React from 'react';
import { NotFound } from '@/components/common/errors/not-found';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    // root not-found must include HTML and body tags.
    <html lang="en">
      <body>
        <NotFound />
      </body>
    </html>
  );
}
