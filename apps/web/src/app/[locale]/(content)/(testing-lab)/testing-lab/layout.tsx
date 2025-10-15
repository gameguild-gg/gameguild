import React, {PropsWithChildren} from 'react';
import {PropsWithModal} from '@/types';

export default function Layout({children, modal}: PropsWithChildren<PropsWithModal>): React.JSX.Element {
  return (
    <>
      {children}
      {modal}
    </>
  );
}
