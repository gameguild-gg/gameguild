"use client"

import { useState, useCallback } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import type { Block } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import type { BlockCellType } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { BLOCK_REGISTRY } from "./block-component-registry"
import {
  AdmonitionEditor,
  ButtonEditor,
  CodeStudioEditor,
  DividerEditor,
  HTMLEditor,
  MarkdownEditor,
  MermaidEditor,
  ModeSelectionDialog,
  QuizSettingsDialog,
  RichTextEditor,
  TableEditor,
  UnifiedMediaEditor,
  VegaLiteEditor,
} from "@/components/block-content-editor/lazy-client-components"

// Pattern A standalone editors
import type { CodeStudioMode } from "@/components/block-content-editor/extras/code-studio/types"
import { LANGUAGE_CONFIGS } from "@/components/block-content-editor/extras/code-studio/types"
import type { BaseMediaData } from "@/components/block-content-editor/nodes/base/media-node-base"

// ============================================================================
// Simple form editors for Pattern B types (no standalone editor exists)
// ============================================================================

function YouTubeForm({ data, onChange }: { data: any; onChange: (d: any) => void }) {
  return (
    <div className="space-y-4">
      <div>
        <Label>YouTube Video URL or ID</Label>
        <Input
          placeholder="https://youtube.com/watch?v=... or video ID"
          value={data.videoId || ""}
          onChange={(e) => {
            const val = e.target.value
            // Extract videoId from URL if needed
            const match = val.match(/(?:v=|youtu\.be\/)([a-zA-Z0-9_-]{11})/)
            onChange({ ...data, videoId: match ? match[1] : val, isNew: false })
          }}
        />
      </div>
      <div>
        <Label>Title</Label>
        <Input placeholder="Video title" value={data.title || ""} onChange={(e) => onChange({ ...data, title: e.target.value })} />
      </div>
      <div>
        <Label>Caption</Label>
        <Input placeholder="Caption text" value={data.caption || ""} onChange={(e) => onChange({ ...data, caption: e.target.value })} />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <Label>Size (%)</Label>
          <Input type="number" min={10} max={100} value={data.size ?? 100} onChange={(e) => onChange({ ...data, size: Number(e.target.value) })} />
        </div>
        <div>
          <Label>Start at (seconds)</Label>
          <Input type="number" min={0} value={data.startAt ?? 0} onChange={(e) => onChange({ ...data, startAt: Number(e.target.value) })} />
        </div>
      </div>
    </div>
  )
}

