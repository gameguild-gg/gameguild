import { BrowserProvider } from 'ethers';
import { Web3Config, NetworkInfo } from '../types';

export const Web3ActionType = {
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

export type Web3ActionType = (typeof Web3ActionType)[keyof typeof Web3ActionType];

export type Web3Action =
  | { type: typeof Web3ActionType.CHECK_PROVIDER }
  | { type: typeof Web3ActionType.PROVIDER_AVAILABLE }
  | { type: typeof Web3ActionType.PROVIDER_UNAVAILABLE; payload: { error: string } }
  | { type: typeof Web3ActionType.CONNECT_START }
  | { type: typeof Web3ActionType.CONNECT_SUCCESS; payload: { provider: BrowserProvider; accountAddress: string; network?: NetworkInfo } }
  | { type: typeof Web3ActionType.CONNECT_FAILURE; payload: { error: string } }
  | { type: typeof Web3ActionType.ACCOUNT_CHANGED; payload: { accountAddress: string } }
  | { type: typeof Web3ActionType.DISCONNECT }
  | { type: typeof Web3ActionType.SET_NETWORK; payload: { network: NetworkInfo } }
  | { type: typeof Web3ActionType.NETWORK_CHANGED; payload: { network: NetworkInfo } }
  | { type: typeof Web3ActionType.RESET_STATE }
  | { type: typeof Web3ActionType.UPDATE_CONFIG; payload: Partial<Web3Config> };

export type Web3Reducer = (state: Web3State, action: Web3Action) => Web3State;

export type Web3State = {};

export const defaultWeb3State: Web3State = {};

export const web3Reducer: Web3Reducer = (state: Web3State, action: Web3Action): Web3State => {
  switch (action.type) {
    //
    case Web3ActionType.CHECK_PROVIDER: {
      return {
        ...state,
        isProviderChecked: false,
        connectionStatus: 'idle',
        error: undefined,
      };
    }
    //
    case Web3ActionType.PROVIDER_AVAILABLE: {
      return {
        ...state,
        isProviderAvailable: true,
        isProviderChecked: true,
        error: undefined,
      };
    }
    //
    case Web3ActionType.PROVIDER_UNAVAILABLE: {
      return {
        ...state,
        isProviderAvailable: false,
        isProviderChecked: true,
        connectionStatus: 'error',
        error: action.payload.error,
      };
    }
    //
    case Web3ActionType.CONNECT_START: {
      return {
        ...state,
        isConnecting: true,
        connectionStatus: 'connecting',
        error: undefined,
      };
    }
    //
    case Web3ActionType.CONNECT_SUCCESS: {
      const now = Date.now();
      return {
        ...state,
        isConnecting: false,
        isConnected: true,
        provider: action.payload.provider,
        accountAddress: action.payload.accountAddress,
        network: action.payload.network,
        connectionStatus: 'connected',
        error: undefined,
        persistedConnection: state.config.persistConnection ?? true,
        lastConnected: now,
      };
    }
    //
    case Web3ActionType.CONNECT_FAILURE: {
      return {
        ...state,
        isConnecting: false,
        isConnected: false,
        connectionStatus: 'error',
        error: action.payload.error,
        persistedConnection: false,
      };
    }
    //
    case Web3ActionType.ACCOUNT_CHANGED: {
      return {
        ...state,
        accountAddress: action.payload.accountAddress,
        error: undefined,
      };
    }
    //
    case Web3ActionType.DISCONNECT: {
      return {
        ...state,
        provider: undefined,
        accountAddress: undefined,
        network: undefined,
        isConnecting: false,
        isConnected: false,
        connectionStatus: 'disconnected',
        error: undefined,
        persistedConnection: false,
      };
    }
    //
    case Web3ActionType.SET_NETWORK: {
      return {
        ...state,
        network: action.payload.network,
        error: undefined,
      };
    }
    //
    case Web3ActionType.NETWORK_CHANGED: {
      return {
        ...state,
        network: action.payload.network,
        error: undefined,
      };
    }
    //
    case Web3ActionType.UPDATE_CONFIG: {
      return {
        ...state,
        config: { ...state.config, ...action.payload },
      };
    }
    //
    case Web3ActionType.RESET_STATE: {
      return {
        ...defaultWeb3State,
        config: state.config, // Preserve config on reset
        supportedNetworks: state.supportedNetworks, // Preserve supported networks
      };
    }
    default: {
      console.warn(`Unhandled Web3 action type: ${(action as { type: string }).type}`);
      return state;
    }
  }
};
