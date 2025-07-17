'use client';

import React, { ComponentProps } from 'react';
import { ThemeProvider as NextThemesProvider } from 'next-themes';

export const ThemeProvider = ({ children, ...props }: ComponentProps<typeof NextThemesProvider>): React.JSX.Element => {
  return <NextThemesProvider {...props}>{children}</NextThemesProvider>;
};
