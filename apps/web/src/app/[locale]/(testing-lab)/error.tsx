// Error boundaries must be Client Components.
'use client';

import React from 'react';
import { ErrorMessage } from '@/components/errors/error-message';
import { ErrorProps } from '@/types';

export default function Error({ error, reset }: ErrorProps): React.JSX.Element {
  return <ErrorMessage error={error} reset={reset} />;
}
