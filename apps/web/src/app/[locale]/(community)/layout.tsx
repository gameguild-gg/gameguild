import React, { PropsWithChildren } from 'react';
import Footer from '@/components/common/footer';
import Header from '@/components/common/header';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <div className="flex flex-col flex-1">
      <Header />
      <div className="flex flex-col flex-1">{children}</div>
      <Footer />
    </div>
  );
}
