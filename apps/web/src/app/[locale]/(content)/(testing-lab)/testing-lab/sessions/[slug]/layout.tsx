import React, { PropsWithChildren } from 'react';

export default function Layout({ children, modal }: PropsWithChildren<{ modal: React.ReactNode }>): React.JSX.Element {
  return (
    <>
      {children}
      {modal}
    </>
  );
}