function SpotifyForm({ data, onChange }: { data: any; onChange: (d: any) => void }) {
  return (
    <div className="space-y-4">
      <div>
        <Label>Spotify URL or ID</Label>
        <Input
          placeholder="https://open.spotify.com/track/... or ID"
          value={data.spotifyId || ""}
          onChange={(e) => {
            const val = e.target.value
            const match = val.match(/(?:track|album|playlist|artist)\/([a-zA-Z0-9]+)/)
            if (match) {
              const typeMatch = val.match(/(track|album|playlist|artist)\//)
              onChange({ ...data, spotifyId: match[1], type: typeMatch?.[1] || data.type, isNew: false })
            } else {
              onChange({ ...data, spotifyId: val, isNew: false })
            }
          }}
        />
      </div>
      <div>
        <Label>Type</Label>
        <Select value={data.type || "track"} onValueChange={(v) => onChange({ ...data, type: v })}>
          <SelectTrigger><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="track">Track</SelectItem>
            <SelectItem value="album">Album</SelectItem>
            <SelectItem value="playlist">Playlist</SelectItem>
            <SelectItem value="artist">Artist</SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div>
        <Label>Title</Label>
        <Input placeholder="Title" value={data.title || ""} onChange={(e) => onChange({ ...data, title: e.target.value })} />
      </div>
    </div>
  )
}

function HeaderForm({ data, onChange }: { data: any; onChange: (d: any) => void }) {
  return (
    <div className="space-y-4">
      <div>
        <Label>Header Text</Label>
        <Input placeholder="Section title" value={data.text || ""} onChange={(e) => onChange({ ...data, text: e.target.value })} autoFocus />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <Label>Level</Label>
          <Select value={String(data.level ?? 1)} onValueChange={(v) => onChange({ ...data, level: Number(v) })}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              {[1, 2, 3, 4, 5, 6].map((l) => (
                <SelectItem key={l} value={String(l)}>H{l}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div>
          <Label>Style</Label>
          <Select value={data.style || "default"} onValueChange={(v) => onChange({ ...data, style: v })}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="default">Default</SelectItem>
              <SelectItem value="underlined">Underlined</SelectItem>
              <SelectItem value="bordered">Bordered</SelectItem>
              <SelectItem value="gradient">Gradient</SelectItem>
              <SelectItem value="accent">Accent</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>
    </div>
  )
}

function SourceForm({ data, onChange }: { data: any; onChange: (d: any) => void }) {
  const sources: any[] = data.sources || []

  const addSource = () => {
    onChange({
      ...data,
      sources: [...sources, { id: crypto.randomUUID(), type: "website", author: "", title: "", url: "" }],
    })
  }

  const updateSource = (index: number, field: string, value: string) => {
    const newSources = sources.map((s, i) => (i === index ? { ...s, [field]: value } : s))
    onChange({ ...data, sources: newSources })
  }

  const removeSource = (index: number) => {
    onChange({ ...data, sources: sources.filter((_, i) => i !== index) })
  }

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <Label>Title</Label>
          <Input value={data.title || "References"} onChange={(e) => onChange({ ...data, title: e.target.value })} />
        </div>
        <div>
          <Label>Citation Style</Label>
          <Select value={data.style || "apa"} onValueChange={(v) => onChange({ ...data, style: v })}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              {["apa", "mla", "chicago", "harvard", "ieee", "abnt"].map((s) => (
                <SelectItem key={s} value={s}>{s.toUpperCase()}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>
      {sources.map((src, i) => (
        <div key={src.id || i} className="border rounded p-3 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-gray-500">Source #{i + 1}</span>
            <button type="button" onClick={() => removeSource(i)} className="text-xs text-red-500 hover:text-red-700">Remove</button>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <Input placeholder="Author" value={src.author || ""} onChange={(e) => updateSource(i, "author", e.target.value)} />
            <Input placeholder="Title" value={src.title || ""} onChange={(e) => updateSource(i, "title", e.target.value)} />
            <Input placeholder="URL" value={src.url || ""} onChange={(e) => updateSource(i, "url", e.target.value)} />
            <Input placeholder="Year" value={src.year || ""} onChange={(e) => updateSource(i, "year", e.target.value)} />
          </div>
        </div>
      ))}
      <Button variant="outline" size="sm" onClick={addSource}>+ Add Source</Button>
    </div>
  )
}

// ============================================================================
// Main Block Editor Modal
// ============================================================================

interface BlockEditorModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  block: Block | null
  blockType: BlockCellType | null
  onSave: (data: any) => void
}

export function BlockEditorModal({ open, onOpenChange, block, blockType, onSave }: BlockEditorModalProps) {
  const [editData, setEditData] = useState<any>(null)
  const [showModeSelection, setShowModeSelection] = useState(false)

  // Initialize editData when block changes
  const currentData = editData ?? block?.data ?? null

  const handleSave = useCallback((data: any) => {
    onSave(data)
    setEditData(null)
    setShowModeSelection(false)
    onOpenChange(false)
  }, [onSave, onOpenChange])

  const handleCancel = useCallback(() => {
    setEditData(null)
    setShowModeSelection(false)
    onOpenChange(false)
  }, [onOpenChange])

  const handleModeSelect = useCallback((mode: CodeStudioMode) => {
    const defaultLanguage = (currentData?.language || "javascript") as keyof typeof LANGUAGE_CONFIGS
    const languageConfig = LANGUAGE_CONFIGS[defaultLanguage]
    const fileId = "1"
    setEditData({
      ...currentData,
      mode,
      files: [
        {
          id: fileId,
          name: `main${languageConfig.defaultExtension}`,
          content: languageConfig.defaultTemplate,
          language: defaultLanguage,
          isFile: 'f' as const,
          isVisible: true,
          path: `main${languageConfig.defaultExtension}`,
        },
      ],
      folders: [],
      openTabs: [fileId],
      activeFileId: fileId,
    })
    setShowModeSelection(false)
  }, [currentData])

  const handleDataChange = useCallback((newData: any) => {
    setEditData(newData)
  }, [])

  if (!blockType || !currentData) return null

  const config = BLOCK_REGISTRY[blockType as BlockCellType]
  if (!config) return null

  // ─── Pattern A: Standalone editors that manage their own UI ───

  // Quiz — has its own dialog
  if (blockType === "quiz") {
    return (
      <QuizSettingsDialog
        isOpen={open}
        onClose={handleCancel}
        entry={currentData}
        onSave={handleSave}
      />
    )
  }

  // Code Studio — mode selection for new blocks, then full-screen editor
  if (blockType === "code-studio") {
    if (!open) return null
    const isNewCodeStudio = !currentData.files || currentData.files.length === 0
    if (isNewCodeStudio && !showModeSelection && !editData) {
      // Auto-show mode selection for new Code Studio blocks
      return <ModeSelectionDialog onSelect={handleModeSelect} onCancel={handleCancel} />
    }
    return <CodeStudioEditor data={currentData} onSave={handleSave} onCancel={handleCancel} />
  }

  // Markdown — renders its own full-screen overlay
  if (blockType === "markdown") {
    if (!open) return null
    return <MarkdownEditor initialData={currentData} onSave={handleSave} onCancel={handleCancel} />
  }

  // Mermaid — renders its own full-screen overlay
  if (blockType === "mermaid") {
    if (!open) return null
    return <MermaidEditor initialData={currentData} onSave={handleSave} onCancel={handleCancel} />
  }

  // Vega-Lite — renders its own full-screen overlay
  if (blockType === "vega-lite") {
    if (!open) return null
    return <VegaLiteEditor initialData={currentData} onSave={handleSave} onCancel={handleCancel} />
  }

  // Media types (image, video, audio) — UnifiedMediaEditor renders its own overlay
  if (blockType === "image" || blockType === "video" || blockType === "audio") {
    if (!open) return null
    const mediaType = blockType
    return (
      <UnifiedMediaEditor
        data={{ ...currentData, type: mediaType } as BaseMediaData}
        onChange={(partial) => handleDataChange({ ...currentData, ...editData, ...partial })}
        onSave={(items?: BaseMediaData[]) => {
          if (items && items.length > 0 && items[0]) {
            handleSave(items[0])
          } else {
            handleCancel()
          }
        }}
        onClose={handleCancel}
      />
    )
  }

  // Gallery — UnifiedMediaEditor in gallery mode, renders its own overlay
  if (blockType === "gallery") {
    if (!open) return null
    const galleryImages: BaseMediaData[] = (currentData.images || []).map((img: any) => ({
      type: "image" as const,
      src: img.src || "",
      alt: img.alt || "",
      caption: img.caption || "",
      size: 100,
    }))
    return (
      <UnifiedMediaEditor
        data={galleryImages[0] || { type: "image" as const, src: "", alt: "", size: 100, isNew: true } as BaseMediaData}
        onChange={() => {}}
        onSave={(items?: BaseMediaData[], columns?: number, caption?: string) => {
          if (!items || items.length === 0) {
            handleCancel()
            return
          }
          const galleryImgs = items.filter(i => i.src && i.src.trim() !== "").map(item => ({
            id: Math.random().toString(36).substring(7),
            src: item.src || "",
            alt: item.alt || "",
            caption: item.caption || "",
            displayMode: "adaptive" as const,
            span: "1x1" as const,
          }))
          handleSave({
            images: galleryImgs,
            layout: columns?.toString() || currentData.layout || "2",
            caption: caption ?? currentData.caption ?? "",
            defaultDisplayMode: currentData.defaultDisplayMode || "crop",
          })
        }}
        onClose={handleCancel}
        galleryItems={galleryImages}
        galleryColumns={Number(currentData.layout) || 2}
        galleryCaption={currentData.caption || ""}
      />
    )
  }

  // Divider, Button, Admonition — render their own full-screen overlay
  if (blockType === "divider") {
    if (!open) return null
    return <DividerEditor initialData={currentData} onSave={handleSave} onCancel={handleCancel} />
  }

  if (blockType === "button") {
    if (!open) return null
    return <ButtonEditor initialData={currentData} onSave={handleSave} onCancel={handleCancel} />
  }

  if (blockType === "admonition") {
    if (!open) return null
    return <AdmonitionEditor initialData={currentData} onSave={handleSave} onCancel={handleCancel} />
  }

  // Table — renders its own full-screen overlay
  if (blockType === "table") {
    if (!open) return null
    return <TableEditor initialData={currentData} onSave={handleSave} onCancel={handleCancel} />
  }

  // HTML — renders its own full-screen overlay
  if (blockType === "html") {
    if (!open) return null
    return <HTMLEditor initialData={currentData} onSave={handleSave} onCancel={handleCancel} />
  }

  // Rich Text — renders its own full-screen overlay
  if (blockType === "rich-text") {
    if (!open) return null
    return <RichTextEditor initialData={currentData} onSave={handleSave} onCancel={handleCancel} />
  }

  // ─── Pattern B: Simple form editors ───

  const formMap: Record<string, React.ComponentType<{ data: any; onChange: (d: any) => void }>> = {
    "youtube": YouTubeForm,
    "spotify": SpotifyForm,
    "header": HeaderForm,
    "source": SourceForm,
  }
  const FormComponent = formMap[blockType]

  // Presentation and Project get a simple form too
  if (blockType === "presentation") {
    return (
      <Dialog open={open} onOpenChange={(v) => { if (!v) handleCancel() }}>
        <DialogContent className="max-w-2xl">
          <DialogHeader><DialogTitle>Presentation Settings</DialogTitle></DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>Title</Label>
              <Input
                value={(editData ?? currentData).title || ""}
                onChange={(e) => handleDataChange({ ...currentData, ...editData, title: e.target.value })}
                autoFocus
              />
            </div>
            <p className="text-xs text-gray-500">Full presentation editing is available in the preview. Configure the title here.</p>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={handleCancel}>Cancel</Button>
            <Button onClick={() => handleSave(editData ?? currentData)}>Save</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    )
  }

  if (blockType === "project") {
    return (
      <Dialog open={open} onOpenChange={(v) => { if (!v) handleCancel() }}>
        <DialogContent className="max-w-2xl">
          <DialogHeader><DialogTitle>Project Reference</DialogTitle></DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>Project ID</Label>
              <Input
                placeholder="Enter project ID to embed"
                value={(editData ?? currentData).projectId || ""}
                onChange={(e) => handleDataChange({ ...currentData, ...editData, projectId: e.target.value })}
                autoFocus
              />
            </div>
            <div>
              <Label>Project Name</Label>
              <Input
                placeholder="Display name"
                value={(editData ?? currentData).projectName || ""}
                onChange={(e) => handleDataChange({ ...currentData, ...editData, projectName: e.target.value })}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={handleCancel}>Cancel</Button>
            <Button onClick={() => handleSave(editData ?? currentData)}>Save</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    )
  }

  // Generic form-based editor for Pattern B types
  if (FormComponent) {
    const formData = editData ?? currentData
    return (
      <Dialog open={open} onOpenChange={(v) => { if (!v) handleCancel() }}>
        <DialogContent className="max-w-2xl max-h-[85vh] overflow-auto">
          <DialogHeader>
            <DialogTitle>{config.label} Editor</DialogTitle>
          </DialogHeader>
          <FormComponent data={formData} onChange={handleDataChange} />
          <DialogFooter>
            <Button variant="outline" onClick={handleCancel}>Cancel</Button>
            <Button onClick={() => handleSave(formData)}>Save</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    )
  }

  // Fallback
  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) handleCancel() }}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{config.label}</DialogTitle>
        </DialogHeader>
        <p className="text-sm text-gray-500">Editor not available for this block type.</p>
        <DialogFooter>
          <Button variant="outline" onClick={handleCancel}>Close</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
