'use client';

import { useEffect } from 'react';

export function DevelopmentReactDiagnostics() {
  useEffect(() => {
    if (
      process.env.NODE_ENV !== 'development' ||
      process.env.NEXT_PUBLIC_DISABLE_REACT_DEVTOOLS === '1'
    ) {
      return;
    }

    void import('react-scan/auto');
  }, []);

  return null;
}
