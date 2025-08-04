'use client';

import { Button } from '@/components/ui/button';
import React from 'react';
import Image from 'next/image';

import { useSignInWithWeb3, Web3ProviderChoice } from '@/components/web3/hooks/use-sign-in-with-web3';

export default function MetaMaskSignInButton() {
  const [signInWithWeb3] = useSignInWithWeb3(Web3ProviderChoice.METAMASK);

  return (
    <Button variant="outline" onClick={signInWithWeb3} className="flex-1">
      <Image alt="MetaMask" src="/assets/images/metamask-icon.svg" width={20} height={20} className="m-2" />
      Metamask
    </Button>
  );
}
