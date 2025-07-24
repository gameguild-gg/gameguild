import React from 'react';
import { Editor, EditorProvider } from '@/components/editor';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <>
      <EditorProvider>
        <Editor />
      </EditorProvider>
    </>
  );
}
