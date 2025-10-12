import React from 'react';
import { GalleryVerticalEnd } from 'lucide-react';

export default async function Layout({ children }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  return (
    <>
      <div className="bg-muted flex min-h-svh flex-col items-center justify-center gap-8 p-8 md:p-8">
        <div className="flex w-full max-w-sm flex-col gap-8">
          <div className="flex items-center justify-center gap-4">
            <div className="bg-primary text-primary-foreground flex size-12 items-center justify-center rounded-md">
              <GalleryVerticalEnd className="size-6" />
            </div>

            <div className="flex flex-col leading-tight">
              <span className="text-xl font-medium">Game Guild</span>
              <span>Console</span>
            </div>
          </div>
          {children}
        </div>
      </div>
    </>
  );
}
