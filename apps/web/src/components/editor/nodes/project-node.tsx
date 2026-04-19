"use client"

import type React from "react"
import { useState, useEffect, useContext } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { DecoratorNode, type NodeKey, type LexicalNode, type SerializedLexicalNode, type LexicalEditor, $getNodeByKey } from "lexical"
import { Button } from "@/components/ui/button"
import { Edit2, ExternalLink, RefreshCw, FileText, Copy } from "lucide-react"
import { toast } from "sonner"
import type { SerializedEditorState } from "lexical"
import { PreviewRenderer } from "../extras/preview/preview-renderer"
import { RefreshConfirmDialog } from "../extras/dialogs/refresh-confirm-dialog"
import { StorageAdapterContext, Editor } from "@/components/editor/engines/lexical/lexical-editor"

export interface ProjectData {
  projectId: string
  projectName: string
  editorState: SerializedEditorState | null
  isLocalCopy: boolean
  isReference?: boolean // True when same level - only stores projectId reference
  wasReference?: boolean // Track if it was originally a reference before becoming local copy
  originalProjectId?: string
  size?: number
  caption?: string
}

export interface SerializedProjectNode extends SerializedLexicalNode {
  data: ProjectData
  type: "project"
  version: 1
}

export class ProjectNode extends DecoratorNode<React.JSX.Element> {
  __data: ProjectData

  static getType(): string {
    return "project"
  }

  static clone(node: ProjectNode): ProjectNode {
    return new ProjectNode(node.__data, node.__key)
  }

  constructor(data: ProjectData, key?: NodeKey) {
    super(key)
    this.__data = data
  }

  static importJSON(serializedNode: SerializedProjectNode): ProjectNode {
    return new ProjectNode(serializedNode.data)
  }

  exportJSON(): SerializedProjectNode {
    return {
      data: this.__data,
      type: "project",
      version: 1,
    }
  }

  createDOM(): HTMLElement {
    const div = document.createElement("div")
    div.className = "project-node-container"
    return div
  }

  updateDOM(): false {
    return false
  }

  getData(): ProjectData {
    return this.__data
  }

