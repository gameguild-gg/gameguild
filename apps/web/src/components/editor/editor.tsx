'use client';

import React from 'react';
import { LexicalComposer } from '@lexical/react/LexicalComposer';
import { Toaster } from '@/components/ui/sonner';
import { EditorProvider, useEditorContext, useEditorActions } from './editor-provider';
import { defaultEditorConfig, EditorConfig, EditorErrorType, EditorState } from './types';

type EditorInternalProps = {
  config?: Partial<EditorConfig>;
};

const EditorInternal = ({ config = {} }: EditorInternalProps): React.JSX.Element => {
  const { state } = useEditorContext();
  const { addError } = useEditorActions();

  const editorConfig = {
    ...defaultEditorConfig,
    ...state.config,
    ...config,
  };

  const lexicalConfig = {
    namespace: editorConfig.namespace,
    onError: (error: Error) => {
      console.error('Lexical Error:', error);
      addError({
        message: error.message,
        type: 'initialization',
        context: { error: error.stack },
      });
      editorConfig.onError?.(error);
    },
    nodes: [], // Add your custom nodes here
    editable: !editorConfig.readOnly,
  };

  return (
    <>
      <LexicalComposer initialConfig={lexicalConfig}>
        {/* Add your editor plugins and components here */}
        <div className="editor-container">{/* This is where you'll add the RichTextPlugin, ContentEditable, etc. */}</div>
      </LexicalComposer>
    </>
  );
};

type EditorProps = {
  config?: Partial<EditorConfig>;
  initialState?: Partial<EditorState>;
  scopeId?: string;
};

export const Editor = ({ config, initialState, scopeId }: EditorProps): React.JSX.Element => {
  return (
    <EditorProvider config={config} initialState={initialState} scopeId={scopeId}>
      <EditorInternal config={config} />
      {/* Sonner Toaster for editor notifications. */}
      <Toaster />
    </EditorProvider>
  );
};
