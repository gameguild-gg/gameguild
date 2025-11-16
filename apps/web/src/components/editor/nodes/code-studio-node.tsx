"use client"

import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import type { JSX } from "react/jsx-runtime"
import { useContext } from "react"
import { EditorLoadingContext } from "../lexical-editor"

import type { CodeStudioData } from "../extras/code-studio/types"
import { CodeStudioEditor } from "../extras/code-studio/code-studio-editor"

export interface SerializedCodeStudioNode extends SerializedLexicalNode {
  type: "code-studio"
  data: CodeStudioData
  version: 1
}

export class CodeStudioNode extends DecoratorNode<JSX.Element> {
  __data: CodeStudioData

  static getType(): string {
    return "code-studio"
  }

  static clone(node: CodeStudioNode): CodeStudioNode {
    return new CodeStudioNode(node.__data, node.__key)
  }

  constructor(data: CodeStudioData, key?: string) {
    super(key)
    this.__data = {
      files: data.files || [],
      mode: data.mode || "execute",
      language: data.language || "javascript",
      readonly: data.readonly ?? false,
      showLineNumbers: data.showLineNumbers ?? true,
      fontSize: data.fontSize ?? 14,
      theme: data.theme ?? "system",
      clearOnRun: data.clearOnRun ?? false,
      autoRun: data.autoRun ?? false,
      showTests: data.showTests ?? false,
      testCases: data.testCases ?? {},
      activeFileId: data.activeFileId || data.files?.[0]?.id,
      title: data.title,
      caption: data.caption,
    }
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: CodeStudioData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedCodeStudioNode {
    return {
      type: "code-studio",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedCodeStudioNode): CodeStudioNode {
    return new CodeStudioNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <CodeStudioComponent data={this.__data} nodeKey={this.__key} />
  }
}

function CodeStudioComponent({ data, nodeKey }: { data: CodeStudioData; nodeKey: string }) {
  const [editor] = useLexicalComposerContext()
  const isLoading = useContext(EditorLoadingContext)

  const handleUpdateCodeStudio = (newData: Partial<CodeStudioData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof CodeStudioNode) {
        const updatedData = {
          ...data,
          ...newData,
        }
        node.setData(updatedData)
      }
    })
  }

  const handleSave = () => {
    console.log("Code Studio saved")
  }

  return (
    <CodeStudioEditor
      data={data}
      isPreview={false}
      onUpdate={handleUpdateCodeStudio}
      onSave={handleSave}
    />
  )
}

export function $createCodeStudioNode(): CodeStudioNode {
  return new CodeStudioNode({
    files: [
      {
        id: "1",
        name: "main.js",
        content: '// Write your code here\nconsole.log("Hello, World!");',
        language: "javascript",
        isMain: true,
        isVisible: true,
      },
    ],
    mode: "execute",
    language: "javascript",
    readonly: false,
    showLineNumbers: true,
    fontSize: 14,
    theme: "system",
    clearOnRun: false,
    autoRun: false,
    showTests: false,
    testCases: {},
  })
}
