'use client';

import { useEffect } from 'react';

/**
 * Hook to add editor-related styles to the document
 * @param options Configuration options for the editor styles
 * @returns void
 */
export function useEditorStyles(options?: { additionalStyles?: string }) {
  useEffect(() => {
    const style = document.createElement('style');
    style.textContent = `
    .monaco-editor-container {
      width: 100%;
      position: relative;
    }
    .monaco-editor-container .monaco-editor {
      width: 100% !important;
    }
    .monaco-editor-container .monaco-editor .overflow-guard {
      width: 100% !important;
    }
    [draggable=true] {
      cursor: grab;
    }
    [draggable=true]:active {
      cursor: grabbing;
    }
    .bg-striped {
      background-image: linear-gradient(45deg, rgba(0, 0, 0, 0.1) 25%, transparent 25%, transparent 50%, rgba(0, 0, 0, 0.1) 50%, rgba(0, 0, 0, 0.1) 75%, transparent 75%, transparent);
      background-size: 8px 8px;
      text-decoration: line-through;
      text-decoration-color: rgba(0, 0, 0, 0.3);
      text-decoration-thickness: 1px;
    }
    /* Ensure Monaco's widgets appear above other UI elements */
    .monaco-editor .suggest-widget {
      z-index: 1000 !important;
    }
    .monaco-editor .suggest-widget.visible {
      z-index: 1000 !important;
    }
    .monaco-editor-hover {
      z-index: 1000 !important;
    }
    .monaco-editor .parameter-hints-widget {
      z-index: 1000 !important;
    }
    ${options?.additionalStyles || ''}
    `;
    document.head.appendChild(style);

    // Clean up function to remove the style when the component unmounts
    return () => {
      document.head.removeChild(style);
    };
  }, [options?.additionalStyles]);
}
