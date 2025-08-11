'use client';

import React, { createContext, useCallback, useContext, useReducer } from 'react';
import { LexicalEditor } from 'lexical';
import {
  defaultEditorConfig,
  defaultEditorState,
  EditorAction,
  EditorActionTypes,
  EditorConfig,
  EditorContent,
  EditorContextValue,
  EditorError,
  EditorErrorType,
  EditorProviderProps,
  EditorReducer,
  EditorState,
  EnabledPlugins,
  ToolbarState,
} from './types';

/**
 * Editor reducer function to handle state updates
 */
const editorReducer: EditorReducer = (state: EditorState, action: EditorAction): EditorState => {
  switch (action.type) {
    case EditorActionTypes.INITIALIZE_EDITOR: {
      const { editor, config } = action.payload;
      return {
        ...state,
        editor,
        config: { ...state.config, ...config },
        isInitialized: true,
      };
    }

    case EditorActionTypes.UPDATE_CONFIG: {
      return {
        ...state,
        config: { ...state.config, ...action.payload },
      };
    }

    case EditorActionTypes.SET_CONTENT: {
      return {
        ...state,
        content: action.payload,
        hasUnsavedChanges: false,
      };
    }

    case EditorActionTypes.UPDATE_CONTENT: {
      return {
        ...state,
        content: { ...state.content, ...action.payload },
        hasUnsavedChanges: true,
      };
    }

    case EditorActionTypes.SET_READ_ONLY: {
      return {
        ...state,
        isReadOnly: action.payload,
      };
    }

    case EditorActionTypes.SET_LOADING: {
      return {
        ...state,
        isLoading: action.payload,
      };
    }

    case EditorActionTypes.SET_SAVING: {
      return {
        ...state,
        isSaving: action.payload,
      };
    }

    case EditorActionTypes.MARK_UNSAVED_CHANGES: {
      return {
        ...state,
        hasUnsavedChanges: action.payload ?? true,
      };
    }

    case EditorActionTypes.MARK_SAVED: {
      return {
        ...state,
        hasUnsavedChanges: false,
        isSaving: false,
        lastSaved: new Date(),
      };
    }

    case EditorActionTypes.ADD_ERROR: {
      const errorId = `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
      const newError: EditorError = {
        id: errorId,
        timestamp: new Date(),
        ...action.payload,
      };

      return {
        ...state,
        errors: [...state.errors, newError],
      };
    }

    case EditorActionTypes.CLEAR_ERROR: {
      return {
        ...state,
        errors: state.errors.filter((error) => error.id !== action.payload.errorId),
      };
    }

    case EditorActionTypes.CLEAR_ALL_ERRORS: {
      return {
        ...state,
        errors: [],
      };
    }

    case EditorActionTypes.TOGGLE_PLUGIN: {
      const { plugin, enabled } = action.payload;
      return {
        ...state,
        plugins: {
          ...state.plugins,
          [plugin]: enabled,
        },
      };
    }

    case EditorActionTypes.UPDATE_TOOLBAR: {
      return {
        ...state,
        toolbar: { ...state.toolbar, ...action.payload },
      };
    }

    case EditorActionTypes.RESET_EDITOR: {
      return {
        ...defaultEditorState,
        config: state.config, // Preserve config on reset
      };
    }

    default: {
      console.error(`Unhandled action type: ${(action as any).type}`);
      return state;
    }
  }
};

/**
 * Default context to handle errors when context is used outside the provider
 */
const defaultContextValue: EditorContextValue = {
  state: defaultEditorState,
  dispatch: () => console.error('EditorContext used outside of provider'),
  initializeEditor: () => console.error('EditorContext used outside of provider'),
  updateConfig: () => console.error('EditorContext used outside of provider'),
  setContent: () => console.error('EditorContext used outside of provider'),
  updateContent: () => console.error('EditorContext used outside of provider'),
  setReadOnly: () => console.error('EditorContext used outside of provider'),
  setLoading: () => console.error('EditorContext used outside of provider'),
  setSaving: () => console.error('EditorContext used outside of provider'),
  markUnsavedChanges: () => console.error('EditorContext used outside of provider'),
  markSaved: () => console.error('EditorContext used outside of provider'),
  addError: () => console.error('EditorContext used outside of provider'),
  clearError: () => console.error('EditorContext used outside of provider'),
  clearAllErrors: () => console.error('EditorContext used outside of provider'),
  togglePlugin: () => console.error('EditorContext used outside of provider'),
  updateToolbar: () => console.error('EditorContext used outside of provider'),
  resetEditor: () => console.error('EditorContext used outside of provider'),
  getContent: () => {
    console.error('EditorContext used outside of provider');
    return undefined;
  },
  hasErrors: () => {
    console.error('EditorContext used outside of provider');
    return false;
  },
  getErrorsByType: () => {
    console.error('EditorContext used outside of provider');
    return [];
  },
  isPluginEnabled: () => {
    console.error('EditorContext used outside of provider');
    return false;
  },
  canSave: () => {
    console.error('EditorContext used outside of provider');
    return false;
  },
  isNested: false,
};

const EditorContext = createContext<EditorContextValue>(defaultContextValue);

/**
 * Initialize editor state with potential localStorage values
 */
const createInitialEditorState = (initialState: EditorState): EditorState => {
  let enhancedState = { ...initialState };

  // Check for persisted configuration in localStorage (client-side only)
  if (typeof window !== 'undefined') {
    try {
      const persistedConfig = localStorage.getItem('editorConfig');
      if (persistedConfig) {
        const parsedConfig = JSON.parse(persistedConfig);
        enhancedState = {
          ...enhancedState,
          config: {
            ...enhancedState.config,
            ...parsedConfig,
          },
        };
      }

      // Check for persisted plugin states
      const persistedPlugins = localStorage.getItem('editorPlugins');
      if (persistedPlugins) {
        const parsedPlugins = JSON.parse(persistedPlugins);
        enhancedState.plugins = {
          ...enhancedState.plugins,
          ...parsedPlugins,
        };
      }
    } catch (error) {
      console.warn('Failed to load editor configuration from localStorage:', error);
    }
  }

  return enhancedState;
};

/**
 * Provider that manages editor state using a reducer
 */
export function EditorProvider({ children, config = {}, initialState = defaultEditorState, forceNested = false }: EditorProviderProps) {
  // Auto-detect if this is a nested provider
  const existingContext = useContext(EditorContext);
  const hasParentProvider = existingContext && existingContext !== defaultContextValue;
  const isNested = forceNested || hasParentProvider;

  // Get parent context if this is a nested provider
  const parentContext = isNested ? existingContext : undefined;

  // Merge configurations: parent config < initial state < provided config
  const mergedConfig: EditorConfig =
    isNested && parentContext
      ? {
          ...defaultEditorConfig,
          ...parentContext.state.config,
          ...initialState.config,
          ...config,
        }
      : {
          ...defaultEditorConfig,
          ...initialState.config,
          ...config,
        };

  const stateWithConfig: EditorState = {
    ...defaultEditorState,
    ...initialState,
    config: mergedConfig,
  };

  const [state, dispatch] = useReducer(
    editorReducer,
    stateWithConfig,
    // Only apply localStorage initialization for root provider
    isNested ? (state) => state : createInitialEditorState,
  );

  // Action creators
  const initializeEditor = useCallback((editor: LexicalEditor, config: Partial<EditorConfig> = {}) => {
    dispatch({
      type: EditorActionTypes.INITIALIZE_EDITOR,
      payload: { editor, config: { ...defaultEditorConfig, ...config } },
    });
  }, []);

  const updateConfig = useCallback(
    (newConfig: Partial<EditorConfig>) => {
      dispatch({ type: EditorActionTypes.UPDATE_CONFIG, payload: newConfig });

      // Persist configuration to localStorage (client-side only)
      if (typeof window !== 'undefined' && !isNested) {
        try {
          const mergedConfig = { ...state.config, ...newConfig };
          localStorage.setItem('editorConfig', JSON.stringify(mergedConfig));
        } catch (error) {
          console.warn('Failed to persist editor configuration to localStorage:', error);
        }
      }
    },
    [state.config, isNested],
  );

  const setContent = useCallback((content: EditorContent) => {
    dispatch({ type: EditorActionTypes.SET_CONTENT, payload: content });
  }, []);

  const updateContent = useCallback((content: Partial<EditorContent>) => {
    dispatch({ type: EditorActionTypes.UPDATE_CONTENT, payload: content });
  }, []);

  const setReadOnly = useCallback((readOnly: boolean) => {
    dispatch({ type: EditorActionTypes.SET_READ_ONLY, payload: readOnly });
  }, []);

  const setLoading = useCallback((loading: boolean) => {
    dispatch({ type: EditorActionTypes.SET_LOADING, payload: loading });
  }, []);

  const setSaving = useCallback((saving: boolean) => {
    dispatch({ type: EditorActionTypes.SET_SAVING, payload: saving });
  }, []);

  const markUnsavedChanges = useCallback((hasChanges?: boolean) => {
    dispatch({ type: EditorActionTypes.MARK_UNSAVED_CHANGES, payload: hasChanges });
  }, []);

  const markSaved = useCallback(() => {
    dispatch({ type: EditorActionTypes.MARK_SAVED });
  }, []);

  const addError = useCallback((error: Omit<EditorError, 'id' | 'timestamp'>) => {
    dispatch({ type: EditorActionTypes.ADD_ERROR, payload: error });
  }, []);

  const clearError = useCallback((errorId: string) => {
    dispatch({ type: EditorActionTypes.CLEAR_ERROR, payload: { errorId } });
  }, []);

  const clearAllErrors = useCallback(() => {
    dispatch({ type: EditorActionTypes.CLEAR_ALL_ERRORS });
  }, []);

  const togglePlugin = useCallback(
    (plugin: keyof EnabledPlugins, enabled: boolean) => {
      dispatch({ type: EditorActionTypes.TOGGLE_PLUGIN, payload: { plugin, enabled } });

      // Persist plugin states to localStorage (client-side only)
      if (typeof window !== 'undefined' && !isNested) {
        try {
          const updatedPlugins = { ...state.plugins, [plugin]: enabled };
          localStorage.setItem('editorPlugins', JSON.stringify(updatedPlugins));
        } catch (error) {
          console.warn('Failed to persist editor plugin states to localStorage:', error);
        }
      }
    },
    [state.plugins, isNested],
  );

  const updateToolbar = useCallback((toolbar: Partial<ToolbarState>) => {
    dispatch({ type: EditorActionTypes.UPDATE_TOOLBAR, payload: toolbar });
  }, []);

  const resetEditor = useCallback(() => {
    dispatch({ type: EditorActionTypes.RESET_EDITOR });
  }, []);

  // Utility functions
  const getContent = useCallback(
    (format: 'html' | 'markdown' | 'json' | 'plainText') => {
      return state.content[format];
    },
    [state.content],
  );

  const hasErrors = useCallback(() => {
    return state.errors.length > 0;
  }, [state.errors]);

  const getErrorsByType = useCallback(
    (type: EditorErrorType) => {
      return state.errors.filter((error) => error.type === type);
    },
    [state.errors],
  );

  const isPluginEnabled = useCallback(
    (plugin: keyof EnabledPlugins) => {
      return state.plugins[plugin];
    },
    [state.plugins],
  );

  const canSave = useCallback(() => {
    return state.hasUnsavedChanges && !state.isSaving && !state.isLoading && state.isInitialized;
  }, [state.hasUnsavedChanges, state.isSaving, state.isLoading, state.isInitialized]);

  const contextValue: EditorContextValue = {
    state,
    dispatch,
    initializeEditor,
    updateConfig,
    setContent,
    updateContent,
    setReadOnly,
    setLoading,
    setSaving,
    markUnsavedChanges,
    markSaved,
    addError,
    clearError,
    clearAllErrors,
    togglePlugin,
    updateToolbar,
    resetEditor,
    getContent,
    hasErrors,
    getErrorsByType,
    isPluginEnabled,
    canSave,
    isNested,
    parentContext,
  };

  return <EditorContext.Provider value={contextValue}>{children}</EditorContext.Provider>;
}

/**
 * Hook to access editor context
 */
export function useEditorContext() {
  const context = useContext(EditorContext);
  if (!context) {
    throw new Error('useEditorContext must be used within EditorProvider');
  }
  return context;
}

/**
 * Hook for dispatching editor actions
 */
export function useEditorActions() {
  const { dispatch, initializeEditor, updateConfig, setContent, updateContent, setReadOnly, setLoading, setSaving, markUnsavedChanges, markSaved, addError, clearError, clearAllErrors, togglePlugin, updateToolbar, resetEditor } =
    useEditorContext();

  return {
    dispatch,
    initializeEditor,
    updateConfig,
    setContent,
    updateContent,
    setReadOnly,
    setLoading,
    setSaving,
    markUnsavedChanges,
    markSaved,
    addError,
    clearError,
    clearAllErrors,
    togglePlugin,
    updateToolbar,
    resetEditor,
  };
}

/**
 * Hook for accessing editor state
 */
export function useEditorState() {
  const { state, getContent, hasErrors, getErrorsByType, isPluginEnabled, canSave } = useEditorContext();
  return { state, getContent, hasErrors, getErrorsByType, isPluginEnabled, canSave };
}

/**
 * Hook for editor content management
 */
export function useEditorContent() {
  const { state, setContent, updateContent, getContent, markUnsavedChanges, markSaved } = useEditorContext();
  return {
    content: state.content,
    hasUnsavedChanges: state.hasUnsavedChanges,
    lastSaved: state.lastSaved,
    setContent,
    updateContent,
    getContent,
    markUnsavedChanges,
    markSaved,
  };
}

/**
 * Hook for editor configuration
 */
export function useEditorConfig() {
  const { state, updateConfig, togglePlugin } = useEditorContext();
  return {
    config: state.config,
    plugins: state.plugins,
    updateConfig,
    togglePlugin,
  };
}

/**
 * Hook for editor status
 */
export function useEditorStatus() {
  const { state, hasErrors, canSave } = useEditorContext();
  return {
    isInitialized: state.isInitialized,
    isReadOnly: state.isReadOnly,
    isLoading: state.isLoading,
    isSaving: state.isSaving,
    hasErrors: hasErrors(),
    canSave: canSave(),
    errors: state.errors,
  };
}
