import React from 'react';
import { Link } from '@/i18n/navigation';

export default async function NotFound(): Promise<React.JSX.Element> {
  return (
    <div>
      <div>
        <h1>404</h1>
        <p>Page not found</p>
      </div>
      <Link href="/">Go to Home</Link>
    </div>
  );
}
