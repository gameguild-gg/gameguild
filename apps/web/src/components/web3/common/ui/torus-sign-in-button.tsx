'use client';

import { Button } from '@/components/ui/button';
import Image from 'next/image';
import React from 'react';

export default function TorusSignInButton() {
  // const [signInWithWeb3] = useSignInWithWeb3();

  // const signInAndRedirectIfSucceed = async () => {
  //   const session = await getSession();
  //   if (!session) await signInWithWeb3();
  //   if (session) window.location.replace('/feed');
  // };

  return (
    <Button
      variant="outline"
      onClick={() => {
        alert('Torus is not supported yet! Help us develop this feature! Talk to us on Discord!');
      }}
      className="flex-1"
    >
      <Image alt="Torus" src="/assets/images/torus-icon.svg" width={20} height={20} className="m-2" />
      Torus
    </Button>
  );
}
