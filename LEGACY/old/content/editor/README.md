# Editor with Reducer and Context

This document explains the new editor implementation using React's reducer pattern and context API, similar to the
error-boundary-provider.

## Overview

The editor system now consists of:

1. **Types** (`auth.types.ts`) - All TypeScript interfaces and types
2. **Provider** (`editor-provider.tsx`) - Context provider with reducer for state management
3. **Editor Component** (`editor.tsx`) - Main editor component that uses the provider
4. **Hooks** - Custom hooks for accessing and manipulating editor state

## Key Features

### State Management

- **Centralized state** using `useReducer` for all editor data
- **Persistent configuration** saved to localStorage
- **Error tracking** and management
- **Plugin state management**
- **Content tracking** with unsaved changes detection

### Nested Provider Support

- **Auto-detection** of nested providers
- **Configuration inheritance** from parent providers
- **Scope isolation** with unique IDs
- **Flexible nesting** for complex editor hierarchies

### TypeScript Support

- **Fully typed** interfaces and actions
- **Strict type checking** for all state updates
- **IntelliSense support** for better developer experience

## Usage Examples

### Basic Editor Usage

```tsx
import { Editor } from '@/components/editor';

function MyComponent() {
  return (
    <Editor
      config={{
        placeholder: 'Start writing...',
        autoFocus: true,
        enableMarkdown: true,
      }}
    />
  );
}
```

### Advanced Usage with Custom Provider

```tsx
import { EditorProvider, useEditorState, useEditorActions } from '@/components/editor';

function EditorToolbar() {
  const { state } = useEditorState();
  const { togglePlugin, markSaved } = useEditorActions();

  return (
    <div>
      <button onClick={() => togglePlugin('markdown', !state.plugins.markdown)}>Toggle Markdown: {state.plugins.markdown ? 'ON' : 'OFF'}</button>
      <button onClick={markSaved} disabled={!state.hasUnsavedChanges}>
        Save ({state.hasUnsavedChanges ? 'Unsaved' : 'Saved'})
      </button>
    </div>
  );
}

function MyEditor() {
  return (
    <EditorProvider
      config={{
        namespace: 'MyCustomEditor',
        enableImageUpload: true,
        maxContentLength: 10000,
      }}
    >
      <EditorToolbar />
      <Editor />
    </EditorProvider>
  );
}
```

### Nested Editors

```tsx
function NestedEditorExample() {
  return (
    <EditorProvider scopeId="main-editor">
      <h2>Main Editor</h2>
      <Editor />

      <h3>Comment Editor (Nested)</h3>
      <EditorProvider scopeId="comment-editor" config={{ placeholder: 'Write a comment...', enableImageUpload: false }}>
        <Editor />
      </EditorProvider>
    </EditorProvider>
  );
}
```

### Content Management

```tsx
import { useEditorContent } from '@/components/editor';

function ContentManager() {
  const { content, hasUnsavedChanges, setContent, getContent } = useEditorContent();

  const saveContent = async () => {
    const htmlContent = getContent('html');
    // Save to backend
    await api.saveContent(htmlContent);
  };

  const loadContent = async () => {
    const savedContent = await api.loadContent();
    setContent({ html: savedContent, plainText: extractText(savedContent) });
  };

  return (
    <div>
      <p>Word Count: {content.wordCount}</p>
      <p>Has Unsaved Changes: {hasUnsavedChanges ? 'Yes' : 'No'}</p>
      <button onClick={loadContent}>Load</button>
      <button onClick={saveContent}>Save</button>
    </div>
  );
}
```

### Error Handling

