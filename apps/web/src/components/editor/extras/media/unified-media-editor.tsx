"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { X, Save, Image, Video, Music, FileText, Eye } from "lucide-react"
import { MediaPreview } from "./media-preview"
import { ImageSizeControl } from "@/components/editor/extras/image-size-control"
import type { BaseMediaData } from "@/components/editor/nodes/base/media-node-base"

interface UnifiedMediaEditorProps {
  data: BaseMediaData
  onChange: (data: Partial<BaseMediaData>) => void
  onClose?: () => void
  onSave?: () => void
}

// Helper functions to detect URL type
function detectVideoEmbedType(url: string): "youtube" | "vimeo" | "dailymotion" | "direct" {
  if (!url) return "direct"
  
  // YouTube
  if (/(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i.test(url)) {
    return "youtube"
  }
  
  // Vimeo
  if (/(?:vimeo\.com\/(?:video\/)?|player\.vimeo\.com\/video\/)([0-9]+)/i.test(url)) {
    return "vimeo"
  }
  
  // Dailymotion
  if (/(?:dailymotion\.com\/(?:video\/|embed\/video\/)|dai\.ly\/)([a-zA-Z0-9]+)/i.test(url)) {
    return "dailymotion"
  }
  
  return "direct"
}

function detectAudioEmbedType(url: string): "youtube" | "spotify" | "soundcloud" | "direct" {
  if (!url) return "direct"
  
  // YouTube
  if (/(?:youtube\.com\/(?:[^/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^"&?/\s]{11})/i.test(url)) {
    return "youtube"
  }
  
  // Spotify
  if (/(?:spotify\.com\/track\/|spotify:track:)([a-zA-Z0-9]+)/i.test(url)) {
    return "spotify"
  }
  
  // SoundCloud
  if (/soundcloud\.com\/([^/]+\/[^/]+)/i.test(url)) {
    return "soundcloud"
  }
  
  return "direct"
}

function detectVideoFileType(url: string): string {
  if (!url) return "video/mp4"
  
  const extension = url.split('.').pop()?.toLowerCase()
  switch (extension) {
    case "webm":
      return "video/webm"
    case "ogg":
    case "ogv":
      return "video/ogg"
    case "mp4":
    default:
      return "video/mp4"
  }
}

function detectAudioFileType(url: string): string {
  if (!url) return "audio/mpeg"
  
  const extension = url.split('.').pop()?.toLowerCase()
  switch (extension) {
    case "wav":
      return "audio/wav"
    case "ogg":
    case "oga":
      return "audio/ogg"
    case "mp3":
    default:
      return "audio/mpeg"
  }
}

export function UnifiedMediaEditor({ data, onChange, onClose, onSave }: UnifiedMediaEditorProps) {
  const [localData, setLocalData] = useState<BaseMediaData>(data)
  const [urlDetectionEnabled, setUrlDetectionEnabled] = useState(true)

  // Block body scroll and pointer events when modal is open
  useEffect(() => {
    // Store original values
    const originalOverflow = document.body.style.overflow
    const originalPointerEvents = document.body.style.pointerEvents
    
    // Disable scroll and pointer events on body
    document.body.style.overflow = 'hidden'
    document.body.style.pointerEvents = 'none'
    
    // Cleanup on unmount
    return () => {
      document.body.style.overflow = originalOverflow
      document.body.style.pointerEvents = originalPointerEvents
    }
  }, [])

  // Auto-detect embed type when URL changes
  useEffect(() => {
    if (!urlDetectionEnabled || !localData.src) return
    
    if (localData.type === "video") {
      const detectedType = detectVideoEmbedType(localData.src)
      
      if (detectedType !== localData.embedType) {
        const updates: Partial<BaseMediaData> = { embedType: detectedType }
        
        // If it's a direct file, also detect the video format
        if (detectedType === "direct") {
          updates.videoType = detectVideoFileType(localData.src)
        }
        
        const newData = { ...localData, ...updates }
        setLocalData(newData)
        // Don't call onChange here, it will be called on save
      }
    } else if (localData.type === "audio") {
      const detectedType = detectAudioEmbedType(localData.src)
      
      if (detectedType !== localData.embedAudioType) {
        const updates: Partial<BaseMediaData> = { embedAudioType: detectedType }
        
        // If it's a direct file, also detect the audio format
        if (detectedType === "direct") {
          updates.audioType = detectAudioFileType(localData.src)
        }
        
        const newData = { ...localData, ...updates }
        setLocalData(newData)
        // Don't call onChange here, it will be called on save
      }
    }
  }, [localData.src, localData.type, urlDetectionEnabled, localData])

  const handleChange = (field: keyof BaseMediaData, value: any) => {
    const newData = { ...localData, [field]: value }
    setLocalData(newData)
    onChange({ [field]: value })
    
    // Disable auto-detection temporarily when user manually changes embed type
    if (field === "embedType" || field === "embedAudioType") {
      setUrlDetectionEnabled(false)
      // Re-enable after a delay
      setTimeout(() => setUrlDetectionEnabled(true), 2000)
    }
  }
  
  const handleUrlChange = (newUrl: string) => {
    setUrlDetectionEnabled(true) // Re-enable detection on URL change
    handleChange("src", newUrl)
  }

  const handleSave = () => {
    // Ensure all localData is synchronized before closing
    onChange(localData)
    
    // Restore body styles before closing
    document.body.style.overflow = ''
    document.body.style.pointerEvents = ''
    
    if (onSave) {
      onSave()
    } else if (onClose) {
      onClose()
    }
  }

  const handleClose = () => {
    // Restore body styles before closing
    document.body.style.overflow = ''
    document.body.style.pointerEvents = ''
    
    if (onClose) {
      onClose()
    }
  }

  const getMediaIcon = () => {
    switch (localData.type) {
      case "image":
        return <Image className="h-5 w-5 text-blue-600 dark:text-blue-400" />
      case "video":
        return <Video className="h-5 w-5 text-blue-600 dark:text-blue-400" />
      case "audio":
        return <Music className="h-5 w-5 text-blue-600 dark:text-blue-400" />
    }
  }

  const getMediaTitle = () => {
    switch (localData.type) {
      case "image":
        return "Image Editor"
      case "video":
        return "Video Editor"
      case "audio":
        return "Audio Editor"
    }
  }

  const renderMediaSpecificOptions = () => {
    switch (localData.type) {
      case "video":
        return (
          <>
                        <div className="flex items-center gap-2">
              <Label className="text-sm font-medium text-gray-700 dark:text-gray-300 min-w-[80px]">
                Source:
              </Label>
              <div className="flex-1">
                <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 rounded-md text-sm font-medium border border-blue-200 dark:border-blue-800">
                  {localData.embedType === "direct" && "📁 Direct File"}
                  {localData.embedType === "youtube" && "▶️ YouTube"}
                  {localData.embedType === "vimeo" && "🎬 Vimeo"}
                  {localData.embedType === "dailymotion" && "📺 Dailymotion"}
                  {!localData.embedType && "📁 Direct File"}
                </span>
              </div>
            </div>
            
            {localData.embedType === "direct" && (
              <div className="flex items-center gap-2">
                <Label className="text-sm font-medium text-gray-700 dark:text-gray-300 min-w-[80px]">
                  Format:
                </Label>
                <div className="flex-1 px-3 py-2 bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-700 dark:text-gray-300">
                  {localData.videoType === "video/mp4" && "📹 MP4 (H.264)"}
                  {localData.videoType === "video/webm" && "📹 WebM (VP8/VP9)"}
                  {localData.videoType === "video/ogg" && "📹 Ogg (Theora)"}
                  {!localData.videoType && "📹 MP4 (H.264)"}
                </div>
                <span className="text-xs text-gray-500 dark:text-gray-400" title="Detected from file extension">
                  �
                </span>
              </div>
            )}
          </>
        )
      
      case "audio":
        return (
          <>
            <div className="flex items-center gap-2">
              <Label className="text-sm font-medium text-gray-700 dark:text-gray-300 min-w-[80px]">
                Source:
              </Label>
              <div className="flex-1">
                <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 rounded-md text-sm font-medium border border-blue-200 dark:border-blue-800">
                  {localData.embedAudioType === "direct" && "📁 Direct File"}
                  {localData.embedAudioType === "youtube" && "▶️ YouTube"}
                  {localData.embedAudioType === "spotify" && "🎵 Spotify"}
                  {localData.embedAudioType === "soundcloud" && "☁️ SoundCloud"}
                  {!localData.embedAudioType && "📁 Direct File"}
                </span>
              </div>
            </div>
            
            {localData.embedAudioType === "direct" && (
              <div className="flex items-center gap-2">
                <Label className="text-sm font-medium text-gray-700 dark:text-gray-300 min-w-[80px]">
                  Format:
                </Label>
                <div className="flex-1 px-3 py-2 bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-700 dark:text-gray-300">
                  {localData.audioType === "audio/mpeg" && "🎵 MP3 (MPEG)"}
                  {localData.audioType === "audio/wav" && "🎵 WAV (Lossless)"}
                  {localData.audioType === "audio/ogg" && "🎵 Ogg (Vorbis)"}
                  {!localData.audioType && "🎵 MP3 (MPEG)"}
                </div>
                <span className="text-xs text-gray-500 dark:text-gray-400" title="Detected from file extension">
                  �
                </span>
              </div>
            )}
          </>
        )
      
      case "image":
      default:
        return (
          <div className="flex items-center gap-2">
            <Label htmlFor="alt" className="text-sm font-medium text-gray-700 dark:text-gray-300 min-w-[80px]">
              Alt Text:
            </Label>
            <Input
              id="alt"
              value={localData.alt || ""}
              onChange={(e) => handleChange("alt", e.target.value)}
              placeholder="Image description"
              className="flex-1 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600"
            />
          </div>
        )
    }
  }

  return (
    <div 
      className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
      style={{ pointerEvents: 'auto' }}
      onClick={handleClose}
      onMouseDown={(e) => e.stopPropagation()}
      onKeyDown={(e) => {
        if (e.key === 'Escape') {
          handleClose()
        }
        // Prevent all keyboard events from propagating to the editor
        e.stopPropagation()
      }}
    >
      <div 
        className="bg-white dark:bg-gray-900 border dark:border-gray-700 rounded-lg shadow-2xl w-full max-w-7xl h-[90vh] flex flex-col"
        style={{ pointerEvents: 'auto' }}
        onClick={(e) => e.stopPropagation()}
        onKeyDown={(e) => e.stopPropagation()}
        onKeyUp={(e) => e.stopPropagation()}
        onKeyPress={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            {getMediaIcon()}
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">{getMediaTitle()}</h2>
            
            {/* Media Type Display */}
            <div className="ml-4 flex items-center gap-3 pl-4 border-l border-gray-300 dark:border-gray-600">
              <span className="text-sm text-gray-600 dark:text-gray-400 bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">
                Type: <span className="font-medium text-gray-800 dark:text-gray-200 capitalize">{localData.type}</span>
              </span>
              {localData.embedType && localData.embedType !== "direct" && (
                <span className="text-sm text-gray-600 dark:text-gray-400 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded flex items-center gap-1">
                  <span className="font-medium text-blue-600 dark:text-blue-400 capitalize">{localData.embedType}</span>
                  {urlDetectionEnabled && <span className="text-xs">🔍</span>}
                </span>
              )}
              {localData.embedAudioType && localData.embedAudioType !== "direct" && (
                <span className="text-sm text-gray-600 dark:text-gray-400 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded flex items-center gap-1">
                  <span className="font-medium text-blue-600 dark:text-blue-400 capitalize">{localData.embedAudioType}</span>
                  {urlDetectionEnabled && <span className="text-xs">🔍</span>}
                </span>
              )}
              {localData.embedType === "direct" && localData.type === "video" && localData.videoType && (
                <span className="text-sm text-gray-600 dark:text-gray-400 bg-green-50 dark:bg-green-900/30 px-2 py-1 rounded">
                  <span className="font-medium text-green-600 dark:text-green-400">{localData.videoType}</span>
                </span>
              )}
              {localData.embedAudioType === "direct" && localData.type === "audio" && localData.audioType && (
                <span className="text-sm text-gray-600 dark:text-gray-400 bg-green-50 dark:bg-green-900/30 px-2 py-1 rounded">
                  <span className="font-medium text-green-600 dark:text-green-400">{localData.audioType}</span>
                </span>
              )}
            </div>
          </div>
          <Button variant="ghost" size="sm" onClick={handleClose} className="hover:bg-gray-100 dark:hover:bg-gray-800">
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Main Content */}
        <div className="flex-1 flex min-h-0">
          {/* Left Panel - Configuration */}
          <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex flex-col bg-white dark:bg-gray-900">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                <FileText className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                Configuration
              </h3>
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              {/* URL Field */}
              <div className="space-y-2">
                <Label htmlFor="src" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Media URL:
                </Label>
                <Input
                  id="src"
                  value={localData.src}
                  onChange={(e) => handleUrlChange(e.target.value)}
                  placeholder={
                    localData.type === "video" 
                      ? "https://youtube.com/... or https://example.com/video.mp4"
                      : localData.type === "audio"
                      ? "https://spotify.com/... or https://example.com/audio.mp3"
                      : "https://example.com/image.jpg"
                  }
                  className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                />
                {(localData.type === "video" || localData.type === "audio") && (
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    💡 Auto-detects YouTube, Vimeo, Spotify, SoundCloud and file URLs
                  </p>
                )}
              </div>

              {/* Media Specific Options - Only show if URL is provided */}
              {localData.src && (
                <div className="space-y-3 pt-2">
                  {renderMediaSpecificOptions()}
                </div>
              )}

              {/* Divider */}
              <div className="border-t border-gray-200 dark:border-gray-700 my-4"></div>

              {/* Size Control */}
              <div className="space-y-2">
                <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Size: {localData.size}%
                </Label>
                <ImageSizeControl
                  size={localData.size || 100}
                  onChange={(value) => handleChange("size", value)}
                />
              </div>

              {/* Caption */}
              <div className="space-y-2">
                <Label htmlFor="caption" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Caption:
                </Label>
                <Textarea
                  id="caption"
                  value={localData.caption || ""}
                  onChange={(e) => handleChange("caption", e.target.value)}
                  placeholder="Add a caption (optional)"
                  rows={3}
                  className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                />
              </div>
            </div>
          </div>

          {/* Right Panel - Preview */}
          <div className="w-1/2 flex flex-col bg-white dark:bg-gray-900">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                <Eye className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                Live Preview
              </h3>
            </div>
            <div className="flex-1 overflow-auto bg-gray-50 dark:bg-gray-950">
              <MediaPreview data={localData} />
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center justify-end gap-2">
            <Button
              variant="outline"
              onClick={handleClose}
              className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
            >
              Cancel
            </Button>
            <Button
              onClick={handleSave}
              className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600"
            >
              <Save className="h-4 w-4" />
              Save Media
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
