'use client';

import React from 'react';

type Props = {
  error: Error & { digest?: string };
  reset: () => void;
};

export default function Error({ error, reset }: Readonly<Props>) {
  return (
    <div>
      <button onClick={reset}>Reset</button>
    </div>
  );
}
