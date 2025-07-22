'use client';

import { ReactNode } from 'react';

interface LayoutProps {
  children: ReactNode;
  modal: ReactNode;
}

export default function SessionLayout({ children, modal }: LayoutProps) {
  console.log('SessionLayout rendered with modal:', !!modal);
  
  return (
    <>
      {children}
      {modal}
    </>
  );
}
