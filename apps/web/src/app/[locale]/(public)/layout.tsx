import { AppShell } from '@/components/app/app-shell';
import React from 'react';

export default async function Layout({ children }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  return await AppShell({ children });
}
