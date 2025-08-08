/**
 * Web3 Reducer Implementation
 */

import { defaultWeb3State, NetworkInfo, Web3Action, Web3ActionTypes, Web3State } from '../types';

/**
 * Web3 reducer function to handle state updates
 */
export const web3Reducer = (state: Web3State, action: Web3Action): Web3State => {
  switch (action.type) {
    case Web3ActionTypes.CHECK_PROVIDER: {
      return {
        ...state,
        isProviderChecked: false,
        connectionStatus: 'idle',
        error: undefined,
      };
    }

    case Web3ActionTypes.PROVIDER_AVAILABLE: {
      return {
        ...state,
        isProviderAvailable: true,
        isProviderChecked: true,
        error: undefined,
      };
    }

    case Web3ActionTypes.PROVIDER_UNAVAILABLE: {
      return {
        ...state,
        isProviderAvailable: false,
        isProviderChecked: true,
        connectionStatus: 'error',
        error: action.payload.error,
      };
    }

    case Web3ActionTypes.CONNECT_START: {
      return {
        ...state,
        isConnecting: true,
        connectionStatus: 'connecting',
        error: undefined,
      };
    }

    case Web3ActionTypes.CONNECT_SUCCESS: {
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

    case Web3ActionTypes.CONNECT_FAILURE: {
      return {
        ...state,
        isConnecting: false,
        isConnected: false,
        connectionStatus: 'error',
        error: action.payload.error,
        persistedConnection: false,
      };
    }

    case Web3ActionTypes.ACCOUNT_CHANGED: {
      return {
        ...state,
        accountAddress: action.payload.accountAddress,
        error: undefined,
      };
    }

    case Web3ActionTypes.DISCONNECT: {
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

    case Web3ActionTypes.SET_NETWORK: {
      return {
        ...state,
        network: action.payload.network,
        error: undefined,
      };
    }

    case Web3ActionTypes.NETWORK_CHANGED: {
      return {
        ...state,
        network: action.payload.network,
        error: undefined,
      };
    }

    case Web3ActionTypes.UPDATE_CONFIG: {
      return {
        ...state,
        config: { ...state.config, ...action.payload },
      };
    }

    case Web3ActionTypes.RESET_STATE: {
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

/**
 * Initialize Web3 state with potential localStorage/cookie values
 */
export const createInitialWeb3State = (initialState: Partial<Web3State>): Web3State => {
  let enhancedState = { ...defaultWeb3State, ...initialState };

  // Check for persisted configuration in localStorage (client-side only)
  if (typeof window !== 'undefined' && enhancedState.config.persistConnection) {
    try {
      const persistedState = localStorage.getItem('web3-state');
      if (persistedState) {
        const parsed = JSON.parse(persistedState);

        // Only restore certain safe properties
        if (parsed.lastConnected && parsed.persistedConnection) {
          enhancedState = {
            ...enhancedState,
            persistedConnection: parsed.persistedConnection,
            lastConnected: parsed.lastConnected,
            config: {
              ...enhancedState.config,
              ...parsed.config,
            },
          };
        }
      }
    } catch (error) {
      console.warn('Failed to load Web3 configuration from localStorage:', error);
    }
  }

  return enhancedState;
};

/**
 * Helper function to determine if a network is supported
 */
export const isNetworkSupported = (chainId: string, supportedNetworks: NetworkInfo[]): boolean => {
  return supportedNetworks.some((network) => network.chainId === chainId);
};

/**
 * Helper function to get network info by chain ID
 */
export const getNetworkInfo = (chainId: string, supportedNetworks: NetworkInfo[]): NetworkInfo | undefined => {
  return supportedNetworks.find((network) => network.chainId === chainId);
};

/** 
 * Helper function to format chain ID
 */
export const formatChainId = (chainId: string | number | bigint): string => {
  if (typeof chainId === 'bigint') {
    return `0x${chainId.toString(16)}`;
  }
  if (typeof chainId === 'number') {
    return `0x${chainId.toString(16)}`;
  }
  return chainId.startsWith('0x') ? chainId : `0x${parseInt(chainId, 10).toString(16)}`;
};

/**
 * Helper function to save Web3 state to localStorage
 */
export const persistWeb3State = (state: Web3State): void => {
  if (typeof window !== 'undefined' && state.config.persistConnection) {
    try {
      const stateToSave = {
        persistedConnection: state.persistedConnection,
        lastConnected: state.lastConnected,
        config: state.config,
      };
      localStorage.setItem('web3-state', JSON.stringify(stateToSave));
    } catch (error) {
      console.warn('Failed to persist Web3 state to localStorage:', error);
    }
  }
};

/**
 * Helper function to clear persisted Web3 state
 */
export const clearPersistedWeb3State = (): void => {
  if (typeof window !== 'undefined') {
    try {
      localStorage.removeItem('web3-state');
    } catch (error) {
      console.warn('Failed to clear persisted Web3 state:', error);
    }
  }
};
