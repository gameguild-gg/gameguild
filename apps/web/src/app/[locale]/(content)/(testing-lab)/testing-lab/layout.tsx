import {PropsWithModal} from '@/types';
import React, {PropsWithChildren} from 'react';

export default function Layout({ children, modal }: PropsWithChildren<PropsWithModal>): React.JSX.Element {
  return (
    <>
      {children}
      {modal}
    </>
  );
}
