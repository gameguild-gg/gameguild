/**
 * Web3 Provider exports
 */

// Main provider and context
export { Web3Provider, Web3Context, useWeb3Context, useWeb3Actions, useWeb3State, useWeb3Connection, useWeb3Network } from './context/web3-context';

// Types
export type { Web3State, Web3Config, Web3ContextValue, Web3ProviderProps, NetworkInfo, Web3Action, EthereumProvider } from './types';

export { Web3ActionTypes, defaultWeb3State, defaultWeb3Config, defaultSupportedNetworks } from './types';

// Reducer and utilities
export { web3Reducer, createInitialWeb3State, isNetworkSupported, getNetworkInfo, formatChainId, persistWeb3State, clearPersistedWeb3State } from './context/web3-reducer';
