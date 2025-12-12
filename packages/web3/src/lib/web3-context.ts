import { createContext } from 'react';
import { defaultWeb3State } from './web3-reducer';

export interface Web3ContextValue {}

export const defaultWeb3ContextValue: Web3ContextValue = {
  state: defaultWeb3State,
  dispatch: () => console.error('Web3Context used outside of provider'),
  connect: async () => console.error('Web3Context used outside of provider'),
  disconnect: () => console.error('Web3Context used outside of provider'),
  reconnect: async () => console.error('Web3Context used outside of provider'),
  switchNetwork: async () => console.error('Web3Context used outside of provider'),
  addNetwork: async () => console.error('Web3Context used outside of provider'),
  updateConfig: () => console.error('Web3Context used outside of provider'),
  isNetworkSupported: () => false,
  getNetworkInfo: () => undefined,
  clearError: () => console.error('Web3Context used outside of provider'),
  resetState: () => console.error('Web3Context used outside of provider'),
};

export const Web3Context = createContext<Web3ContextValue>(defaultWeb3ContextValue);
