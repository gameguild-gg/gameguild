"use client"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { $getNodeByKey, DecoratorNode, type SerializedLexicalNode } from "lexical";
import { useContext } from "react";
import type { JSX } from "react/jsx-runtime"; // Import JSX to fix the undeclared variable error
import { EditorLoadingContext } from "../lexical-editor";

// Import types
import { SourceCodeCore, type SourceCodeNodeData } from "./source-code-core";

// Re-export SourceCodeNodeData to ensure type consistency
export type { SourceCodeNodeData } from "./source-code-core";

export interface SerializedSourceCodeNode extends SerializedLexicalNode {
  type: "source-code"
  data: SourceCodeNodeData
  version: 1
}

export class SourceCodeNode extends DecoratorNode<JSX.Element> {
  __data: SourceCodeNodeData

  static getType(): string {
    return "source-code"
  }

  static clone(node: SourceCodeNode): SourceCodeNode {
    return new SourceCodeNode(node.__data, node.__key)
  }

  constructor(data: SourceCodeNodeData, key?: string) {
    super(key)
    this.__data = {
      files: data.files || [],
      readonly: data.readonly ?? false,
      showExecution: data.showExecution ?? false,
      isNew: data.isNew,
      isDarkTheme: data.isDarkTheme ?? false,
      activeFileId: data.activeFileId || data.files?.[0]?.id,
      selectedLanguage: data.selectedLanguage || "javascript",
      clearTerminalOnRun: data.clearTerminalOnRun ?? false,
      showBasicFileActionsInReadMode: data.showBasicFileActionsInReadMode ?? true,
      showFilePropertiesInReadMode: data.showFilePropertiesInReadMode ?? false,
      showTests: data.showTests ?? false,
      testCases: data.testCases ?? {},
      activeTab: data.activeTab ?? "terminal",
    }
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: SourceCodeNodeData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedSourceCodeNode {
    return {
      type: "source-code",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedSourceCodeNode): SourceCodeNode {
    return new SourceCodeNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <SourceCodeComponent data={this.__data} nodeKey={this.__key} />
  }
}

function SourceCodeComponent({ data, nodeKey }: { data: SourceCodeNodeData; nodeKey: string }) {
  const [editor] = useLexicalComposerContext()
  const isLoading = useContext(EditorLoadingContext)

  // Handle updating the source code node
  const handleUpdateSourceCode = (newData: Partial<SourceCodeNodeData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof SourceCodeNode) {
        const updatedData = {
          ...data,
          ...newData,
          // Don't set isNew if we're loading
          isNew: isLoading ? false : newData.isNew,
        }
        console.log("Setting node data:", updatedData)
        node.setData(updatedData)
      }
    })
  }

  // Handle save action
  const handleSave = () => {
    console.log("Source code node saved")
  }

  return (
    <SourceCodeCore data={data} isPreview={false} onUpdateSourceCode={handleUpdateSourceCode} onSave={handleSave} />
  )
}

export function $createSourceCodeNode(): SourceCodeNode {
  return new SourceCodeNode({
    files: [],
    readonly: false,
    showExecution: false,
    isNew: true,
    isDarkTheme: false,
    selectedLanguage: "javascript",
    clearTerminalOnRun: false,
    showTests: false,
    testCases: {},
    activeTab: "terminal",
  })
}
