"use client"

import { useRef, useState, useEffect } from "react"
import { AlertCircle, X, Plus, Zap, HardDrive, Upload, Package } from "lucide-react"

import { CompressionSettingsDialog, type CompressionSettings } from "@/components/block-content-editor/extras/compressor/compression-settings-dialog"
import { LocalAssetGrid } from "./media-upload-dialog/local-asset-grid"
import { ReviewPanel } from "./media-upload-dialog/review-panel"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { WebPConverter } from "@/components/block-content-editor/lib/editor/webp-converter"
import { assetManager } from "@/components/block-content-editor/lib/storage/assets/asset-manager"
import type { CollectionMetadata } from "@/components/block-content-editor/lib/storage/assets/collection-types"

export interface MediaUploadResult {
  type: "file" | "url"
  data: string // Asset URL (asset://hash) or web URL
  name?: string // Original filename if available
  size?: number // File size in bytes if available
  compressed?: boolean // Whether the file was compressed
  originalSize?: number // Original size before compression
  compressionRatio?: number // Compression ratio if compressed
  assetId?: string // Asset ID (SHA1 hash) for file uploads
}

interface MediaSourceConfig {
  files?: boolean // Allow file upload and local asset selection
  url?: boolean // Allow URL input
  collections?: boolean // Allow collection selection
}

