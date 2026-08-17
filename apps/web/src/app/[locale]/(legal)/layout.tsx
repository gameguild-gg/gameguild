import { LegalShell } from '@/components/legal/legal-shell';
import React from 'react';

export default async function Layout({ children }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  return await LegalShell({ children });
}
