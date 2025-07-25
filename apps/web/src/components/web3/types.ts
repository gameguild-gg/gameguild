/**
 * Types and interfaces for Web3 Provider
 */

import { BrowserProvider } from 'ethers';
import { ReactNode } from 'react';

/**
 * Web3 Action Types
 */
export const Web3ActionTypes = {
  CHECK_PROVIDER: 'CHECK_PROVIDER',
  PROVIDER_AVAILABLE: 'PROVIDER_AVAILABLE',
  PROVIDER_UNAVAILABLE: 'PROVIDER_UNAVAILABLE',
  CONNECT_START: 'CONNECT_START',
  CONNECT_SUCCESS: 'CONNECT_SUCCESS',
  CONNECT_FAILURE: 'CONNECT_FAILURE',
  ACCOUNT_CHANGED: 'ACCOUNT_CHANGED',
  DISCONNECT: 'DISCONNECT',
  SET_NETWORK: 'SET_NETWORK',
  NETWORK_CHANGED: 'NETWORK_CHANGED',
  RESET_STATE: 'RESET_STATE',
  UPDATE_CONFIG: 'UPDATE_CONFIG',
} as const;

/**
 * Network information
 */
export interface NetworkInfo {
  chainId: string;
  name: string;
  rpcUrl?: string;
  blockExplorerUrl?: string;
}

/**
 * Web3 Configuration
 */
export interface Web3Config {
  autoConnect?: boolean;
  persistConnection?: boolean;
  supportedChainIds?: string[];
  defaultChainId?: string;
  enableAccountChangeTracking?: boolean;
  enableNetworkChangeTracking?: boolean;
  reconnectAttempts?: number;
  reconnectDelay?: number;
}

/**
 * Web3 State
 */
export interface Web3State {
  // Connection state
  provider: BrowserProvider | undefined;
  accountAddress: string | undefined;
  isConnecting: boolean;
  isConnected: boolean;
  isProviderAvailable: boolean;
  isProviderChecked: boolean;
  
  // Network state
  network: NetworkInfo | undefined;
  supportedNetworks: NetworkInfo[];
  
  // Error and status
  error: string | undefined;
  connectionStatus: 'idle' | 'connecting' | 'connected' | 'disconnected' | 'error';
  
  // Configuration
  config: Web3Config;
  
  // Persistence
  persistedConnection: boolean;
  lastConnected: number | undefined;
}

/**
 * Web3 Actions
 */
export type Web3Action =
  | { type: typeof Web3ActionTypes.CHECK_PROVIDER }
  | { type: typeof Web3ActionTypes.PROVIDER_AVAILABLE }
  | { type: typeof Web3ActionTypes.PROVIDER_UNAVAILABLE; payload: { error: string } }
  | { type: typeof Web3ActionTypes.CONNECT_START }
  | { type: typeof Web3ActionTypes.CONNECT_SUCCESS; payload: { provider: BrowserProvider; accountAddress: string; network?: NetworkInfo } }
  | { type: typeof Web3ActionTypes.CONNECT_FAILURE; payload: { error: string } }
  | { type: typeof Web3ActionTypes.ACCOUNT_CHANGED; payload: { accountAddress: string } }
  | { type: typeof Web3ActionTypes.DISCONNECT }
  | { type: typeof Web3ActionTypes.SET_NETWORK; payload: { network: NetworkInfo } }
  | { type: typeof Web3ActionTypes.NETWORK_CHANGED; payload: { network: NetworkInfo } }
  | { type: typeof Web3ActionTypes.RESET_STATE }
  | { type: typeof Web3ActionTypes.UPDATE_CONFIG; payload: Partial<Web3Config> };

/**
 * Web3 Reducer
 */
export type Web3Reducer = (state: Web3State, action: Web3Action) => Web3State;

/**
 * Web3 Context Value
 */
export interface Web3ContextValue {
  state: Web3State;
  dispatch: React.Dispatch<Web3Action>;
  
  // Connection methods
  connect: () => Promise<void>;
  disconnect: () => void;
  reconnect: () => Promise<void>;
  
  // Network methods
  switchNetwork: (chainId: string) => Promise<void>;
  addNetwork: (network: NetworkInfo) => Promise<void>;
  
  // Configuration methods
  updateConfig: (config: Partial<Web3Config>) => void;
  
  // Utility methods
  isNetworkSupported: (chainId: string) => boolean;
  getNetworkInfo: (chainId: string) => NetworkInfo | undefined;
  clearError: () => void;
  resetState: () => void;
}

/**
 * Web3 Provider Props
 */
export interface Web3ProviderProps {
  children: ReactNode;
  config?: Partial<Web3Config>;
  initialState?: Partial<Web3State>;
  supportedNetworks?: NetworkInfo[];
}

/**
 * Default Web3 Configuration
 */
export const defaultWeb3Config: Web3Config = {
  autoConnect: false,
  persistConnection: true,
  supportedChainIds: ['0x1', '0x89', '0xa4b1'], // Ethereum, Polygon, Arbitrum
  defaultChainId: '0x1',
  enableAccountChangeTracking: true,
  enableNetworkChangeTracking: true,
  reconnectAttempts: 3,
  reconnectDelay: 1000,
};

/**
 * Default supported networks
 */
export const defaultSupportedNetworks: NetworkInfo[] = [
  {
    chainId: '0x1',
    name: 'Ethereum Mainnet',
    rpcUrl: 'https://eth-mainnet.g.alchemy.com/v2/',
    blockExplorerUrl: 'https://etherscan.io',
  },
  {
    chainId: '0x89',
    name: 'Polygon',
    rpcUrl: 'https://polygon-rpc.com',
    blockExplorerUrl: 'https://polygonscan.com',
  },
  {
    chainId: '0xa4b1',
    name: 'Arbitrum One',
    rpcUrl: 'https://arb1.arbitrum.io/rpc',
    blockExplorerUrl: 'https://arbiscan.io',
  },
];

/**
 * Default Web3 State
 */
export const defaultWeb3State: Web3State = {
  provider: undefined,
  accountAddress: undefined,
  isConnecting: false,
  isConnected: false,
  isProviderAvailable: false,
  isProviderChecked: false,
  network: undefined,
  supportedNetworks: defaultSupportedNetworks,
  error: undefined,
  connectionStatus: 'idle',
  config: defaultWeb3Config,
  persistedConnection: false,
  lastConnected: undefined,
};

/**
 * Ethereum Provider Interface
 */
export interface EthereumProvider {
  request: (args: { method: string; params?: unknown[] }) => Promise<unknown>;
  on: (event: string, listener: (...args: unknown[]) => void) => void;
  removeListener: (event: string, listener: (...args: unknown[]) => void) => void;
  isMetaMask?: boolean;
  chainId?: string;
}

/**
 * Window interface extension
 */
declare global {
  interface Window {
    ethereum?: EthereumProvider;
  }
}
