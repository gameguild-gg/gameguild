"use client"

import type React from "react"

import { useRef, useState } from "react"
import { AlertCircle, Upload, X } from "lucide-react"

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

    if (multiple) {
      const results: MediaUploadResult[] = []
      let hasError = false

      Array.from(files).forEach((file) => {
        if (!validateFileSize(file)) {
          hasError = true
          return
        }

        const reader = new FileReader()
        reader.onload = () => {
          if (typeof reader.result === "string") {
            results.push({
              type: "file",
              data: reader.result,
              name: file.name,
              size: file.size,
            })

            // If we've processed all files and no errors, return results
            if (results.length === files.length && !hasError) {
              onMediaSelected(results)
              onOpenChange(false)
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
    } else {
      // Single file upload (original behavior)
      const file = files[0]
      if (file) {
        if (!validateFileSize(file)) return

        const reader = new FileReader()
        reader.onload = () => {
          if (typeof reader.result === "string") {
            onMediaSelected({
              type: "file",
              data: reader.result,
              name: file.name,
              size: file.size,
            })
            onOpenChange(false)
            setError(null)
          }
        }
        reader.onerror = () => {
          setError("Failed to read the file. Please try again.")
        }
        reader.readAsDataURL(file)
      }
    }
  }

  const handleUrlSubmit = () => {
    if (mediaUrl.trim()) {
      onMediaSelected({
        type: "url",
        data: mediaUrl,
      })
      onOpenChange(false)
      setMediaUrl("")
      setError(null)
    } else {
      setError("Please enter a valid URL")
    }
  }

  // Handle drag and drop
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

  // Reset error when dialog closes or tab changes
  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      setError(null)
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
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-hidden">
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

        <div className="py-6">
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
                        <p className="text-lg font-medium text-gray-900">
                          {multiple ? "Drop your files here" : "Drop your file here"}
                        </p>
                        <p className="text-sm text-muted-foreground">or click to browse from your device</p>
                      </div>
                    </div>

                    <Button variant="outline" size="lg" onClick={() => fileInputRef.current?.click()} className="mt-2">
                      {multiple ? "Choose Files" : "Choose File"}
                    </Button>

                    <input
                      ref={fileInputRef}
                      type="file"
                      id="media-upload"
                      accept={acceptTypes}
                      multiple={multiple}
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
                    />
                    <Button onClick={handleUrlSubmit} className="w-full h-12 text-base" disabled={!mediaUrl.trim()}>
                      Insert from URL
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
                    <p className="text-lg font-medium text-gray-900">
                      {multiple ? "Drop your files here" : "Drop your file here"}
                    </p>
                    <p className="text-sm text-muted-foreground">or click to browse from your device</p>
                  </div>
                </div>

                <Button variant="outline" size="lg" onClick={() => fileInputRef.current?.click()} className="mt-2">
                  {multiple ? "Choose Files" : "Choose File"}
                </Button>

                <input
                  ref={fileInputRef}
                  type="file"
                  id="media-upload"
                  accept={acceptTypes}
                  multiple={multiple}
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
                />
                <Button onClick={handleUrlSubmit} className="w-full h-12 text-base" disabled={!mediaUrl.trim()}>
                  Insert from URL
                </Button>
              </div>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
