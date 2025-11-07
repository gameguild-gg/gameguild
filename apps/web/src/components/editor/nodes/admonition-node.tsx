"use client"

import { useState, useEffect, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { 
  ChevronDown, 
  Pencil, 
  Check,
  Notebook,
  Info,
  FileText,
  Flame,
  CheckCircle,
  HelpCircle,
  AlertTriangle,
  AlertCircle,
  Skull,
  Bug,
  List,
  Quote,
  Zap,
  ShieldAlert,
  Bell,
  Lightbulb,
  Check as CheckIcon,
  BookMarked,
} from "lucide-react"
import type { JSX } from "react/jsx-runtime"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Admonition as UIAdmonition, type AdmonitionType } from "@/components/editor/extras/admonition"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"
import { EditorLoadingContext } from "../lexical-editor"

export interface AdmonitionData {
  title: string
  content: string
  type: AdmonitionType
  customBorderColor?: string
  customTextColor?: string
  design?: "default" | "compact" | "bordered" | "vertical-bar"
  isNew?: boolean
}

export interface SerializedAdmonitionNode extends SerializedLexicalNode {
  type: "admonition"
  data: AdmonitionData
  version: 1
}

export class AdmonitionNode extends DecoratorNode<JSX.Element> {
  __data: AdmonitionData

  static getType(): string {
    return "admonition"
  }

  static clone(node: AdmonitionNode): AdmonitionNode {
    return new AdmonitionNode(node.__data, node.__key)
  }

