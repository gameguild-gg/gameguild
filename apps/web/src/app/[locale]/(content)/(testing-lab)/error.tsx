// Error boundaries must be Client Components.
'use client';

import { Error } from '@/components/common/errors';
import { ErrorProps } from '@/types';
import React from 'react';

export default function Page({ error, reset }: ErrorProps): React.JSX.Element {
  return <Error error={error} reset={reset} />;
}
