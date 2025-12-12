'use client';

import { useContext } from 'react';
import { defaultWeb3ContextValue, Web3Context, Web3ContextValue } from '../lib/web3-context';

export function useWeb3(): Web3ContextValue {
  const context = useContext(Web3Context);

  if (context === defaultWeb3ContextValue) throw new Error('useWeb3 must be used within a Web3Provider');

  return context;
}
