'use client';

import React, { createContext, useCallback, useContext, useEffect, useReducer } from 'react';
import { BrowserProvider } from 'ethers';
import { Web3ActionTypes, Web3ContextValue, Web3ProviderProps, Web3State, Web3Config, defaultWeb3State, defaultSupportedNetworks, NetworkInfo } from '../types';
import { web3Reducer, createInitialWeb3State, isNetworkSupported, getNetworkInfo, formatChainId, persistWeb3State, clearPersistedWeb3State } from './web3-reducer';

/**
 * Default context to handle errors when context is used outside the provider
 */
const defaultContextValue: Web3ContextValue = {
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

const Web3Context = createContext<Web3ContextValue>(defaultContextValue);

/**
 * Provider that manages Web3 state using a reducer
 */
export function Web3Provider({ children, config = {}, initialState = defaultWeb3State, supportedNetworks = defaultSupportedNetworks }: Web3ProviderProps) {
  // Merge configurations: default < initial state < provided config
  const mergedConfig = {
    ...defaultWeb3State.config,
    ...initialState?.config,
    ...config,
  };

  const stateWithConfig: Partial<Web3State> = {
    ...defaultWeb3State,
    ...initialState,
    config: mergedConfig,
    supportedNetworks,
  };

  const [state, dispatch] = useReducer(web3Reducer, stateWithConfig as Web3State, createInitialWeb3State);

  // Auto-connect if enabled and we have persisted connection
  useEffect(() => {
    if (state.config.autoConnect && state.persistedConnection && !state.isConnected) {
      connect();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [state.config.autoConnect, state.persistedConnection, state.isConnected]);

  // Persist state changes to localStorage
  useEffect(() => {
    if (state.config.persistConnection) {
      persistWeb3State(state);
    }
  }, [state]);

  // Check if provider is available and set up event listeners
  useEffect(() => {
    dispatch({ type: Web3ActionTypes.CHECK_PROVIDER });

    if (window.ethereum) {
      dispatch({ type: Web3ActionTypes.PROVIDER_AVAILABLE });

      // Set up event listeners
      const handleAccountsChanged = (...args: unknown[]) => {
        const accounts = args[0] as string[];
        if (!state.config.enableAccountChangeTracking) return;

        if (accounts.length === 0) {
          // User disconnected
          dispatch({ type: Web3ActionTypes.DISCONNECT });
        } else if (state.accountAddress && accounts[0] && accounts[0] !== state.accountAddress) {
          // Account changed
          dispatch({
            type: Web3ActionTypes.ACCOUNT_CHANGED,
            payload: { accountAddress: accounts[0] },
          });
        }
      };

      const handleChainChanged = async (...args: unknown[]) => {
        const chainId = args[0] as string;
        if (!state.config.enableNetworkChangeTracking) return;

        const formattedChainId = formatChainId(chainId);
        const networkInfo = getNetworkInfo(formattedChainId, state.supportedNetworks);

        if (networkInfo) {
          dispatch({
            type: Web3ActionTypes.NETWORK_CHANGED,
            payload: { network: networkInfo },
          });
        }
      };

      // Add event listeners
      window.ethereum.on('accountsChanged', handleAccountsChanged);
      window.ethereum.on('chainChanged', handleChainChanged);

      return () => {
        if (window.ethereum) {
          window.ethereum.removeListener('accountsChanged', handleAccountsChanged);
          window.ethereum.removeListener('chainChanged', handleChainChanged);
        }
      };
    } else {
      dispatch({
        type: Web3ActionTypes.PROVIDER_UNAVAILABLE,
        payload: { error: 'No Ethereum provider found. Please install MetaMask or another Web3 wallet.' },
      });
    }
  }, [state.accountAddress, state.config, state.supportedNetworks]);

  /**
   * Connect to Web3 provider
   */
  const connect = useCallback(async (): Promise<void> => {
    if (!window.ethereum) {
      dispatch({
        type: Web3ActionTypes.CONNECT_FAILURE,
        payload: { error: 'No Ethereum provider found. Please install MetaMask or another Web3 wallet.' },
      });
      return;
    }

    try {
      dispatch({ type: Web3ActionTypes.CONNECT_START });

      // Request accounts
      const accountsResponse = await window.ethereum.request({ method: 'eth_requestAccounts' });
      const accounts = accountsResponse as string[];

      if (!accounts || accounts.length === 0) {
        throw new Error('No accounts found. Please unlock your wallet.');
      }

      // Get provider
      const provider = new BrowserProvider(window.ethereum);

      // Get network information
      let networkInfo: NetworkInfo | undefined;
      try {
        const network = await provider.getNetwork();
        const chainId = formatChainId(network.chainId);
        networkInfo = getNetworkInfo(chainId, state.supportedNetworks);
      } catch (error) {
        console.warn('Failed to get network information:', error);
      }

      dispatch({
        type: Web3ActionTypes.CONNECT_SUCCESS,
        payload: {
          provider,
          accountAddress: accounts[0]!,
          network: networkInfo,
        },
      });
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to connect to Web3 provider';
      dispatch({
        type: Web3ActionTypes.CONNECT_FAILURE,
        payload: { error: errorMessage },
      });
    }
  }, [state.supportedNetworks]);

  /**
   * Disconnect from Web3 provider
   */
  const disconnect = useCallback((): void => {
    dispatch({ type: Web3ActionTypes.DISCONNECT });
    clearPersistedWeb3State();
  }, []);

  /**
   * Reconnect to Web3 provider with retry logic
   */
  const reconnect = useCallback(async (): Promise<void> => {
    const maxAttempts = state.config.reconnectAttempts || 3;
    const delay = state.config.reconnectDelay || 1000;

    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        await connect();
        return; // Success, exit retry loop
      } catch (error) {
        if (attempt === maxAttempts) {
          throw error; // Last attempt failed, propagate error
        }
        // Wait before next attempt
        await new Promise((resolve) => setTimeout(resolve, delay * attempt));
      }
    }
  }, [connect, state.config.reconnectAttempts, state.config.reconnectDelay]);

  /**
   * Switch to a different network
   */
  const switchNetwork = useCallback(
    async (chainId: string): Promise<void> => {
      if (!window.ethereum || !state.isConnected) {
        throw new Error('Not connected to Web3 provider');
      }

      const formattedChainId = formatChainId(chainId);
      const networkInfo = getNetworkInfo(formattedChainId, state.supportedNetworks);

      if (!networkInfo) {
        throw new Error(`Unsupported network: ${chainId}`);
      }

      try {
        await window.ethereum.request({
          method: 'wallet_switchEthereumChain',
          params: [{ chainId: formattedChainId }],
        });

        dispatch({
          type: Web3ActionTypes.SET_NETWORK,
          payload: { network: networkInfo },
        });
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Failed to switch network';
        throw new Error(errorMessage);
      }
    },
    [state.isConnected, state.supportedNetworks],
  );

  /**
   * Add a new network to the wallet
   */
  const addNetwork = useCallback(async (network: NetworkInfo): Promise<void> => {
    if (!window.ethereum) {
      throw new Error('No Ethereum provider found');
    }

    try {
      await window.ethereum.request({
        method: 'wallet_addEthereumChain',
        params: [
          {
            chainId: network.chainId,
            chainName: network.name,
            rpcUrls: network.rpcUrl ? [network.rpcUrl] : [],
            blockExplorerUrls: network.blockExplorerUrl ? [network.blockExplorerUrl] : [],
          },
        ],
      });
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to add network';
      throw new Error(errorMessage);
    }
  }, []);

  /**
   * Update Web3 configuration
   */
  const updateConfig = useCallback((newConfig: Partial<Web3Config>): void => {
    dispatch({
      type: Web3ActionTypes.UPDATE_CONFIG,
      payload: newConfig,
    });
  }, []);

  /**
   * Check if a network is supported
   */
  const isNetworkSupportedCallback = useCallback(
    (chainId: string): boolean => {
      return isNetworkSupported(chainId, state.supportedNetworks);
    },
    [state.supportedNetworks],
  );

  /**
   * Get network information by chain ID
   */
  const getNetworkInfoCallback = useCallback(
    (chainId: string): NetworkInfo | undefined => {
      return getNetworkInfo(chainId, state.supportedNetworks);
    },
    [state.supportedNetworks],
  );

  /**
   * Clear current error
   */
  const clearError = useCallback((): void => {
    // This will clear error by updating config which triggers a reducer action
    updateConfig({});
  }, [updateConfig]);

  /**
   * Reset Web3 state to initial values
   */
  const resetState = useCallback((): void => {
    dispatch({ type: Web3ActionTypes.RESET_STATE });
    clearPersistedWeb3State();
  }, []);

  const contextValue: Web3ContextValue = {
    state,
    dispatch,
    connect,
    disconnect,
    reconnect,
    switchNetwork,
    addNetwork,
    updateConfig,
    isNetworkSupported: isNetworkSupportedCallback,
    getNetworkInfo: getNetworkInfoCallback,
    clearError,
    resetState,
  };

  return <Web3Context.Provider value={contextValue}>{children}</Web3Context.Provider>;
}

/**
 * Hook to access Web3 context
 */
export function useWeb3Context(): Web3ContextValue {
  const context = useContext(Web3Context);
  if (context === defaultContextValue) {
    throw new Error('useWeb3Context must be used within a Web3Provider');
  }
  return context;
}

/**
 * Hook for dispatching Web3 actions
 */
export function useWeb3Actions() {
  const { connect, disconnect, reconnect, switchNetwork, addNetwork, clearError, resetState } = useWeb3Context();
  return {
    connect,
    disconnect,
    reconnect,
    switchNetwork,
    addNetwork,
    clearError,
    resetState,
  };
}

/**
 * Hook for accessing Web3 state
 */
export function useWeb3State() {
  const { state } = useWeb3Context();
  return state;
}

/**
 * Hook for Web3 connection status
 */
export function useWeb3Connection() {
  const { state, connect, disconnect } = useWeb3Context();
  return {
    isConnected: state.isConnected,
    isConnecting: state.isConnecting,
    accountAddress: state.accountAddress,
    connect,
    disconnect,
  };
}

/**
 * Hook for Web3 network operations
 */
export function useWeb3Network() {
  const { state, switchNetwork, addNetwork, isNetworkSupported, getNetworkInfo } = useWeb3Context();
  return {
    network: state.network,
    supportedNetworks: state.supportedNetworks,
    switchNetwork,
    addNetwork,
    isNetworkSupported,
    getNetworkInfo,
  };
}

// Export the context for advanced usage
export { Web3Context };

// Re-export types
export * from '../types';
