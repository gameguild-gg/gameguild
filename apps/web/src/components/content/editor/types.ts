import { LexicalEditor } from 'lexical';
import { ReactNode } from 'react';

/**
 * Editor theme configuration
 */
export interface EditorTheme {
  paragraph?: string;
  heading?: {
    h1?: string;
    h2?: string;
    h3?: string;
    h4?: string;
    h5?: string;
    h6?: string;
  };
  quote?: string;
  list?: {
    nested?: {
      listitem?: string;
    };
    ol?: string;
    ul?: string;
    listitem?: string;
  };
  link?: string;
  text?: {
    bold?: string;
    italic?: string;
    underline?: string;
    strikethrough?: string;
    code?: string;
  };
  code?: string;
  codeHighlight?: {
    [key: string]: string;
  };
}

/**
 * Editor configuration options
 */
export interface EditorConfig {
  namespace: string;
  theme?: EditorTheme;
  placeholder?: string;
  readOnly?: boolean;
  autoFocus?: boolean;
  enableMarkdown?: boolean;
  enableImageUpload?: boolean;
  enableVideoEmbed?: boolean;
  enableQuiz?: boolean;
  enableHTML?: boolean;
  enableAudio?: boolean;
  enableDivider?: boolean;
  enableCallout?: boolean;
  enableButton?: boolean;
  enableHeader?: boolean;
  enableCode?: boolean;
  maxContentLength?: number;
  allowedFileTypes?: string[];
  imageUploadUrl?: string;
  onError?: (error: Error) => void;
  onContentChange?: (content: string) => void;
  onContentSave?: (content: string) => void;
}

/**
 * Editor content state
 */
export interface EditorContent {
  html?: string;
  markdown?: string;
  json?: string;
  plainText?: string;
  wordCount?: number;
  characterCount?: number;
}

/**
 * Editor state management
 */
export interface EditorState {
  editor: LexicalEditor | null;
  config: EditorConfig;
  content: EditorContent;
  isInitialized: boolean;
  isReadOnly: boolean;
  isLoading: boolean;
  isSaving: boolean;
  hasUnsavedChanges: boolean;
  lastSaved?: Date;
  errors: EditorError[];
  plugins: EnabledPlugins;
  toolbar: ToolbarState;
}

/**
 * Editor error tracking
 */
export interface EditorError {
  id: string;
  message: string;
  type: EditorErrorType;
  timestamp: Date;
  context?: Record<string, unknown>;
}

export enum EditorErrorType {
  INITIALIZATION = 'initialization',
  CONTENT_LOAD = 'content_load',
  CONTENT_SAVE = 'content_save',
  PLUGIN = 'plugin',
  VALIDATION = 'validation',
  NETWORK = 'network',
}

/**
 * Plugin state management
 */
export interface EnabledPlugins {
  richText: boolean;
  history: boolean;
  list: boolean;
  markdown: boolean;
  image: boolean;
  video: boolean;
  quiz: boolean;
  html: boolean;
  audio: boolean;
  divider: boolean;
  callout: boolean;
  button: boolean;
  header: boolean;
  code: boolean;
  floatingToolbar: boolean;
  contentInsert: boolean;
}

/**
 * Toolbar state
 */
export interface ToolbarState {
  isVisible: boolean;
  position: { x: number; y: number } | null;
  activeFormats: Set<string>;
  selectedText: string;
}

/**
 * Editor action types
 */
export enum EditorActionTypes {
  INITIALIZE_EDITOR = 'INITIALIZE_EDITOR',
  UPDATE_CONFIG = 'UPDATE_CONFIG',
  SET_CONTENT = 'SET_CONTENT',
  UPDATE_CONTENT = 'UPDATE_CONTENT',
  SET_READ_ONLY = 'SET_READ_ONLY',
  SET_LOADING = 'SET_LOADING',
  SET_SAVING = 'SET_SAVING',
  MARK_UNSAVED_CHANGES = 'MARK_UNSAVED_CHANGES',
  MARK_SAVED = 'MARK_SAVED',
  ADD_ERROR = 'ADD_ERROR',
  CLEAR_ERROR = 'CLEAR_ERROR',
  CLEAR_ALL_ERRORS = 'CLEAR_ALL_ERRORS',
  TOGGLE_PLUGIN = 'TOGGLE_PLUGIN',
  UPDATE_TOOLBAR = 'UPDATE_TOOLBAR',
  RESET_EDITOR = 'RESET_EDITOR',
}

/**
 * Editor actions
 */
