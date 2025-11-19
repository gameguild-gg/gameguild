"use client"

import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import type { JSX } from "react/jsx-runtime"
import { useContext, useState, useEffect } from "react"
import { EditorLoadingContext } from "../lexical-editor"
import { Edit } from "lucide-react"

import type { CodeStudioData, CodeStudioMode } from "../extras/code-studio/types"
import { CodeStudioEditor } from "../extras/code-studio/code-studio-editor"
import { ModeSelectionDialog } from "../extras/code-studio/mode-selection-dialog"
import { LANGUAGE_CONFIGS } from "../extras/code-studio/types"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"

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
      folders: data.folders || [],
      openTabs: data.openTabs || [],
      mode: data.mode,
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
      isCodeMode: data.isCodeMode ?? false,
      showFileExplorer: data.showFileExplorer ?? true,
      shikiTheme: data.shikiTheme ?? "github",
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
  const [showEditor, setShowEditor] = useState(false)
  const [showModeSelection, setShowModeSelection] = useState(false)
  const [hasAutoOpened, setHasAutoOpened] = useState(false)
  const [selectedMode, setSelectedMode] = useState<EditorMode | null>(null)

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

  const handleModeSelect = (mode: EditorMode) => {
    setSelectedMode(mode)
    setShowModeSelection(false)
    
    // Criar arquivo inicial baseado na linguagem padrão
    const defaultLanguage = data.language || "javascript"
    const languageConfig = LANGUAGE_CONFIGS[defaultLanguage]
    const fileId = "1"
    
    // Update node with selected mode and initial file
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof CodeStudioNode) {
        const updatedData: CodeStudioData = {
          ...data,
          mode,
          files: [
            {
              id: fileId,
              name: `main${languageConfig.defaultExtension}`,
              content: languageConfig.defaultTemplate,
              language: defaultLanguage,
              isMain: true,
              isVisible: true,
              path: `main${languageConfig.defaultExtension}`,
            },
          ],
          folders: [],
          openTabs: [fileId],
          activeFileId: fileId,
        }
        node.setData(updatedData)
      }
    })
    
    // Open editor after mode selection
    setShowEditor(true)
  }

  const handleSave = (updatedData: CodeStudioData) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof CodeStudioNode) {
        node.setData(updatedData)
      }
    })
    setShowEditor(false)
  }

  const handleCancel = () => {
    setShowEditor(false)
    setShowModeSelection(false)
  }

  const handleEdit = () => {
    setShowEditor(true)
  }

  // Auto-open mode selection for new/empty code studio
  useEffect(() => {
    const isNew = !data.files || data.files.length === 0 || 
                  (data.files.length === 1 && data.files[0] && !data.files[0].content)
    
    // If new and no mode set, show mode selection
    if (isNew && !hasAutoOpened && !data.mode) {
      setShowModeSelection(true)
      setHasAutoOpened(true)
    } else if (isNew && !hasAutoOpened && data.mode) {
      // If has mode but is new, open editor directly
      setShowEditor(true)
      setHasAutoOpened(true)
    }
  }, [data.files, data.mode, hasAutoOpened])

  return (
    <>
      {showModeSelection && (
        <ModeSelectionDialog
          onSelect={handleModeSelect}
          onCancel={handleCancel}
        />
      )}
      
      {showEditor ? (
        <CodeStudioEditor
          data={data}
          isPreview={false}
          onUpdate={handleUpdateCodeStudio}
          onSave={handleSave}
          onCancel={handleCancel}
        />
      ) : (
        <div className="relative">
          <CodeStudioEditor
            data={data}
            isPreview={true}
            onUpdate={handleUpdateCodeStudio}
            onSave={handleSave}
            onEdit={handleEdit}
          />
          
          {/* ContentEditMenu for lateral edit button */}
          <ContentEditMenu
            options={[
              {
                id: "edit",
                icon: <Edit className="h-4 w-4" />,
                label: "Edit Code Studio",
                action: handleEdit,
              },
            ]}
          />
        </div>
      )}
    </>
  )
}

export function $createCodeStudioNode(mode: CodeStudioMode = "execution"): CodeStudioNode {
  return new CodeStudioNode({
    files: [],
    folders: [],
    openTabs: [],
    mode,
    language: "javascript",
    readonly: false,
    showLineNumbers: true,
    fontSize: 14,
    theme: "system",
    clearOnRun: false,
    autoRun: false,
    showTests: false,
    testCases: {},
    showFileExplorer: true,
    shikiTheme: "github",
  })
}
