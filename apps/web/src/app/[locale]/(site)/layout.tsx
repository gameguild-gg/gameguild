import { PublicWebsiteShell } from '@/components/site/public-website-shell';
import type React from 'react';
import type { ReactNode } from 'react';

export default async function Layout({ children }: { readonly children: ReactNode }): Promise<React.JSX.Element> {
  return <PublicWebsiteShell>{children}</PublicWebsiteShell>;
}