export type EditorAction =
  | { type: EditorActionTypes.INITIALIZE_EDITOR; payload: { editor: LexicalEditor; config: EditorConfig } }
  | { type: EditorActionTypes.UPDATE_CONFIG; payload: Partial<EditorConfig> }
  | { type: EditorActionTypes.SET_CONTENT; payload: EditorContent }
  | { type: EditorActionTypes.UPDATE_CONTENT; payload: Partial<EditorContent> }
  | { type: EditorActionTypes.SET_READ_ONLY; payload: boolean }
  | { type: EditorActionTypes.SET_LOADING; payload: boolean }
  | { type: EditorActionTypes.SET_SAVING; payload: boolean }
  | { type: EditorActionTypes.MARK_UNSAVED_CHANGES; payload?: boolean }
  | { type: EditorActionTypes.MARK_SAVED }
  | { type: EditorActionTypes.ADD_ERROR; payload: Omit<EditorError, 'id' | 'timestamp'> }
  | { type: EditorActionTypes.CLEAR_ERROR; payload: { errorId: string } }
  | { type: EditorActionTypes.CLEAR_ALL_ERRORS }
  | { type: EditorActionTypes.TOGGLE_PLUGIN; payload: { plugin: keyof EnabledPlugins; enabled: boolean } }
  | { type: EditorActionTypes.UPDATE_TOOLBAR; payload: Partial<ToolbarState> }
  | { type: EditorActionTypes.RESET_EDITOR };

/**
 * Editor reducer type
 */
export type EditorReducer = (state: EditorState, action: EditorAction) => EditorState;

/**
 * Editor context value
 */
export interface EditorContextValue {
  state: EditorState;
  dispatch: React.Dispatch<EditorAction>;
  // Editor actions
  initializeEditor: (editor: LexicalEditor, config?: Partial<EditorConfig>) => void;
  updateConfig: (config: Partial<EditorConfig>) => void;
  setContent: (content: EditorContent) => void;
  updateContent: (content: Partial<EditorContent>) => void;
  setReadOnly: (readOnly: boolean) => void;
  setLoading: (loading: boolean) => void;
  setSaving: (saving: boolean) => void;
  markUnsavedChanges: (hasChanges?: boolean) => void;
  markSaved: () => void;
  addError: (error: Omit<EditorError, 'id' | 'timestamp'>) => void;
  clearError: (errorId: string) => void;
  clearAllErrors: () => void;
  togglePlugin: (plugin: keyof EnabledPlugins, enabled: boolean) => void;
  updateToolbar: (toolbar: Partial<ToolbarState>) => void;
  resetEditor: () => void;
  // Utility functions
  getContent: (format: 'html' | 'markdown' | 'json' | 'plainText') => string | undefined;
  hasErrors: () => boolean;
  getErrorsByType: (type: EditorErrorType) => EditorError[];
  isPluginEnabled: (plugin: keyof EnabledPlugins) => boolean;
  canSave: () => boolean;
  // Nested provider support
  isNested: boolean;
  parentContext?: EditorContextValue;
}

/**
 * Editor provider props
 */
export interface EditorProviderProps {
  children: ReactNode;
  config?: Partial<EditorConfig>;
  initialState?: Partial<EditorState>;
  forceNested?: boolean;
}

/**
 * Default editor configuration
 */
export const defaultEditorConfig: EditorConfig = {
  namespace: 'GameGuildEditor',
  placeholder: 'Start typing...',
  readOnly: false,
  autoFocus: false,
  enableMarkdown: true,
  enableImageUpload: true,
  enableVideoEmbed: true,
  enableQuiz: true,
  enableHTML: true,
  enableAudio: true,
  enableDivider: true,
  enableCallout: true,
  enableButton: true,
  enableHeader: true,
  enableCode: true,
  maxContentLength: 50000,
  allowedFileTypes: ['image/jpeg', 'image/png', 'image/gif', 'image/webp'],
};

/**
 * Default enabled plugins
 */
export const defaultEnabledPlugins: EnabledPlugins = {
  richText: true,
  history: true,
  list: true,
  markdown: true,
  image: true,
  video: true,
  quiz: true,
  html: true,
  audio: true,
  divider: true,
  callout: true,
  button: true,
  header: true,
  code: true,
  floatingToolbar: true,
  contentInsert: true,
};

/**
 * Default toolbar state
 */
export const defaultToolbarState: ToolbarState = {
  isVisible: false,
  position: null,
  activeFormats: new Set(),
  selectedText: '',
};

/**
 * Default editor state
 */
export const defaultEditorState: EditorState = {
  editor: null,
  config: defaultEditorConfig,
  content: {
    html: '',
    markdown: '',
    json: '',
    plainText: '',
    wordCount: 0,
    characterCount: 0,
  },
  isInitialized: false,
  isReadOnly: false,
  isLoading: false,
  isSaving: false,
  hasUnsavedChanges: false,
  lastSaved: undefined,
  errors: [],
  plugins: defaultEnabledPlugins,
  toolbar: defaultToolbarState,
};