```tsx
import { useEditorStatus, useEditorActions } from '@/components/editor';

function ErrorDisplay() {
  const { errors, hasErrors } = useEditorStatus();
  const { clearError, clearAllErrors } = useEditorActions();

  if (!hasErrors) return null;

  return (
    <div className="error-panel">
      <h3>Editor Errors</h3>
      {errors.map((error) => (
        <div key={error.id} className="error-item">
          <p>{error.message}</p>
          <small>
            {error.type} - {error.timestamp.toLocaleString()}
          </small>
          <button onClick={() => clearError(error.id)}>Clear</button>
        </div>
      ))}
      <button onClick={clearAllErrors}>Clear All</button>
    </div>
  );
}
```

### Configuration Management

```tsx
import { useEditorConfig } from '@/components/editor';

function EditorSettings() {
  const { config, plugins, updateConfig, togglePlugin } = useEditorConfig();

  return (
    <div>
      <h3>Editor Settings</h3>

      <label>
        <input type="checkbox" checked={config.readOnly} onChange={(e) => updateConfig({ readOnly: e.target.checked })} />
        Read Only Mode
      </label>

      <label>
        <input type="text" value={config.placeholder} onChange={(e) => updateConfig({ placeholder: e.target.value })} />
        Placeholder Text
      </label>

      <h4>Plugins</h4>
      {Object.entries(plugins).map(([plugin, enabled]) => (
        <label key={plugin}>
          <input type="checkbox" checked={enabled} onChange={(e) => togglePlugin(plugin as keyof EnabledPlugins, e.target.checked)} />
          {plugin}
        </label>
      ))}
    </div>
  );
}
```

## Available Hooks

### `useEditorContext()`

Access the full editor context with state and all actions.

### `useEditorState()`

Access editor state and utility functions:

- `state` - Current editor state
- `getContent(format)` - Get content in specific format
- `hasErrors()` - Check if there are errors
- `getErrorsByType(type)` - Filter errors by type
- `isPluginEnabled(plugin)` - Check plugin status
- `canSave()` - Check if content can be saved

### `useEditorActions()`

Access action dispatchers:

- `initializeEditor(editor, config)` - Initialize Lexical editor
- `updateConfig(config)` - Update configuration
- `setContent(content)` - Set complete content
- `updateContent(content)` - Update partial content
- `markUnsavedChanges(hasChanges)` - Mark content as modified
- `markSaved()` - Mark content as saved
- `addError(error)` - Add an error
- `clearError(errorId)` - Clear specific error
- `clearAllErrors()` - Clear all errors
- `togglePlugin(plugin, enabled)` - Toggle plugin state
- `resetEditor()` - Reset editor to default state

### `useEditorContent()`

Specialized hook for content management:

- `content` - Current content object
- `hasUnsavedChanges` - Boolean flag
- `lastSaved` - Last save timestamp
- `setContent(content)` - Set content
- `updateContent(content)` - Update content
- `getContent(format)` - Get formatted content
- `markUnsavedChanges(hasChanges)` - Mark changes
- `markSaved()` - Mark as saved

### `useEditorConfig()`

Configuration management hook:

- `config` - Current configuration
- `plugins` - Plugin states
- `updateConfig(config)` - Update config
- `togglePlugin(plugin, enabled)` - Toggle plugins

### `useEditorStatus()`

Editor status information:

- `isInitialized` - Initialization status
- `isReadOnly` - Read-only mode
- `isLoading` - Loading state
- `isSaving` - Saving state
- `hasErrors` - Error state
- `canSave` - Save capability
- `errors` - Error list

## Migration from Old Editor

The old editor had basic structure with minimal state management. The new editor provides:

1. **Better state management** with predictable updates
2. **Error handling** built-in
3. **Plugin management** system
4. **Content tracking** with unsaved changes
5. **Nested editor support** for complex layouts
6. **Persistent configuration** across sessions
7. **TypeScript support** for better development experience

## Best Practices

1. **Use specific hooks** instead of `useEditorContext()` when possible
2. **Wrap related components** in a single `EditorProvider`
3. **Handle errors gracefully** using the error management system
4. **Persist important state** using the configuration system
5. **Use nested providers** for complex editor hierarchies
6. **Type your configurations** properly for better IntelliSense
