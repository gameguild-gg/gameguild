// Error boundaries must be Client Components.
'use client';

import React from 'react';
import { Error } from '@/components/common/error/error';
import { ErrorProps } from '@/types';

export default function Page({ error, reset }: ErrorProps): React.JSX.Element {
  return <Error error={error} reset={reset} />;
}
