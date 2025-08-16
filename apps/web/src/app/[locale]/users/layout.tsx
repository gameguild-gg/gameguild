import Footer from '@/components/common/footer/default-footer';
import Header from '@/components/common/header/default-header';
import React, { PropsWithChildren } from 'react';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <div className="flex flex-col flex-1">
  <Header className="bg-slate-900/70 dark:bg-slate-900/70 border-slate-800/60 text-white" />
      <div className="flex flex-col flex-1">
        {children}
      </div>
      <Footer />
    </div>
  );
}