interface MediaUploadDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onMediaSelected: (result: MediaUploadResult | MediaUploadResult[]) => void
  title?: string
  sources?: MediaSourceConfig // Configure which sources are available (default: all enabled)
  acceptTypes?: string // e.g. "image/*" or "image/png,image/jpeg"
  urlPlaceholder?: string
  uploadLabel?: string
  urlLabel?: string
  maxSizeKB?: number // Maximum file size in KB
  multiple?: boolean // Allow multiple file selection
  compress?: boolean // Enable compression by default
  allowCompressionToggle?: boolean // Allow users to toggle compression
  hideLocalAssets?: boolean // Hide local asset selection, show only upload button
  forceTextStorage?: boolean // Force storage as plain text instead of base64 (for code files)
  projectId?: string // Current project id for scoping asset saves
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
  sources = { files: true, url: true },
  acceptTypes = "image/*",
  urlPlaceholder = "https://example.com/image.jpg",
  uploadLabel = "Select a file from your device",
  urlLabel = "Enter the URL of the media",
  maxSizeKB,
  multiple = true,
  compress = true,
  allowCompressionToggle = false,
  hideLocalAssets = false,
  forceTextStorage = false,
  projectId,
}: MediaUploadDialogProps) {
  
  const enabledSources = {
    files: sources.files === true,
    url: sources.url === true,
    collections: sources.collections === true,
  }
  
  const getDefaultTab = () => {
    if (enabledSources.collections) return "collections"
    if (enabledSources.files) return "files"
    if (enabledSources.url) return "url"
    return "files"
  }
  
  const [mediaUrl, setMediaUrl] = useState("")
  const [activeTab, setActiveTab] = useState<string>(getDefaultTab())
  const [localAssets, setLocalAssets] = useState<Array<{ id: string; name: string; type: string; size: number; dataUrl: string }>>([])
  const [selectedLocalAssets, setSelectedLocalAssets] = useState<Set<string>>(new Set())
  const [isLoadingAssets, setIsLoadingAssets] = useState(false)
  const [searchQuery, setSearchQuery] = useState("")
  const [error, setError] = useState<string | null>(null)
  const [pendingUploads, setPendingUploads] = useState<PendingUpload[]>([])
  const [compressionEnabled, setCompressionEnabled] = useState(compress)
  const [compressionSettingsOpen, setCompressionSettingsOpen] = useState(false)
  const [currentCompressionFile, setCurrentCompressionFile] = useState<File | null>(null)
  const [globalCompressionSettings, setGlobalCompressionSettings] = useState<CompressionSettings | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  
  // Collections state
  const [collections, setCollections] = useState<CollectionMetadata[]>([])
  const [selectedCollection, setSelectedCollection] = useState<string | null>(null)
  const [isLoadingCollections, setIsLoadingCollections] = useState(false)

  // Debug: Log projectId when dialog opens or projectId changes
  useEffect(() => {
    if (open) {
      console.log("MediaUploadDialog: projectId =", projectId)
    }
  }, [open, projectId])

  // Load all assets when dialog opens
  useEffect(() => {
    async function loadLocalAssets() {
      if (open && enabledSources.files && !hideLocalAssets) {
        setIsLoadingAssets(true)
        try {
          // Get ALL assets, not just from current project
          const assetMetadataList = await assetManager.listAssets()
          
          const allAssets = await Promise.all(
            assetMetadataList.map(async (metadata) => {
              try {
                const assetData = await assetManager.getAsset(metadata.id)
                if (assetData && assetData.data) {
                  return {
                    id: metadata.id,
                    name: metadata.name || metadata.id,
                    type: metadata.mimeType,
                    size: metadata.size || 0,
                    dataUrl: assetData.data,
                  }
                }
                return null
              } catch (error) {
                console.error(`Failed to load asset ${metadata.id}:`, error)
                return null
              }
            })
          )
          
          setLocalAssets(allAssets.filter((asset): asset is NonNullable<typeof asset> => asset !== null))
        } catch (error) {
          console.error('Failed to load local assets:', error)
        } finally {
          setIsLoadingAssets(false)
        }
      }
    }
    loadLocalAssets()
  }, [open, enabledSources.files, hideLocalAssets])

  // Load collections when dialog opens
  useEffect(() => {
    async function loadCollections() {
      if (open && enabledSources.collections) {
        setIsLoadingCollections(true)
        try {
          const list = await assetManager.listCollections()
          setCollections(list)
        } catch (error) {
          console.error('Failed to load collections:', error)
        } finally {
          setIsLoadingCollections(false)
        }
      }
    }
    loadCollections()
  }, [open, enabledSources.collections])

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
    const fileToCompress = currentCompressionFile
    if (!fileToCompress) return

    const fileId = pendingUploads.find((upload) => upload.file === fileToCompress)?.id
    if (!fileId) return

    // Clear compression state synchronously to prevent race conditions / stale states
    setCurrentCompressionFile(null)
    setCompressionSettingsOpen(false)

    // Set upload as compressing
    setPendingUploads((prev) =>
      prev.map((upload) => (upload.id === fileId ? { ...upload, isCompressing: true } : upload)),
    )

    try {
      const result = await WebPConverter.convertToWebP(fileToCompress, settings)

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

  const toggleLocalAssetSelection = (assetId: string) => {
    setSelectedLocalAssets(prev => {
      const newSet = new Set(prev)
      if (newSet.has(assetId)) {
        newSet.delete(assetId)
      } else {
        newSet.add(assetId)
      }
      return newSet
    })
  }

  const handleAddSelectedAssets = () => {
    const newUploads: PendingUpload[] = localAssets
      .filter(asset => selectedLocalAssets.has(asset.id))
      .map(asset => ({
        id: `local-${asset.id}-${Date.now()}`,
        type: "file" as const,
        data: asset.dataUrl,
        name: asset.name,
        size: asset.size,
        assetId: asset.id,
      }))
    
    setPendingUploads(prev => [...prev, ...newUploads])
    setSelectedLocalAssets(new Set())
    setError(null)
  }

  const removeFromStaging = (id: string) => {
    setPendingUploads((prev) => prev.filter((upload) => upload.id !== id))
  }

  const handleSubmitAll = async () => {
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

    try {
      setError(null)
      const results: MediaUploadResult[] = []

      console.log("MediaUploadDialog: handleSubmitAll - projectId =", projectId)

      // Process files one by one for safety
      for (const upload of pendingUploads) {
        // Check if it's a collection URL
        if (upload.data.startsWith('collection://')) {
          // Collection - pass through directly without saving as asset
          results.push({
            type: "file",
            data: upload.data, // collection://id
            name: upload.name,
            size: upload.size,
          })
          continue
        }
        
        if (upload.type === "file") {
          // Save file to assets
          console.log("MediaUploadDialog: Saving file asset with projectId =", projectId)
          
          // If file was compressed, create a new File object with the compressed data
          // but keep the original filename
          let fileToSave = upload.file
          if (upload.compressed && upload.data && upload.name) {
            // Convert dataUrl back to File with original name
            const blob = await fetch(upload.data).then(r => r.blob())
            fileToSave = new File([blob], upload.name, { type: blob.type })
          }
          
          const saveResult = await assetManager.saveAsset({
            file: fileToSave,
            dataUrl: upload.data,
            author: "user", // TODO: Get from auth context
            license: "user-uploaded",
            projectId,
            nodeId: `temp-${Date.now()}`, // Temporary ID, will be updated when node is created
            forceTextStorage, // Use text storage for code files
          })

          if (saveResult.success && saveResult.assetUrl) {
            results.push({
              type: "file",
              data: saveResult.assetUrl, // Asset URL: asset://hash
              name: upload.name,
              size: upload.size,
              compressed: upload.compressed,
              originalSize: upload.originalSize,
              compressionRatio: upload.compressionRatio,
              assetId: saveResult.assetId,
            })
          } else {
            setError(`Failed to save asset: ${saveResult.error || "Unknown error"}`)
            return
          }
        } else {
          // URL type - save URL reference to assets
          const saveResult = await assetManager.saveAsset({
            urlSource: upload.data,
            author: "user",
            license: "external-url",
            projectId,
            nodeId: `temp-${Date.now()}`,
          })

          if (saveResult.success && saveResult.assetUrl) {
            results.push({
              type: "url",
              data: saveResult.assetUrl, // Asset URL: asset://hash
              name: upload.name,
              assetId: saveResult.assetId,
            })
          } else {
            // Fallback to direct URL if asset save fails
            results.push({
              type: "url",
              data: upload.data,
              name: upload.name,
            })
          }
        }
      }

      if (multiple) {
        onMediaSelected(results)
      } else {
        const firstResult = results[0]
        if (firstResult) {
          onMediaSelected(firstResult)
        }
      }

      setPendingUploads([])
      onOpenChange(false)
      setError(null)
    } catch (error) {
      console.error("Error saving assets:", error)
      setCollections([])
      setSelectedCollection(null)
      setError(`Failed to save assets: ${error instanceof Error ? error.message : "Unknown error"}`)
    }
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
      setSelectedLocalAssets(new Set())
      setLocalAssets([])
      setSearchQuery("")
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

  const filteredAssets = localAssets.filter(asset => 
    asset.name.toLowerCase().includes(searchQuery.toLowerCase())
  )

  return (
    <>
      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent
          className="sm:max-w-6xl h-[85vh] overflow-hidden flex flex-col"
          onPointerDownOutside={(e) => {
            e.preventDefault()
          }}
          onInteractOutside={(e) => {
            e.preventDefault()
          }}
        >
          <DialogHeader className="pb-2 border-b shrink-0">
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
                  className="h-8 w-8 p-0 hover:bg-gray-100 dark:hover:bg-gray-800"
                >
                  <X className="h-4 w-4" />
                  <span className="sr-only">Close</span>
                </Button>
              </div>
            </div>
          </DialogHeader>

          <div className="py-3 flex gap-6 flex-1 min-h-0 overflow-hidden">
            <div className="flex-1 flex flex-col min-h-0">
              {error && (
                <Alert variant="destructive" className="mb-3">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              )}

              {compressionEnabled && (
                <Alert className="mb-3">
                  <Zap className="h-4 w-4" />
                  <AlertDescription>
                    Image compression is enabled. Large images will be automatically optimized to WebP format for better
                    performance.
                  </AlertDescription>
                </Alert>
              )}

              {Object.values(enabledSources).filter(Boolean).length > 1 ? (
                <Tabs defaultValue={activeTab} value={activeTab} onValueChange={handleTabChange} className="w-full flex flex-col flex-1 min-h-0">
                  <TabsList className={`grid w-full ${enabledSources.collections && enabledSources.files && enabledSources.url ? 'grid-cols-3' : enabledSources.collections || (enabledSources.files && enabledSources.url) ? 'grid-cols-2' : 'grid-cols-1'} mb-3`}>
                    {enabledSources.collections && (
                      <TabsTrigger value="collections" className="flex items-center gap-2">
                        <Package className="h-4 w-4" />
                        Collections
                      </TabsTrigger>
                    )}
                    {enabledSources.files && (
                      <TabsTrigger value="files" className="flex items-center gap-2">
                        <HardDrive className="h-4 w-4" />
                        Files
                      </TabsTrigger>
                    )}
                    {enabledSources.url && (
                      <TabsTrigger value="url" className="flex items-center gap-2">
                        <span className="text-sm">🔗</span>
                        URL
                      </TabsTrigger>
                    )}
                  </TabsList>

                  {enabledSources.collections && (
                    <TabsContent value="collections" className="mt-0 flex flex-col flex-1 min-h-0">
                      <div className="flex flex-col flex-1 min-h-0 gap-4">
                        <div className="space-y-2 shrink-0">
                          <Label className="text-sm font-medium">Select a collection to import</Label>
                          <Input
                            placeholder="Search collections..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="h-9"
                          />
                        </div>

                        <div className="border-2 border-gray-200 dark:border-gray-700 rounded-xl p-3 flex-1 overflow-y-auto min-h-0 dark:bg-gray-900/50">
                          {isLoadingCollections ? (
                            <div className="flex items-center justify-center h-full">
                              <div className="text-center">
                                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-2"></div>
                                <p className="text-sm text-gray-500">Loading collections...</p>
                              </div>
                            </div>
                          ) : collections.length === 0 ? (
                            <div className="flex items-center justify-center h-full">
                              <div className="text-center space-y-2">
                                <Package className="h-12 w-12 text-gray-400 mx-auto" />
                                <p className="text-sm text-gray-500">No collections found</p>
                                <p className="text-xs text-gray-400">Create collections in the Code Studio</p>
                              </div>
                            </div>
                          ) : (
                            <div className="grid grid-cols-1 gap-2">
                              {collections
                                .filter(collection => 
                                  !searchQuery || 
                                  collection.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                                  (collection.description && collection.description.toLowerCase().includes(searchQuery.toLowerCase()))
                                )
                                .map(collection => (
                                  <button
                                    key={collection.id}
                                    onClick={() => setSelectedCollection(collection.id)}
                                    className={`p-4 rounded-lg border-2 text-left transition-all hover:border-blue-500 hover:bg-blue-50/50 dark:hover:bg-blue-950/20 ${
                                      selectedCollection === collection.id
                                        ? 'border-blue-500 bg-blue-50 dark:bg-blue-950/30'
                                        : 'border-gray-200 dark:border-gray-700'
                                    }`}
                                  >
                                    <div className="flex items-start gap-3">
                                      <Package className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5 shrink-0" />
                                      <div className="flex-1 min-w-0">
                                        <h4 className="font-medium text-sm mb-1 truncate">{collection.name}</h4>
                                        {collection.description && (
                                          <p className="text-xs text-gray-600 dark:text-gray-400 line-clamp-2 mb-2">
                                            {collection.description}
                                          </p>
                                        )}
                                        <div className="flex items-center gap-2 text-xs text-gray-500">
                                          <span>ID: {collection.id.substring(0, 8)}...</span>
                                          <span>•</span>
                                          <span>{new Date(collection.created).toLocaleDateString()}</span>
                                        </div>
                                      </div>
                                    </div>
                                  </button>
                                ))}
                            </div>
                          )}
                        </div>

                        {selectedCollection && (
                          <div className="pt-2 shrink-0">
                            <Button
                              onClick={async () => {
                                try {
                                  const manifest = await assetManager.getCollection(selectedCollection)
                                  if (manifest) {
                                    // Add collection as a pending upload with special type
                                    const newUpload: PendingUpload = {
                                      id: `collection-${selectedCollection}-${Date.now()}`,
                                      type: "file" as const,
                                      data: `collection://${selectedCollection}`,
                                      name: manifest.metadata.name,
                                      size: 0,
                                    }
                                    setPendingUploads(prev => [...prev, newUpload])
                                    setSelectedCollection(null)
                                    setError(null)
                                  }
                                } catch (error) {
                                  console.error('Failed to load collection:', error)
                                  setError('Failed to load collection')
                                }
                              }}
                              className="w-full h-10 text-sm"
                            >
                              <Plus className="h-4 w-4 mr-2" />
                              Add Selected Collection
                            </Button>
                          </div>
                        )}
                      </div>
                    </TabsContent>
                  )}

                  {enabledSources.files && (
                    <TabsContent value="files" className="mt-0 flex flex-col flex-1 min-h-0">
                      {hideLocalAssets ? (
                        // Upload-only mode - prominent upload area
                        <div className="flex flex-col items-center justify-center flex-1 min-h-0">
                          <div
                            className="relative flex flex-col items-center justify-center gap-6 border-2 border-dashed border-blue-300 dark:border-blue-700 rounded-xl p-16 transition-colors hover:border-blue-400 dark:hover:border-blue-600 hover:bg-blue-50/50 dark:hover:bg-blue-950/20 w-full max-w-2xl"
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
                              <div className="p-6 bg-blue-100 dark:bg-blue-900/50 rounded-full">
                                <Upload className="h-12 w-12 text-blue-600 dark:text-blue-400" />
                              </div>
                              <div className="text-center space-y-2">
                                <p className="text-xl font-semibold text-gray-900 dark:text-gray-100">Drop your files here</p>
                                <p className="text-base text-gray-600 dark:text-gray-400">or click the button below to browse</p>
                              </div>
                            </div>

                            <Button 
                              size="lg" 
                              onClick={() => fileInputRef.current?.click()} 
                              className="mt-4 h-12 px-8 text-base bg-blue-600 hover:bg-blue-700"
                            >
                              <Plus className="h-5 w-5 mr-2" />
                              Select Files to Upload
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
                        // Full mode with local assets
                        <>
                          <div className="space-y-2 shrink-0">
                            <div className="flex items-center justify-between">
                              <Label className="text-sm font-medium">Select from your files</Label>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => fileInputRef.current?.click()}
                                className="h-9"
                              >
                                <Plus className="h-4 w-4 mr-2" />
                                Upload New
                              </Button>
                            </div>

                            <Input
                              placeholder="Search by name..."
                              value={searchQuery}
                              onChange={(e) => setSearchQuery(e.target.value)}
                              className="h-9"
                            />

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

                          <div className="border-2 border-gray-200 dark:border-gray-700 rounded-xl p-3 flex-1 overflow-y-auto min-h-0 mt-2 dark:bg-gray-900/50">
                            <LocalAssetGrid
                              assets={filteredAssets}
                              selectedAssets={selectedLocalAssets}
                              isLoading={isLoadingAssets}
                              hasNoAssets={filteredAssets.length === 0 && localAssets.length === 0}
                              noSearchResults={filteredAssets.length === 0 && localAssets.length > 0}
                              onToggleSelection={toggleLocalAssetSelection}
                              formatFileSize={formatFileSize}
                            />
                          </div>

                          {selectedLocalAssets.size > 0 && (
                            <div className="pt-2 shrink-0">
                              <Button
                                onClick={handleAddSelectedAssets}
                                className="w-full h-10 text-sm"
                              >
                                <Plus className="h-4 w-4 mr-2" />
                                Add {selectedLocalAssets.size} Selected File{selectedLocalAssets.size !== 1 ? 's' : ''}
                              </Button>
                            </div>
                          )}
                        </>
                      )}
                    </TabsContent>
                  )}

                  {enabledSources.url && (
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
                  )}
                </Tabs>
              ) : enabledSources.collections ? (
                <div className="flex flex-col flex-1 min-h-0 gap-4">
                  <div className="space-y-2 shrink-0">
                    <Label className="text-sm font-medium">Select a collection to import</Label>
                    <Input
                      placeholder="Search collections..."
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      className="h-9"
                    />
                  </div>

                  <div className="border-2 border-gray-200 dark:border-gray-700 rounded-xl p-3 flex-1 overflow-y-auto min-h-0 dark:bg-gray-900/50">
                    {isLoadingCollections ? (
                      <div className="flex items-center justify-center h-full">
                        <div className="text-center">
                          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-2"></div>
                          <p className="text-sm text-gray-500">Loading collections...</p>
                        </div>
                      </div>
                    ) : collections.length === 0 ? (
                      <div className="flex items-center justify-center h-full">
                        <div className="text-center space-y-2">
                          <Package className="h-12 w-12 text-gray-400 mx-auto" />
                          <p className="text-sm text-gray-500">No collections found</p>
                          <p className="text-xs text-gray-400">Create collections in the Code Studio</p>
                        </div>
                      </div>
                    ) : (
                      <div className="grid grid-cols-1 gap-2">
                        {collections
                          .filter(collection => 
                            !searchQuery || 
                            collection.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                            (collection.description && collection.description.toLowerCase().includes(searchQuery.toLowerCase()))
                          )
                          .map(collection => (
                            <button
                              key={collection.id}
                              onClick={() => setSelectedCollection(collection.id)}
                              className={`p-4 rounded-lg border-2 text-left transition-all hover:border-blue-500 hover:bg-blue-50/50 dark:hover:bg-blue-950/20 ${
                                selectedCollection === collection.id
                                  ? 'border-blue-500 bg-blue-50 dark:bg-blue-950/30'
                                  : 'border-gray-200 dark:border-gray-700'
                              }`}
                            >
                              <div className="flex items-start gap-3">
                                <Package className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5 shrink-0" />
                                <div className="flex-1 min-w-0">
                                  <h4 className="font-medium text-sm mb-1 truncate">{collection.name}</h4>
                                  {collection.description && (
                                    <p className="text-xs text-gray-600 dark:text-gray-400 line-clamp-2 mb-2">
                                      {collection.description}
                                    </p>
                                  )}
                                  <div className="flex items-center gap-2 text-xs text-gray-500">
                                    <span>ID: {collection.id.substring(0, 8)}...</span>
                                    <span>•</span>
                                    <span>{new Date(collection.created).toLocaleDateString()}</span>
                                  </div>
                                </div>
                              </div>
                            </button>
                          ))}
                      </div>
                    )}
                  </div>

                  {selectedCollection && (
                    <div className="pt-2 shrink-0">
                      <Button
                        onClick={async () => {
                          try {
                            const manifest = await assetManager.getCollection(selectedCollection)
                            if (manifest) {
                              const newUpload: PendingUpload = {
                                id: `collection-${selectedCollection}-${Date.now()}`,
                                type: "file" as const,
                                data: `collection://${selectedCollection}`,
                                name: manifest.metadata.name,
                                size: 0,
                              }
                              setPendingUploads(prev => [...prev, newUpload])
                              setSelectedCollection(null)
                              setError(null)
                            }
                          } catch (error) {
                            console.error('Failed to load collection:', error)
                            setError('Failed to load collection')
                          }
                        }}
                        className="w-full h-10 text-sm"
                      >
                        <Plus className="h-4 w-4 mr-2" />
                        Add Selected Collection
                      </Button>
                    </div>
                  )}
                </div>
              ) : enabledSources.files ? (
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

            <ReviewPanel
              pendingUploads={pendingUploads}
              onRemove={removeFromStaging}
              onCompressionSettings={handleCompressionSettings}
              onSubmit={handleSubmitAll}
              formatFileSize={formatFileSize}
              isImageFile={isImageFile}
            />
          </div>
        </DialogContent>
      </Dialog>

      {currentCompressionFile && (
        <CompressionSettingsDialog
          isOpen={compressionSettingsOpen}
          onClose={() => {
            setCompressionSettingsOpen(false)
            setCurrentCompressionFile(null)
          }}
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