  constructor(data: AdmonitionData, key?: string) {
    super(key)
    this.__data = {
      title: data.title || "",
      content: data.content || "",
      type: data.type || "note",
      customBorderColor: data.customBorderColor || "",
      customTextColor: data.customTextColor || "",
      design: data.design || "default",
      isNew: data.isNew,
    }
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: AdmonitionData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedAdmonitionNode {
    return {
      type: "admonition",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedAdmonitionNode): AdmonitionNode {
    return new AdmonitionNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <AdmonitionComponent data={this.__data} nodeKey={this.__key} />
  }
}

interface AdmonitionComponentProps {
  data: AdmonitionData
  nodeKey: string
}

const typeToIcon: Record<AdmonitionType, React.ReactNode> = {
  note: <Notebook className="h-4 w-4" />,
  abstract: <FileText className="h-4 w-4" />,
  info: <Info className="h-4 w-4" />,
  tip: <Flame className="h-4 w-4" />,
  success: <CheckCircle className="h-4 w-4" />,
  question: <HelpCircle className="h-4 w-4" />,
  warning: <AlertTriangle className="h-4 w-4" />,
  failure: <AlertCircle className="h-4 w-4" />,
  danger: <Skull className="h-4 w-4" />,
  bug: <Bug className="h-4 w-4" />,
  example: <List className="h-4 w-4" />,
  quote: <Quote className="h-4 w-4" />,
  important: <Zap className="h-4 w-4" />,
  caution: <ShieldAlert className="h-4 w-4" />,
  attention: <Bell className="h-4 w-4" />,
  hint: <Lightbulb className="h-4 w-4" />,
  check: <CheckIcon className="h-4 w-4" />,
  summary: <BookMarked className="h-4 w-4" />,
}

function AdmonitionComponent({ data, nodeKey }: AdmonitionComponentProps) {
  const [editor] = useLexicalComposerContext()
  const isLoading = useContext(EditorLoadingContext)
  const [isEditing, setIsEditing] = useState((data.isNew || false) && !isLoading)
  const [activeTab, setActiveTab] = useState<"content" | "customize">("content")
  const [title, setTitle] = useState(data.title || "")
  const [content, setContent] = useState(data.content || "")
  const [type, setType] = useState<AdmonitionType>(data.type || "note")
  const [customBorderColor, setCustomBorderColor] = useState(data.customBorderColor || "")
  const [customTextColor, setCustomTextColor] = useState(data.customTextColor || "")
  const [design, setDesign] = useState<"default" | "compact" | "bordered" | "vertical-bar">(data.design || "default")

  // Block body scroll and pointer events when modal is open
  useEffect(() => {
    if (isEditing) {
      document.body.style.overflow = 'hidden'
      document.body.style.pointerEvents = 'none'
      
      return () => {
        document.body.style.overflow = ''
        document.body.style.pointerEvents = ''
      }
    }
  }, [isEditing])

  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node instanceof AdmonitionNode) {
          const { isNew, ...rest } = data
          node.setData(rest)
        }
      })
    }
  }, [data, editor, nodeKey])

  useEffect(() => {
    if (isLoading) {
      setIsEditing(false)
    }
  }, [isLoading])

  const updateAdmonition = (newData: Partial<AdmonitionData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof AdmonitionNode) {
        node.setData({ ...data, ...newData })
      }
    })
  }

  const handleTitleChange = (newTitle: string) => {
    setTitle(newTitle)
    updateAdmonition({ title: newTitle })
  }

  const handleContentChange = (newContent: string) => {
    setContent(newContent)
    updateAdmonition({ content: newContent })
  }

  const handleTypeChange = (newType: AdmonitionType) => {
    setType(newType)
    updateAdmonition({ type: newType })
  }

  const handleCustomBorderColorChange = (newColor: string) => {
    setCustomBorderColor(newColor)
    updateAdmonition({ customBorderColor: newColor })
  }

  const handleCustomTextColorChange = (newColor: string) => {
    setCustomTextColor(newColor)
    updateAdmonition({ customTextColor: newColor })
  }

  const handleDesignChange = (newDesign: "default" | "compact" | "bordered" | "vertical-bar") => {
    setDesign(newDesign)
    updateAdmonition({ design: newDesign })
  }

  const handleClose = () => {
    // Restore body styles before closing
    document.body.style.overflow = ''
    document.body.style.pointerEvents = ''
    setIsEditing(false)
  }

  if (!isEditing) {
    return (
      <div className="my-4 relative">
        <UIAdmonition 
          title={title} 
          content={content} 
          type={type} 
          customBorderColor={customBorderColor}
          customTextColor={customTextColor}
          design={design}
        />
        <ContentEditMenu
          options={[
            {
              id: "edit",
              icon: <Pencil className="h-4 w-4" />,
              label: "Edit Admonition",
              action: () => setIsEditing(true),
            },
          ]}
        />
      </div>
    )
  }

  return (
    <div
      className="fixed inset-0 bg-black/60 dark:bg-black/80 backdrop-blur-sm flex items-center justify-center z-50"
      style={{ pointerEvents: 'auto' }}
      onClick={handleClose}
      onKeyDown={(e) => {
        e.stopPropagation()
        if (e.key === 'Escape') {
          handleClose()
        }
      }}
      onKeyUp={(e) => e.stopPropagation()}
      onKeyPress={(e) => e.stopPropagation()}
    >
      <div
        className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 shadow-2xl p-6 max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="space-y-4">
          <div className="flex items-center justify-between mb-4 border-b border-gray-200 dark:border-gray-700 pb-4">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Configure Admonition</h3>
            <Button 
              variant="ghost" 
              size="sm" 
              onClick={handleClose}
              className="hover:bg-gray-100 dark:hover:bg-gray-800"
            >
              <Check className="h-4 w-4 mr-2" />
              Done
            </Button>
          </div>

          {/* Tabs */}
          <div className="flex gap-1 border-b border-gray-200 dark:border-gray-700">
            <button
              onClick={() => setActiveTab("content")}
              className={`px-4 py-2 text-sm font-medium transition-colors ${
                activeTab === "content"
                  ? "text-blue-600 dark:text-blue-400 border-b-2 border-blue-600 dark:border-blue-400"
                  : "text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200"
              }`}
            >
              Content
            </button>
            <button
              onClick={() => setActiveTab("customize")}
              className={`px-4 py-2 text-sm font-medium transition-colors ${
                activeTab === "customize"
                  ? "text-blue-600 dark:text-blue-400 border-b-2 border-blue-600 dark:border-blue-400"
                  : "text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200"
              }`}
            >
              Customize
            </button>
          </div>

          <div className="grid gap-4 min-h-[300px]">
            {activeTab === "content" && (
              <>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <label htmlFor="admonition-design" className="text-sm font-medium text-gray-900 dark:text-gray-100">
                      Design Style
                    </label>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button 
                          variant="outline" 
                          className="w-full justify-between bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700 text-gray-900 dark:text-gray-100"
                        >
                          <div className="flex items-center gap-2 capitalize">{design}</div>
                          <ChevronDown className="h-4 w-4 ml-2" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent className="w-full bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700">
                        <DropdownMenuItem onClick={() => handleDesignChange("default")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                          Default - Full background
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleDesignChange("compact")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                          Compact - Left border
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleDesignChange("bordered")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                          Bordered - Full border
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleDesignChange("vertical-bar")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                          Vertical Bar - Left accent
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>

                  <div className="space-y-1">
                    <label htmlFor="admonition-type" className="text-sm font-medium text-gray-900 dark:text-gray-100">
                      Type
                    </label>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button 
                          variant="outline" 
                          className="w-full justify-between bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700 text-gray-900 dark:text-gray-100"
                        >
                          <div className="flex items-center gap-2 capitalize">
                            {typeToIcon[type]}
                            {type}
                          </div>
                          <ChevronDown className="h-4 w-4 ml-2" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent className="w-[480px] bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700">
                        <div className="grid grid-cols-2 gap-1 p-1">
                          <DropdownMenuItem onClick={() => handleTypeChange("note")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <Notebook className="h-4 w-4 mr-2" />Note
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("important")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <Zap className="h-4 w-4 mr-2" />Important
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("abstract")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <FileText className="h-4 w-4 mr-2" />Abstract
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("caution")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <ShieldAlert className="h-4 w-4 mr-2" />Caution
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("info")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <Info className="h-4 w-4 mr-2" />Info
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("attention")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <Bell className="h-4 w-4 mr-2" />Attention
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("tip")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <Flame className="h-4 w-4 mr-2" />Tip
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("hint")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <Lightbulb className="h-4 w-4 mr-2" />Hint
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("success")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <CheckCircle className="h-4 w-4 mr-2" />Success
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("check")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <CheckIcon className="h-4 w-4 mr-2" />Check
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("question")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <HelpCircle className="h-4 w-4 mr-2" />Question
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("summary")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <BookMarked className="h-4 w-4 mr-2" />Summary
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("warning")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <AlertTriangle className="h-4 w-4 mr-2" />Warning
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("bug")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <Bug className="h-4 w-4 mr-2" />Bug
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("failure")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <AlertCircle className="h-4 w-4 mr-2" />Failure
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("example")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <List className="h-4 w-4 mr-2" />Example
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("danger")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <Skull className="h-4 w-4 mr-2" />Danger
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleTypeChange("quote")} className="dark:hover:bg-gray-700 dark:focus:bg-gray-700">
                            <Quote className="h-4 w-4 mr-2" />Quote
                          </DropdownMenuItem>
                        </div>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                </div>

                <div className="space-y-1">
                  <label htmlFor="admonition-content" className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    Content
                  </label>
                  <Textarea
                id="admonition-content"
                value={content}
                onChange={(e) => handleContentChange(e.target.value)}
                placeholder="Admonition content"
                rows={4}
                className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100 placeholder:text-gray-500 dark:placeholder:text-gray-400"
                  />
                </div>
              </>
            )}

            {activeTab === "customize" && (
              <>
                <div className="space-y-1">
                  <label htmlFor="admonition-title" className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    Title (optional)
                  </label>
                  <Input
                    id="admonition-title"
                    value={title}
                    onChange={(e) => handleTitleChange(e.target.value)}
                    placeholder="Admonition title"
                    className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100 placeholder:text-gray-500 dark:placeholder:text-gray-400"
                  />
                </div>

                <div className="space-y-1">
                  <label htmlFor="admonition-border-color" className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    Border & Background Color (optional)
                  </label>
                  <div className="flex gap-2 items-center">
                    <Input
                      id="admonition-border-color"
                      type="color"
                      value={customBorderColor || "#3b82f6"}
                      onChange={(e) => handleCustomBorderColorChange(e.target.value)}
                      className="w-20 h-10 cursor-pointer"
                    />
                    <Input
                      type="text"
                      value={customBorderColor}
                      onChange={(e) => handleCustomBorderColorChange(e.target.value)}
                      placeholder="#3b82f6"
                      className="flex-1 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100 placeholder:text-gray-500 dark:placeholder:text-gray-400"
                    />
                    {customBorderColor && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleCustomBorderColorChange("")}
                        className="dark:hover:bg-gray-700"
                      >
                        Reset
                      </Button>
                    )}
                  </div>
                </div>

                <div className="space-y-1">
                  <label htmlFor="admonition-text-color" className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    Text Color (optional)
                  </label>
                  <div className="flex gap-2 items-center">
                    <Input
                      id="admonition-text-color"
                      type="color"
                      value={customTextColor || "#ffffff"}
                      onChange={(e) => handleCustomTextColorChange(e.target.value)}
                      className="w-20 h-10 cursor-pointer"
                    />
                    <Input
                      type="text"
                      value={customTextColor}
                      onChange={(e) => handleCustomTextColorChange(e.target.value)}
                      placeholder="#ffffff"
                      className="flex-1 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100 placeholder:text-gray-500 dark:placeholder:text-gray-400"
                    />
                    {customTextColor && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleCustomTextColorChange("")}
                        className="dark:hover:bg-gray-700"
                      >
                        Reset
                      </Button>
                    )}
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    Leave colors empty to use the default colors for the selected type
                  </p>
                </div>
              </>
            )}
          </div>
          <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
              <h4 className="text-sm font-medium mb-3 text-gray-900 dark:text-gray-100">Preview</h4>
              <div className="rounded-lg min-h-[120px] bg-gray-50 dark:bg-gray-800/50 p-4">
                <UIAdmonition 
                  title={title} 
                  content={content} 
                  type={type} 
                  customBorderColor={customBorderColor}
                  customTextColor={customTextColor}
                  design={design}
                />
              </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export function $createAdmonitionNode(data: Partial<AdmonitionData> = {}): AdmonitionNode {
  return new AdmonitionNode({
    title: data.title || "",
    content: data.content || "",
    type: data.type || "note",
    isNew: true,
  })
}
