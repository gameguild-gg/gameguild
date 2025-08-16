"use client"

import type React from "react"

import { useRef, useState } from "react"
import { AlertCircle, Upload, X, Trash2, Plus, Send } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Label } from "@/components/ui/label"
import { Alert, AlertDescription } from "@/components/ui/alert"

export interface MediaUploadResult {
  type: "file" | "url"
  data: string // Either a data URL or a web URL
  name?: string // Original filename if available
  size?: number // File size in bytes if available
}

interface MediaUploadDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onMediaSelected: (result: MediaUploadResult | MediaUploadResult[]) => void
  title?: string
  mode?: 0 | 1 | 2 // 0 = both upload and URL, 1 = only upload, 2 = only URL
  acceptTypes?: string // e.g. "image/*" or "image/png,image/jpeg"
  urlPlaceholder?: string
  uploadLabel?: string
  urlLabel?: string
  maxSizeKB?: number // Maximum file size in KB
  multiple?: boolean // Allow multiple file selection
}

export function MediaUploadDialog({
  open,
  onOpenChange,
  onMediaSelected,
  title = "Upload Media",
  mode = 0,
  acceptTypes = "image/*",
  urlPlaceholder = "https://example.com/image.jpg",
  uploadLabel = "Select a file from your device",
  urlLabel = "Enter the URL of the media",
  maxSizeKB,
  multiple = false,
}: MediaUploadDialogProps) {
  const [mediaUrl, setMediaUrl] = useState("")
  const [activeTab, setActiveTab] = useState<string>(mode === 2 ? "url" : "upload")
  const [error, setError] = useState<string | null>(null)
  const [pendingUploads, setPendingUploads] = useState<MediaUploadResult[]>([])
  const fileInputRef = useRef<HTMLInputElement>(null)

  const validateFileSize = (file: File): boolean => {
    if (!maxSizeKB) return true

    const fileSizeKB = file.size / 1024
    if (fileSizeKB > maxSizeKB) {
      setError(`File size exceeds the maximum limit of ${maxSizeKB} KB. Your file is ${Math.round(fileSizeKB)} KB.`)
      return false
    }

    setError(null)
    return true
  }

  const handleFileUpload = (files: FileList) => {
    if (files.length === 0) return

    const newUploads: MediaUploadResult[] = []
    let hasError = false

    Array.from(files).forEach((file) => {
      if (!validateFileSize(file)) {
        hasError = true
        return
      }

      const reader = new FileReader()
      reader.onload = () => {
        if (typeof reader.result === "string") {
          const newUpload: MediaUploadResult = {
            type: "file",
            data: reader.result,
            name: file.name,
            size: file.size,
          }
          newUploads.push(newUpload)

          if (newUploads.length === files.length && !hasError) {
            setPendingUploads((prev) => [...prev, ...newUploads])
            setError(null)
          }
        }
      }
      reader.onerror = () => {
        setError("Failed to read one or more files. Please try again.")
        hasError = true
      }
      reader.readAsDataURL(file)
    })
  }

  const handleUrlAdd = () => {
    if (mediaUrl.trim()) {
      const newUpload: MediaUploadResult = {
        type: "url",
        data: mediaUrl,
        name: `URL: ${mediaUrl.substring(0, 30)}${mediaUrl.length > 30 ? "..." : ""}`,
      }
      setPendingUploads((prev) => [...prev, newUpload])
      setMediaUrl("")
      setError(null)
    } else {
      setError("Please enter a valid URL")
    }
  }

  const removeFromStaging = (index: number) => {
    setPendingUploads((prev) => prev.filter((_, i) => i !== index))
  }

  const handleSubmitAll = () => {
    if (pendingUploads.length === 0) {
      setError("Please add at least one file or URL")
      return
    }

    if (multiple) {
      onMediaSelected(pendingUploads)
    } else {
      onMediaSelected(pendingUploads[0])
    }

    setPendingUploads([])
    onOpenChange(false)
    setError(null)
  }

  const formatFileSize = (bytes?: number) => {
    if (!bytes) return ""
    const kb = bytes / 1024
    if (kb < 1024) return `${Math.round(kb)} KB`
    return `${(kb / 1024).toFixed(1)} MB`
  }

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    e.stopPropagation()
  }

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    e.stopPropagation()

    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      handleFileUpload(e.dataTransfer.files)
    }
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      setError(null)
      setPendingUploads([])
      setMediaUrl("")
    }
    onOpenChange(newOpen)
  }

  const handleTabChange = (value: string) => {
    setActiveTab(value)
    setError(null)
  }

  const formatMaxSize = () => {
    if (!maxSizeKB) return ""

    if (maxSizeKB >= 1024) {
      const maxSizeMB = (maxSizeKB / 1024).toFixed(1)
      return ` (Max: ${maxSizeMB} MB)`
    }

    return ` (Max: ${maxSizeKB} KB)`
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-4xl max-h-[90vh] overflow-hidden">
        <DialogHeader className="pb-4 border-b">
          <div className="flex items-center justify-between">
            <DialogTitle className="text-xl font-semibold">{title}</DialogTitle>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onOpenChange(false)}
              className="h-8 w-8 p-0 hover:bg-gray-100"
            >
              <X className="h-4 w-4" />
              <span className="sr-only">Close</span>
            </Button>
          </div>
        </DialogHeader>

        <div className="py-6 flex gap-6 h-[70vh]">
          <div className="flex-1">
            {error && (
              <Alert variant="destructive" className="mb-6">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            {mode === 0 ? (
              <Tabs defaultValue={activeTab} value={activeTab} onValueChange={handleTabChange} className="w-full">
                <TabsList className="grid w-full grid-cols-2 mb-6">
                  <TabsTrigger value="upload" className="flex items-center gap-2">
                    <Upload className="h-4 w-4" />
                    Upload Files
                  </TabsTrigger>
                  <TabsTrigger value="url" className="flex items-center gap-2">
                    <span className="text-sm">🔗</span>
                    From URL
                  </TabsTrigger>
                </TabsList>

                <TabsContent value="upload" className="mt-0">
                  <div className="space-y-4">
                    <div className="text-center">
                      <Label className="text-base font-medium">
                        {uploadLabel}
                        {formatMaxSize() && (
                          <span className="text-sm text-muted-foreground ml-1">{formatMaxSize()}</span>
                        )}
                      </Label>
                    </div>

                    <div
                      className="relative flex flex-col items-center justify-center gap-6 border-2 border-dashed border-gray-300 rounded-xl p-12 transition-colors hover:border-gray-400 hover:bg-gray-50/50"
                      onDragOver={handleDragOver}
                      onDrop={handleDrop}
                    >
                      <div className="flex flex-col items-center gap-4">
                        <div className="p-4 bg-blue-50 rounded-full">
                          <Upload className="h-8 w-8 text-blue-600" />
                        </div>
                        <div className="text-center space-y-2">
                          <p className="text-lg font-medium text-gray-900">Drop your files here</p>
                          <p className="text-sm text-muted-foreground">or click to browse from your device</p>
                        </div>
                      </div>

                      <Button
                        variant="outline"
                        size="lg"
                        onClick={() => fileInputRef.current?.click()}
                        className="mt-2"
                      >
                        <Plus className="h-4 w-4 mr-2" />
                        Add Files
                      </Button>

                      <input
                        ref={fileInputRef}
                        type="file"
                        id="media-upload"
                        accept={acceptTypes}
                        multiple={true}
                        className="hidden"
                        onChange={(e) => {
                          if (e.target.files && e.target.files.length > 0) {
                            handleFileUpload(e.target.files)
                          }
                        }}
                      />
                    </div>
                  </div>
                </TabsContent>

                <TabsContent value="url" className="mt-0">
                  <div className="space-y-6">
                    <div className="text-center">
                      <Label className="text-base font-medium">{urlLabel}</Label>
                    </div>

                    <div className="space-y-4">
                      <Input
                        id="media-url"
                        placeholder={urlPlaceholder}
                        value={mediaUrl}
                        onChange={(e) => setMediaUrl(e.target.value)}
                        className="h-12 text-base"
                        onKeyDown={(e) => {
                          if (e.key === "Enter") {
                            handleUrlAdd()
                          }
                        }}
                      />
                      <Button onClick={handleUrlAdd} className="w-full h-12 text-base" disabled={!mediaUrl.trim()}>
                        <Plus className="h-4 w-4 mr-2" />
                        Add URL
                      </Button>
                    </div>
                  </div>
                </TabsContent>
              </Tabs>
            ) : mode === 1 ? (
              <div className="space-y-4">
                <div className="text-center">
                  <Label className="text-base font-medium">
                    {uploadLabel}
                    {formatMaxSize() && <span className="text-sm text-muted-foreground ml-1">{formatMaxSize()}</span>}
                  </Label>
                </div>

                <div
                  className="relative flex flex-col items-center justify-center gap-6 border-2 border-dashed border-gray-300 rounded-xl p-12 transition-colors hover:border-gray-400 hover:bg-gray-50/50"
                  onDragOver={handleDragOver}
                  onDrop={handleDrop}
                >
                  <div className="flex flex-col items-center gap-4">
                    <div className="p-4 bg-blue-50 rounded-full">
                      <Upload className="h-8 w-8 text-blue-600" />
                    </div>
                    <div className="text-center space-y-2">
                      <p className="text-lg font-medium text-gray-900">Drop your files here</p>
                      <p className="text-sm text-muted-foreground">or click to browse from your device</p>
                    </div>
                  </div>

                  <Button variant="outline" size="lg" onClick={() => fileInputRef.current?.click()} className="mt-2">
                    <Plus className="h-4 w-4 mr-2" />
                    Add Files
                  </Button>

                  <input
                    ref={fileInputRef}
                    type="file"
                    id="media-upload"
                    accept={acceptTypes}
                    multiple={true}
                    className="hidden"
                    onChange={(e) => {
                      if (e.target.files && e.target.files.length > 0) {
                        handleFileUpload(e.target.files)
                      }
                    }}
                  />
                </div>
              </div>
            ) : (
              <div className="space-y-6">
                <div className="text-center">
                  <Label className="text-base font-medium">{urlLabel}</Label>
                </div>

                <div className="space-y-4">
                  <Input
                    id="media-url"
                    placeholder={urlPlaceholder}
                    value={mediaUrl}
                    onChange={(e) => setMediaUrl(e.target.value)}
                    className="h-12 text-base"
                    onKeyDown={(e) => {
                      if (e.key === "Enter") {
                        handleUrlAdd()
                      }
                    }}
                  />
                  <Button onClick={handleUrlAdd} className="w-full h-12 text-base" disabled={!mediaUrl.trim()}>
                    <Plus className="h-4 w-4 mr-2" />
                    Add URL
                  </Button>
                </div>
              </div>
            )}
          </div>

          <div className="w-80 border-l pl-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold">Review Files</h3>
              <span className="text-sm text-muted-foreground">
                {pendingUploads.length} item{pendingUploads.length !== 1 ? "s" : ""}
              </span>
            </div>

            <div className="space-y-3 max-h-[50vh] overflow-y-auto">
              {pendingUploads.length === 0 ? (
                <div className="text-center py-8 text-muted-foreground">
                  <Upload className="h-8 w-8 mx-auto mb-2 opacity-50" />
                  <p className="text-sm">No files added yet</p>
                  <p className="text-xs">Add files to review them here</p>
                </div>
              ) : (
                pendingUploads.map((upload, index) => (
                  <div key={index} className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium truncate" title={upload.name}>
                        {upload.name || "Unnamed file"}
                      </p>
                      <div className="flex items-center gap-2 text-xs text-muted-foreground">
                        <span
                          className={`px-2 py-1 rounded text-xs ${
                            upload.type === "file" ? "bg-blue-100 text-blue-700" : "bg-green-100 text-green-700"
                          }`}
                        >
                          {upload.type === "file" ? "File" : "URL"}
                        </span>
                        {upload.size && <span>{formatFileSize(upload.size)}</span>}
                      </div>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => removeFromStaging(index)}
                      className="h-8 w-8 p-0 hover:bg-red-100 hover:text-red-600"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                ))
              )}
            </div>

            <div className="mt-6 pt-4 border-t">
              <Button
                onClick={handleSubmitAll}
                className="w-full h-12 text-base"
                disabled={pendingUploads.length === 0}
              >
                <Send className="h-4 w-4 mr-2" />
                Send{" "}
                {pendingUploads.length > 0
                  ? `${pendingUploads.length} item${pendingUploads.length !== 1 ? "s" : ""}`
                  : "Files"}
              </Button>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