  setData(data: ProjectData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  decorate(editor: LexicalEditor): React.JSX.Element {
    return <ProjectComponent nodeKey={this.__key} data={this.__data} editor={editor} />
  }
}

export function $createProjectNode(data: ProjectData): ProjectNode {
  return new ProjectNode(data)
}

export function $isProjectNode(node: LexicalNode | null | undefined): node is ProjectNode {
  return node instanceof ProjectNode
}

// Component to render the imported project
function ProjectComponent({ nodeKey, data, editor }: { nodeKey: NodeKey; data: ProjectData; editor: LexicalEditor }) {
  const [isHovered, setIsHovered] = useState(false)
  const [projectData, setProjectData] = useState(data)
  const [showRefreshDialog, setShowRefreshDialog] = useState(false)
  const [loadedData, setLoadedData] = useState<ProjectData | null>(data.isReference ? null : data)
  const [isLoading, setIsLoading] = useState(data.isReference ? true : false)
  const [hasLoadedRef, setHasLoadedRef] = useState(false) // Track if reference was loaded
  const storageAdapter = useContext(StorageAdapterContext)

  // Load project data when it's a reference - only once
  useEffect(() => {
    if (projectData.isReference && storageAdapter && !hasLoadedRef && !loadedData) {
      const loadReferenceData = async () => {
        try {
          setIsLoading(true)
          const fullProject = await storageAdapter.load(projectData.projectId)
          
          if (!fullProject) {
            toast.error("Referenced project not found", {
              description: "The project may have been deleted",
              duration: 3000
            })
            setIsLoading(false)
            setHasLoadedRef(true)
            return
          }

          // Parse project data
          let editorState = null

          try {
            const data = JSON.parse(fullProject.data)
            // Single layout: data is the editor state directly, or data.blocks.b1
            if (data.blocks && data.blocks.b1) {
              editorState = data.blocks.b1
            } else {
              editorState = data
            }
          } catch (error) {
            console.error("Failed to parse project data:", error)
          }

          const loadedProjectData: ProjectData = {
            ...projectData,
            editorState,
            size: fullProject.size,
          }

          setLoadedData(loadedProjectData)
          setIsLoading(false)
          setHasLoadedRef(true)
        } catch (error) {
          console.error("Error loading reference project:", error)
          toast.error("Failed to load project", {
            description: "Could not load referenced project data",
            duration: 3000
          })
          setIsLoading(false)
          setHasLoadedRef(true)
        }
      }

      loadReferenceData()
    }
  }, [projectData.isReference, projectData.projectId, storageAdapter, hasLoadedRef, loadedData])

  // Format size
  const formatSize = (sizeInKB?: number): string => {
    if (!sizeInKB) return "Unknown size"
    if (sizeInKB < 1024) {
      return `${sizeInKB.toFixed(1)}KB`
    } else {
      return `${(sizeInKB / 1024).toFixed(1)}MB`
    }
  }

  // Handle editor changes - update node data
  const handleEditorChange = (newState: string) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (!node || !$isProjectNode(node)) return

      const parsedState = JSON.parse(newState)
      const updatedData: ProjectData = {
        ...projectData,
        editorState: parsedState,
      }

      const writableNode = node.getWritable() as ProjectNode
      writableNode.setData(updatedData)
      setProjectData(updatedData)
    })
  }

  // Handle block editor changes - update node data
  // Handle block editor changes - removed (type2 no longer supported)

  // Handle edit - creates local copy (permanent, no going back except refresh)
  const handleEdit = async () => {
    if (projectData.isLocalCopy) {
      // Already a local copy, nothing to do
      return
    }

    // If it's a reference, need to load data first
    if (projectData.isReference) {
      if (!loadedData || isLoading) {
        toast.error("Please wait", {
          description: "Loading project data...",
          duration: 2000
        })
        return
      }

      // Convert reference to local copy with full data
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (!node || !$isProjectNode(node)) return

        const newData: ProjectData = {
          ...loadedData,
          isLocalCopy: true,
          isReference: false,
          wasReference: true, // Track that it was originally a reference
          originalProjectId: projectData.projectId,
          projectId: `copy_${Date.now()}_${projectData.projectId}`,
        }

        const writableNode = node.getWritable() as ProjectNode
        writableNode.setData(newData)
        setProjectData(newData)
        setLoadedData(newData)

        toast.success("Editable copy created", {
          description: "You can now edit this project. Use Refresh to restore original.",
          duration: 3000
        })
      })
      return
    }

    // Create local copy from imported data
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)

      if (!node || !$isProjectNode(node)) return

      const newData: ProjectData = {
        ...projectData,
        isLocalCopy: true,
        originalProjectId: projectData.projectId,
        projectId: `copy_${Date.now()}_${projectData.projectId}`,
      }

      const writableNode = node.getWritable() as ProjectNode
      writableNode.setData(newData)
      setProjectData(newData)
      setLoadedData(newData)

      toast.success("Editable copy created", {
        description: "You can now edit this project. Use Refresh to restore original.",
        duration: 3000
      })
    })
  }

  // Handle refresh from original
  const handleRefresh = async () => {
    if (!projectData.isLocalCopy || !projectData.originalProjectId) {
      toast.error("Cannot refresh", {
        description: "This is not a local copy",
        duration: 2000
      })
      return
    }

    setShowRefreshDialog(true)
  }

  // Confirm refresh and load original project
  const confirmRefresh = async () => {
    if (!projectData.originalProjectId || !storageAdapter) {
      toast.error("Cannot refresh", {
        description: "Storage adapter not available",
        duration: 2000
      })
      return
    }

    try {
      // Check if it was originally a reference
      if (projectData.wasReference) {
        // Restore as reference (no data loading)
        const originalData: ProjectData = {
          projectId: projectData.originalProjectId,
          projectName: projectData.projectName,
          editorState: null,
          isLocalCopy: false,
          isReference: true,
          wasReference: undefined,
          originalProjectId: undefined,
          size: projectData.size,
          caption: projectData.caption,
        }

        // Update node
        editor.update(() => {
          const node = $getNodeByKey(nodeKey)
          if (!node || !$isProjectNode(node)) return

          const writableNode = node.getWritable() as ProjectNode
          writableNode.setData(originalData)
          setProjectData(originalData)
          setLoadedData(null)
          setHasLoadedRef(false) // Allow reload of reference

          toast.success("Project refreshed", {
            description: "Restored to reference state",
            duration: 3000
          })
        })
        return
      }

      // Load original project using correct method (for imported projects)
      const originalProject = await storageAdapter.load(projectData.originalProjectId)
      
      if (!originalProject) {
        toast.error("Original project not found", {
          description: "The original project may have been deleted",
          duration: 3000
        })
        return
      }

      // Parse project data
      let editorState = null

      try {
        const data = JSON.parse(originalProject.data)
        
        if (data.blocks && data.blocks.b1) {
          editorState = data.blocks.b1
        } else {
          editorState = data
        }
      } catch (error) {
        console.error("Failed to parse project data:", error)
        toast.error("Invalid project data", {
          description: "Could not parse project content",
          duration: 3000
        })
        return
      }

      // Reset to original state
      const originalData: ProjectData = {
        projectId: originalProject.id,
        projectName: originalProject.name,
        editorState,
        isLocalCopy: false,
        isReference: false,
        wasReference: undefined,
        originalProjectId: undefined,
        size: originalProject.size,
        caption: projectData.caption,
      }

      // Update node
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (!node || !$isProjectNode(node)) return

        const writableNode = node.getWritable() as ProjectNode
        writableNode.setData(originalData)
        setProjectData(originalData)
        setLoadedData(originalData)

        toast.success("Project refreshed", {
          description: "Restored to original state",
          duration: 3000
        })
      })
    } catch (error) {
      console.error("Error refreshing project:", error)
      toast.error("Failed to refresh", {
        description: "Could not load original project data",
        duration: 3000
      })
    }
  }

  // Handle open in new tab
  const handleOpenInNewTab = () => {
    // Always use original ID if available
    const targetId = projectData.originalProjectId || projectData.projectId
    const url = `/gglexical/studio#${targetId}`
    window.open(url, '_blank')
  }

  return (
    <div className="my-4 w-full border-l-4 border-primary/20 hover:border-primary/50 transition-colors">
      {/* Header with project info */}
      <div className="flex items-center justify-between bg-muted/30 px-4 py-2 mb-2">
        <div className="flex items-center gap-3 flex-1 min-w-0">
          <FileText className="h-5 w-5 text-muted-foreground shrink-0" />
          <div className="flex flex-col min-w-0">
            <div className="flex items-center gap-2">
              <span className="font-semibold truncate">
                {projectData.projectName}
              </span>
              {projectData.isLocalCopy && (
                <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium bg-muted text-muted-foreground rounded">
                  <Copy className="h-3 w-3" />
                  Local Copy (Editable)
                </span>
              )}
              {projectData.isReference && (
                <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 rounded">
                  <FileText className="h-3 w-3" />
                  Referenced
                </span>
              )}
            </div>
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <span className="capitalize">Single Project</span>
              <span>•</span>
              <span>{formatSize(projectData.size)}</span>
              {projectData.isLocalCopy && projectData.originalProjectId && (
                <>
                  <span>•</span>
                  <span className="font-mono">From: {projectData.originalProjectId.slice(0, 8)}</span>
                </>
              )}
            </div>
          </div>
        </div>

        {/* Action buttons */}
        <div className="flex items-center gap-1">
          {!projectData.isLocalCopy && (
            <Button
              variant="default"
              size="sm"
              onClick={handleEdit}
              title="Create editable copy"
              className="h-8 px-3"
            >
              <Edit2 className="h-4 w-4 mr-1" />
              Edit
            </Button>
          )}
          
          {projectData.isLocalCopy && projectData.originalProjectId && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleRefresh}
              title="Refresh from original project"
              className="h-8 w-8 p-0"
            >
              <RefreshCw className="h-4 w-4" />
            </Button>
          )}

          <Button
            variant="ghost"
            size="sm"
            onClick={handleOpenInNewTab}
            title="Open in new tab"
            className="h-8 w-8 p-0"
          >
            <ExternalLink className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Caption */}
      {projectData.caption && (
        <div className="px-4 py-2 text-sm text-muted-foreground bg-muted/10 mb-2">
          {projectData.caption}
        </div>
      )}

      {/* Content - Seamless integration without borders */}
      <div className="w-full">
        {isLoading ? (
          <div className="flex items-center justify-center py-12 px-4">
            <div className="text-center text-muted-foreground">
              <FileText className="h-12 w-12 mx-auto mb-2 opacity-50 animate-pulse" />
              <p className="text-sm">Loading project...</p>
            </div>
          </div>
        ) : !loadedData ? (
          <div className="flex items-center justify-center py-12 px-4">
            <div className="text-center text-muted-foreground">
              <FileText className="h-12 w-12 mx-auto mb-2 opacity-50" />
              <p className="text-sm">Failed to load project</p>
            </div>
          </div>
        ) : loadedData.editorState ? (
          <div className="w-full">
            {loadedData.isLocalCopy ? (
              <Editor
                initialState={JSON.stringify(loadedData.editorState)}
                onChange={handleEditorChange}
                className="border-0"
                mode="free-page"
              />
            ) : (
              <PreviewRenderer serializedState={loadedData.editorState} />
            )}
          </div>
        ) : (
          <div className="flex items-center justify-center py-12 px-4">
            <div className="text-center text-muted-foreground">
              <FileText className="h-12 w-12 mx-auto mb-2 opacity-50" />
              <p className="text-sm">No content available</p>
              <p className="text-xs mt-1">Single Layout</p>
            </div>
          </div>
        )}
      </div>

      {/* Refresh Confirmation Dialog */}
      <RefreshConfirmDialog
        open={showRefreshDialog}
        onOpenChange={setShowRefreshDialog}
        projectName={projectData.projectName}
        onConfirm={() => {
          setShowRefreshDialog(false)
          confirmRefresh()
        }}
      />
    </div>
  )
}
  
