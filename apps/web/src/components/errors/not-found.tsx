'use client';

import React from 'react';

export const NotFound = (): React.JSX.Element => (
  <div>
    <h2>Not Found</h2>
    <p>The page you're looking for doesn't exist or has been moved.</p>
    <div>
      {/* TODO: Add a search component here.*/}
      <p>Try searching for what you need or return to the homepage.</p>
    </div>
    {/* TODO: Move only the navigation to a client component. */}
    <div>
      <button onClick={() => (window.location.href = '/')}>Go Home</button>
      <button onClick={() => window.history.back()}>Go Back</button>
    </div>
    <div>
      <p>If you believe this is an error, please contact support or try again later.</p>
      <a href="/contact">Contact Support</a>
      <p>Thank you for your patience!</p>
    </div>
  </div>
);
