import React from 'react';
import { Web3Context } from '@/components/web3/context/web3-context';

/**
 * Legacy hook for backward compatibility
 * @deprecated Use useWeb3Context, useWeb3Connection, or useWeb3Network instead
 */
export function useWeb3() {
  const context = React.useContext(Web3Context);

  if (context === undefined) {
    throw new Error('useWeb3 must be used within a Web3Provider');
  }

  // Return legacy-compatible interface
  return {
    state: context.state,
    connect: context.connect,
    disconnect: context.disconnect,
  };
}
