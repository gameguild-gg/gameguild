import React, { createContext, useContext, useEffect, useReducer } from 'react';
import { useContextIntegration, useEnhancedStorage, useStorageObserver } from '../hooks';

// Example: Game State Context with Storage Integration

interface GameState {
  playerId: string | null;
  level: number;
  score: number;
  inventory: string[];
  preferences: {
    soundEnabled: boolean;
    difficulty: 'easy' | 'medium' | 'hard';
  };
}

type GameAction =
  | { type: 'SET_PLAYER'; playerId: string }
  | { type: 'LEVEL_UP' }
  | { type: 'ADD_SCORE'; points: number }
  | { type: 'ADD_ITEM'; item: string }
  | { type: 'REMOVE_ITEM'; item: string }
  | { type: 'UPDATE_PREFERENCES'; preferences: Partial<GameState['preferences']> }
  | { type: 'RESET_GAME' }
  | { type: 'LOAD_STATE'; state: GameState };

const initialGameState: GameState = {
  playerId: null,
  level: 1,
  score: 0,
  inventory: [],
  preferences: {
    soundEnabled: true,
    difficulty: 'medium',
  },
};

function gameReducer(state: GameState, action: GameAction): GameState {
  switch (action.type) {
    case 'SET_PLAYER':
      return { ...state, playerId: action.playerId };

    case 'LEVEL_UP':
      return { ...state, level: state.level + 1 };

    case 'ADD_SCORE':
      return { ...state, score: state.score + action.points };

    case 'ADD_ITEM':
      return { ...state, inventory: [...state.inventory, action.item] };

    case 'REMOVE_ITEM':
      return { ...state, inventory: state.inventory.filter((item) => item !== action.item) };

    case 'UPDATE_PREFERENCES':
      return { ...state, preferences: { ...state.preferences, ...action.preferences } };

    case 'RESET_GAME':
      return initialGameState;

    case 'LOAD_STATE':
      return action.state;

    default:
      return state;
  }
}

interface GameContextType {
  state: GameState;
  dispatch: React.Dispatch<GameAction>;

  // Storage-backed actions
  saveGame: () => Promise<void>;
  loadGame: () => Promise<void>;
  resetGame: () => Promise<void>;

  // Auto-save status
  isAutoSaving: boolean;
  lastSaveTime: Date | null;
  saveError: Error | null;
}

const GameContext = createContext<GameContextType | undefined>(undefined);

interface GameProviderProps {
  children: React.ReactNode;
  autoSave?: boolean;
  autoSaveInterval?: number;
}

export const GameProvider: React.FC<GameProviderProps> = ({
  children,
  autoSave = true,
  autoSaveInterval = 10000, // 10 seconds
}) => {
  const [state, dispatch] = useReducer(gameReducer, initialGameState);

  // Use context integration for namespace management
  const contextIntegration = useContextIntegration<GameState>({
    namespace: 'game',
    syncKeys: ['gameState', 'preferences'],
    autoSync: autoSave,
    syncInterval: autoSaveInterval,
  });

  // Use enhanced storage for specific game state
  const gameStateStorage = useEnhancedStorage<GameState>({
    key: 'game:state',
    defaultValue: initialGameState,
    ttl: 7 * 24 * 60 * 60 * 1000, // 7 days
    debounceMs: 1000,
  });

  // Use storage observer to monitor external changes
  const storageObserver = useStorageObserver({
    keys: ['game:state', 'game:preferences'],
    includeValues: true,
    debounceMs: 500,
  });

  // Auto-save mechanism
  useEffect(() => {
    if (!autoSave) return;

    const interval = setInterval(async () => {
      try {
        await contextIntegration.syncStateToStorage('gameState', state);
      } catch (error) {
        console.error('Auto-save failed:', error);
      }
    }, autoSaveInterval);

    return () => clearInterval(interval);
  }, [autoSave, autoSaveInterval, state, contextIntegration]);

  // Load game on mount
  useEffect(() => {
    const loadGameOnMount = async () => {
      const savedState = await contextIntegration.loadStateFromStorage('gameState', initialGameState);
      if (savedState) {
        dispatch({ type: 'LOAD_STATE', state: savedState });
      }
    };

    loadGameOnMount();
  }, [contextIntegration]);

  // Watch for external storage changes
  useEffect(() => {
    const unsubscribe = contextIntegration.watchStateChanges('gameState', (newState) => {
      dispatch({ type: 'LOAD_STATE', state: newState });
    });

    return unsubscribe;
  }, [contextIntegration]);

  // Storage actions
  const saveGame = async () => {
    await contextIntegration.syncStateToStorage('gameState', state);
    await gameStateStorage.setData(state);
  };

  const loadGame = async () => {
    const savedState = await contextIntegration.loadStateFromStorage('gameState', initialGameState);
    if (savedState) {
      dispatch({ type: 'LOAD_STATE', state: savedState });
    }
  };

  const resetGame = async () => {
    dispatch({ type: 'RESET_GAME' });
    await contextIntegration.clearNamespaceData();
    await gameStateStorage.deleteData();
  };

  const value: GameContextType = {
    state,
    dispatch,
    saveGame,
    loadGame,
    resetGame,
    isAutoSaving: contextIntegration.isReady,
    lastSaveTime: contextIntegration.lastSyncTime,
    saveError: contextIntegration.syncErrors[0] || gameStateStorage.error,
  };

  return <GameContext.Provider value={value}>{children}</GameContext.Provider>;
};

export const useGame = (): GameContextType => {
  const context = useContext(GameContext);
  if (context === undefined) {
    throw new Error('useGame must be used within a GameProvider');
  }
  return context;
};

// Example usage component
export const GameComponent: React.FC = () => {
  const { state, dispatch, saveGame, loadGame, resetGame, isAutoSaving, lastSaveTime } = useGame();

  return (
    <div>
      <h2>Game State</h2>
      <p>Player ID: {state.playerId || 'Not set'}</p>
      <p>Level: {state.level}</p>
      <p>Score: {state.score}</p>
      <p>Inventory: {state.inventory.join(', ')}</p>

      <div>
        <button onClick={() => dispatch({ type: 'SET_PLAYER', playerId: 'player123' })}>Set Player</button>
        <button onClick={() => dispatch({ type: 'LEVEL_UP' })}>Level Up</button>
        <button onClick={() => dispatch({ type: 'ADD_SCORE', points: 100 })}>Add Score</button>
        <button onClick={() => dispatch({ type: 'ADD_ITEM', item: 'sword' })}>Add Item</button>
      </div>

      <div style={{ marginTop: '20px' }}>
        <button onClick={saveGame}>Save Game</button>
        <button onClick={loadGame}>Load Game</button>
        <button onClick={resetGame}>Reset Game</button>
      </div>

      <div style={{ marginTop: '20px', fontSize: '0.8em', color: '#666' }}>
        {isAutoSaving && <p>Auto-saving enabled</p>}
        {lastSaveTime && <p>Last saved: {lastSaveTime.toLocaleTimeString()}</p>}
      </div>
    </div>
  );
};
