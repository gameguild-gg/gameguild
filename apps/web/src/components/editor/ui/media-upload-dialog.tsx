"use client"

import { useRef, useState } from "react"
import { AlertCircle, Upload, X, Trash2, Plus, Send, Settings, Zap, ImageIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Label } from "@/components/ui/label"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Switch } from "@/components/ui/switch"
import { CompressionSettingsDialog, type CompressionSettings } from "@/components/editor/ui/compression-settings-dialog"
import { WebPConverter } from "@/lib/editor/webp-converter"

export interface MediaUploadResult {
  type: "file" | "url"
  data: string // Either a data URL or a web URL
  name?: string // Original filename if available
  size?: number // File size in bytes if available
  compressed?: boolean // Whether the file was compressed
  originalSize?: number // Original size before compression
  compressionRatio?: number // Compression ratio if compressed
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
  compress?: boolean // Enable compression by default
  allowCompressionToggle?: boolean // Allow users to toggle compression
}

interface PendingUpload extends MediaUploadResult {
  id: string
  file?: File // Original file for compression
  needsCompression?: boolean
  isCompressing?: boolean
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
  multiple = true,
  compress = true,
  allowCompressionToggle = false,
}: MediaUploadDialogProps) {
  const [mediaUrl, setMediaUrl] = useState("")
  const [activeTab, setActiveTab] = useState<string>(mode === 2 ? "url" : "upload")
  const [error, setError] = useState<string | null>(null)
  const [pendingUploads, setPendingUploads] = useState<PendingUpload[]>([])
  const [compressionEnabled, setCompressionEnabled] = useState(compress)
  const [compressionSettingsOpen, setCompressionSettingsOpen] = useState(false)
  const [currentCompressionFile, setCurrentCompressionFile] = useState<File | null>(null)
  const [globalCompressionSettings, setGlobalCompressionSettings] = useState<CompressionSettings | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const isImageFile = (file: File): boolean => {
    return file.type.startsWith("image/")
  }

  const shouldRecommendCompression = (file: File): boolean => {
    return isImageFile(file) && WebPConverter.shouldCompress(file)
  }

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

  const handleFileUpload = async (files: FileList) => {
    if (files.length === 0) return

    const newUploads: PendingUpload[] = []
    let hasError = false

    for (const file of Array.from(files)) {
      if (!validateFileSize(file)) {
        hasError = true
        continue
      }

      const id = `${Date.now()}-${Math.random()}`
      const isImage = isImageFile(file)
      const needsCompression = compressionEnabled && isImage && shouldRecommendCompression(file)

      try {
        let finalData: string
        let finalSize = file.size
        let compressed = false
        const originalSize = file.size
        let compressionRatio = 0

        // Auto-compress if enabled and recommended
        if (compressionEnabled && isImage && !needsCompression) {
          const result = await WebPConverter.convertToWebP(file, {
            quality: WebPConverter.getOptimalQuality(file.size),
            maxWidth: 1920,
            maxHeight: 1080,
            maintainAspectRatio: true,
          })

          if (result.success && result.dataUrl) {
            finalData = result.dataUrl
            finalSize = result.compressedSize
            compressed = true
            compressionRatio = result.compressionRatio
          } else {
            // Fallback to original if compression fails
            finalData = await new Promise<string>((resolve, reject) => {
              const reader = new FileReader()
              reader.onload = () => resolve(reader.result as string)
              reader.onerror = reject
              reader.readAsDataURL(file)
            })
          }
        } else {
          // Use original file
          finalData = await new Promise<string>((resolve, reject) => {
            const reader = new FileReader()
            reader.onload = () => resolve(reader.result as string)
            reader.onerror = reject
            reader.readAsDataURL(file)
          })
        }

        const newUpload: PendingUpload = {
          id,
          type: "file",
          data: finalData,
          name: file.name,
          size: finalSize,
          file,
          needsCompression,
          compressed,
          originalSize,
          compressionRatio,
        }

        newUploads.push(newUpload)
      } catch (error) {
        console.error("Error processing file:", error)
        hasError = true
      }
    }

    if (!hasError && newUploads.length > 0) {
      setPendingUploads((prev) => [...prev, ...newUploads])
      setError(null)
    }
  }

  const handleCompressionSettings = (file: File) => {
    setCurrentCompressionFile(file)
    setCompressionSettingsOpen(true)
  }

  const handleCompressionConfirm = async (settings: CompressionSettings) => {
    if (!currentCompressionFile) return

    const fileId = pendingUploads.find((upload) => upload.file === currentCompressionFile)?.id
    if (!fileId) return

    // Set upload as compressing
    setPendingUploads((prev) =>
      prev.map((upload) => (upload.id === fileId ? { ...upload, isCompressing: true } : upload)),
    )

    try {
      const result = await WebPConverter.convertToWebP(currentCompressionFile, settings)

      if (result.success && result.dataUrl) {
        setPendingUploads((prev) =>
          prev.map((upload) =>
            upload.id === fileId
              ? {
                  ...upload,
                  data: result.dataUrl!,
                  size: result.compressedSize,
                  compressed: true,
                  compressionRatio: result.compressionRatio,
                  needsCompression: false,
                  isCompressing: false,
                }
              : upload,
          ),
        )

        // Apply to all if requested
        if (settings.applyToAll) {
          setGlobalCompressionSettings(settings)
          // Process remaining files with same settings
          const remainingFiles = pendingUploads.filter(
            (upload) => upload.needsCompression && upload.id !== fileId && upload.file,
          )

          for (const upload of remainingFiles) {
            if (upload.file) {
              const batchResult = await WebPConverter.convertToWebP(upload.file, settings)
              if (batchResult.success && batchResult.dataUrl) {
                setPendingUploads((prev) =>
                  prev.map((u) =>
                    u.id === upload.id
                      ? {
                          ...u,
                          data: batchResult.dataUrl!,
                          size: batchResult.compressedSize,
                          compressed: true,
                          compressionRatio: batchResult.compressionRatio,
                          needsCompression: false,
                        }
                      : u,
                  ),
                )
              }
            }
          }
        }
      }
    } catch (error) {
      console.error("Compression failed:", error)
      setPendingUploads((prev) =>
        prev.map((upload) => (upload.id === fileId ? { ...upload, isCompressing: false } : upload)),
      )
    }

    setCurrentCompressionFile(null)
  }

  const handleUrlAdd = () => {
    if (mediaUrl.trim()) {
      const newUpload: PendingUpload = {
        id: `${Date.now()}-${Math.random()}`,
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

  const removeFromStaging = (id: string) => {
    setPendingUploads((prev) => prev.filter((upload) => upload.id !== id))
  }

  const handleSubmitAll = () => {
    if (pendingUploads.length === 0) {
      setError("Please add at least one file or URL")
      return
    }

    // Check if any files still need compression
    const needsCompression = pendingUploads.some((upload) => upload.needsCompression)
    if (needsCompression) {
      setError("Some files still need compression settings. Please configure compression for all files.")
      return
    }

    const results: MediaUploadResult[] = pendingUploads.map((upload) => ({
      type: upload.type,
      data: upload.data,
      name: upload.name,
      size: upload.size,
      compressed: upload.compressed,
      originalSize: upload.originalSize,
      compressionRatio: upload.compressionRatio,
    }))

    if (multiple) {
      onMediaSelected(results)
    } else {
      onMediaSelected(results[0])
    }

    setPendingUploads([])
    onOpenChange(false)
    setError(null)
  }

  const formatFileSize = (bytes?: number) => {
    if (!bytes) return ""
    return WebPConverter.formatFileSize(bytes)
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      setError(null)
      setPendingUploads([])
      setMediaUrl("")
      setGlobalCompressionSettings(null)
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
    <>
      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent className="sm:max-w-4xl max-h-[90vh] overflow-hidden">
          <DialogHeader className="pb-4 border-b">
            <div className="flex items-center justify-between">
              <DialogTitle className="text-xl font-semibold">{title}</DialogTitle>
              <div className="flex items-center gap-3">
                {allowCompressionToggle && (
                  <div className="flex items-center gap-2">
                    <Label htmlFor="compression-toggle" className="text-sm">
                      Auto Compress
                    </Label>
                    <Switch
                      id="compression-toggle"
                      checked={compressionEnabled}
                      onCheckedChange={setCompressionEnabled}
                    />
                  </div>
                )}
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

              {compressionEnabled && (
                <Alert className="mb-6">
                  <Zap className="h-4 w-4" />
                  <AlertDescription>
                    Image compression is enabled. Large images will be automatically optimized to WebP format for better
                    performance.
                  </AlertDescription>
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
                        onDragOver={(e) => {
                          e.preventDefault()
                          e.stopPropagation()
                        }}
                        onDrop={(e) => {
                          e.preventDefault()
                          e.stopPropagation()
                          if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
                            handleFileUpload(e.dataTransfer.files)
                          }
                        }}
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
                    onDragOver={(e) => {
                      e.preventDefault()
                      e.stopPropagation()
                    }}
                    onDrop={(e) => {
                      e.preventDefault()
                      e.stopPropagation()
                      if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
                        handleFileUpload(e.dataTransfer.files)
                      }
                    }}
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
                  pendingUploads.map((upload) => (
                    <div key={upload.id} className="p-3 bg-gray-50 rounded-lg space-y-2">
                      <div className="flex items-start gap-3">
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
                          onClick={() => removeFromStaging(upload.id)}
                          className="h-8 w-8 p-0 hover:bg-red-100 hover:text-red-600"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>

                      {upload.type === "file" && isImageFile(upload.file!) && (
                        <div className="space-y-2">
                          {upload.compressed && (
                            <div className="flex items-center gap-2">
                              <Badge variant="secondary" className="text-xs">
                                <Zap className="h-3 w-3 mr-1" />
                                Compressed -{Math.round(upload.compressionRatio || 0)}%
                              </Badge>
                              <span className="text-xs text-muted-foreground">
                                {formatFileSize(upload.originalSize)} → {formatFileSize(upload.size)}
                              </span>
                            </div>
                          )}

                          {upload.needsCompression && (
                            <div className="space-y-2">
                              <Badge variant="outline" className="text-xs">
                                <ImageIcon className="h-3 w-3 mr-1" />
                                Compression Recommended
                              </Badge>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => upload.file && handleCompressionSettings(upload.file)}
                                className="w-full h-7 text-xs"
                                disabled={upload.isCompressing}
                              >
                                {upload.isCompressing ? (
                                  <>
                                    <div className="animate-spin rounded-full h-3 w-3 border-b border-current mr-1" />
                                    Compressing...
                                  </>
                                ) : (
                                  <>
                                    <Settings className="h-3 w-3 mr-1" />
                                    Configure Compression
                                  </>
                                )}
                              </Button>
                            </div>
                          )}
                        </div>
                      )}
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

      {currentCompressionFile && (
        <CompressionSettingsDialog
          isOpen={compressionSettingsOpen}
          onClose={() => setCompressionSettingsOpen(false)}
          file={currentCompressionFile}
          onConfirm={handleCompressionConfirm}
          onCancel={() => {
            setCompressionSettingsOpen(false)
            setCurrentCompressionFile(null)
          }}
          remainingCount={pendingUploads.filter((upload) => upload.needsCompression).length - 1}
        />
      )}
    </>
  )
}
