# Web3 Provider with Reducer and Context

This document explains the refactored Web3Provider implementation using React's reducer pattern and context API, following the established patterns from other providers in the codebase.

## Overview

The Web3 system now consists of:

1. **Types** (`types.ts`) - All TypeScript interfaces and types
2. **Reducer** (`web3-reducer.ts`) - State management logic with helper functions
3. **Provider** (`web3-context.tsx`) - Context provider with reducer for state management
4. **Hooks** - Custom hooks for accessing and manipulating Web3 state

## Key Features

### State Management

- **Centralized state** using `useReducer` for all Web3 data
- **Persistent configuration** saved to localStorage
- **Network tracking** and management
- **Connection status tracking**
- **Error tracking** and management

### Network Support

- **Multi-network support** with configurable supported networks
- **Automatic network detection** when connecting
- **Network switching** capabilities
- **Custom network addition** support

### Configuration Management

- **Flexible configuration** with sensible defaults
- **Auto-connect** capability
- **Connection persistence** across sessions
- **Event tracking** for account and network changes

### TypeScript Support

- **Fully typed** interfaces and actions
- **Strict type checking** for all state updates
- **IntelliSense support** for better developer experience

## Usage Examples

### Basic Web3 Usage

```tsx
import { Web3Provider } from '@/components/web3/web3-context';

function MyApp() {
  return (
    <Web3Provider
      config={{
        autoConnect: true,
        persistConnection: true,
        supportedChainIds: ['0x1', '0x89'], // Ethereum, Polygon
      }}
    >
      <MyComponent />
    </Web3Provider>
  );
}
```

### Advanced Usage with Custom Networks

```tsx
import { Web3Provider } from '@/components/web3/web3-context';

const customNetworks = [
  {
    chainId: '0x1',
    name: 'Ethereum Mainnet',
    rpcUrl: 'https://eth-mainnet.g.alchemy.com/v2/your-key',
    blockExplorerUrl: 'https://etherscan.io',
  },
  {
    chainId: '0x89',
    name: 'Polygon',
    rpcUrl: 'https://polygon-rpc.com',
    blockExplorerUrl: 'https://polygonscan.com',
  },
];

function MyApp() {
  return (
    <Web3Provider
      config={{
        autoConnect: false,
        persistConnection: true,
        enableNetworkChangeTracking: true,
        reconnectAttempts: 5,
      }}
      supportedNetworks={customNetworks}
    >
      <MyComponent />
    </Web3Provider>
  );
}
```

### Using Web3 Hooks

```tsx
import { useWeb3Connection, useWeb3Network, useWeb3State } from '@/components/web3/web3-context';

function ConnectButton() {
  const { isConnected, isConnecting, connect, disconnect, accountAddress } = useWeb3Connection();

  if (isConnected) {
    return (
      <div>
        <p>Connected: {accountAddress}</p>
        <button onClick={disconnect}>Disconnect</button>
      </div>
    );
  }

  return (
    <button onClick={connect} disabled={isConnecting}>
      {isConnecting ? 'Connecting...' : 'Connect Wallet'}
    </button>
  );
}

function NetworkSwitcher() {
  const { network, supportedNetworks, switchNetwork } = useWeb3Network();

  return (
    <div>
      <p>Current Network: {network?.name}</p>
      <select
        value={network?.chainId || ''}
        onChange={(e) => switchNetwork(e.target.value)}
      >
        {supportedNetworks.map((net) => (
          <option key={net.chainId} value={net.chainId}>
            {net.name}
          </option>
        ))}
      </select>
    </div>
  );
}
```

## Available Hooks

### Core Hooks

- `useWeb3Context()` - Access full Web3 context
- `useWeb3State()` - Access Web3 state only
- `useWeb3Actions()` - Access Web3 actions only

### Specialized Hooks

- `useWeb3Connection()` - Connection-specific state and actions
- `useWeb3Network()` - Network-specific state and actions

## Configuration Options

```typescript
interface Web3Config {
  autoConnect?: boolean; // Auto-connect on mount if previously connected
  persistConnection?: boolean; // Persist connection state in localStorage
  supportedChainIds?: string[]; // List of supported chain IDs
  defaultChainId?: string; // Default chain ID to connect to
  enableAccountChangeTracking?: boolean; // Track account changes
  enableNetworkChangeTracking?: boolean; // Track network changes
  reconnectAttempts?: number; // Number of reconnection attempts
  reconnectDelay?: number; // Delay between reconnection attempts (ms)
}
```

## State Structure

```typescript
interface Web3State {
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
```

## Migration from Old Web3Provider

### Before (Old Implementation)

```tsx
// Old usage
import { Web3Context } from '@/components/web3/web3-context';

function MyComponent() {
  const { state, connect, disconnect } = useContext(Web3Context);
  
  return (
    <button onClick={connect}>
      {state.isConnecting ? 'Connecting...' : 'Connect'}
    </button>
  );
}
```

### After (New Implementation)

```tsx
// New usage
import { useWeb3Connection } from '@/components/web3/web3-context';

function MyComponent() {
  const { isConnecting, connect } = useWeb3Connection();
  
  return (
    <button onClick={connect}>
      {isConnecting ? 'Connecting...' : 'Connect'}
    </button>
  );
}
```

## Error Handling

The new implementation provides better error handling:

```tsx
import { useWeb3State, useWeb3Actions } from '@/components/web3/web3-context';

function ErrorDisplay() {
  const { error, connectionStatus } = useWeb3State();
  const { clearError, resetState } = useWeb3Actions();

  if (error) {
    return (
      <div className="error">
        <p>Error: {error}</p>
        <button onClick={clearError}>Clear Error</button>
        <button onClick={resetState}>Reset</button>
      </div>
    );
  }

  return null;
}
```

## Best Practices

1. **Use specialized hooks** instead of the full context when possible
2. **Configure supported networks** based on your application needs
3. **Handle connection persistence** appropriately for your use case
4. **Implement proper error handling** with user-friendly messages
5. **Use TypeScript** for better type safety and development experience

## Performance Optimizations

- **Memoized callbacks** prevent unnecessary re-renders
- **Selective state updates** minimize component updates
- **Lazy network detection** only when needed
- **Optimized event listeners** with proper cleanup

## Security Considerations

- **Input validation** for all user-provided data
- **Safe localStorage usage** with error handling
- **Network validation** before switching
- **Provider availability checks** before operations
