"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $getSelection, $isRangeSelection, SELECTION_CHANGE_COMMAND, createCommand } from "lexical"
import {
  AlertCircle,
  BookMarkedIcon as MarkdownIcon,
  Bookmark,
  BookOpen,
  BoxIcon as ButtonIcon,
  ClipboardList,
  Code2,
  CodeIcon,
  CodepenIcon,
  Eye,
  FileIcon,
  Heading,
  ImageIcon,
  Images,
  LayoutTemplateIcon as LayoutPresentationIcon,
  Mail,
  MoreHorizontal,
  MousePointerClick,
  Music,
  Music2,
  Music3,
  Plus,
  SeparatorHorizontal,
  ShoppingBag,
  ToggleLeft,
  Twitter,
  UserPlus,
  Video,
  Youtube,
} from "lucide-react"

import { Button } from "@/components/editor/ui/button"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/editor/ui/popover"
import { Separator } from "@/components/editor/ui/separator"
import { MediaUploadDialog, type MediaUploadResult } from "@/components/editor/ui/media-upload-dialog"
import type { ImageData } from "../nodes/image-node"
import type { VideoData } from "../nodes/video-node"
import type { AudioData } from "../nodes/audio-node" // Import type for AudioData
import type { HeaderData } from "../nodes/header-node"
import type { DividerData } from "../nodes/divider-node"
import type { ButtonData } from "../nodes/button-node"
import type { CalloutData } from "../nodes/callout-node"
import type { YouTubeData } from "../nodes/youtube-node"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/editor/ui/dialog"
import { Label } from "@/components/editor/ui/label"
import { Input } from "@/components/editor/ui/input"
import { Checkbox } from "@/components/editor/ui/checkbox"
import { extractYouTubeVideoId } from "../nodes/youtube-node"
import type { SpotifyData } from "../nodes/spotify-node"
import { extractSpotifyInfo } from "../nodes/spotify-node"

// Image insertion mode: 0 = both upload and URL, 1 = only upload, 2 = only URL
const IMAGE_INSERTION_MODE = 0

// Maximum image size in KB (e.g., 5120 = 5MB)
const MAX_IMAGE_SIZE_KB = 5120

// Video insertion mode: 0 = both upload and URL, 1 = only upload, 2 = only URL
const VIDEO_INSERTION_MODE = 0

// Maximum video size in KB (e.g., 51200 = 50MB)
const MAX_VIDEO_SIZE_KB = 51200

// Maximum audio size in KB (e.g., 10240 = 10MB)
const MAX_AUDIO_SIZE_KB = 10240

// Audio insertion mode: 0 = both upload and URL, 1 = only upload, 2 = only URL
const AUDIO_INSERTION_MODE = 0

export const INSERT_IMAGE_COMMAND = createCommand<ImageData>("INSERT_IMAGE_COMMAND")
export const INSERT_VIDEO_COMMAND = createCommand<VideoData>("INSERT_VIDEO_COMMAND")
export const INSERT_QUIZ_COMMAND = createCommand("INSERT_QUIZ_COMMAND")
export const INSERT_MARKDOWN_COMMAND = createCommand("INSERT_MARKDOWN_COMMAND")
export const INSERT_HTML_COMMAND = createCommand("INSERT_HTML_COMMAND")
export const SHOW_ACTIONS_COMMAND = createCommand("SHOW_ACTIONS_COMMAND")
export const INSERT_AUDIO_COMMAND = createCommand<AudioData>("INSERT_AUDIO_COMMAND")
export const INSERT_HEADER_COMMAND = createCommand<HeaderData>("INSERT_HEADER_COMMAND")
export const INSERT_DIVIDER_COMMAND = createCommand<Partial<DividerData>>("INSERT_DIVIDER_COMMAND")
export const INSERT_BUTTON_COMMAND = createCommand<Partial<ButtonData>>("INSERT_BUTTON_COMMAND")
export const INSERT_CALLOUT_COMMAND = createCommand<Partial<CalloutData>>("INSERT_CALLOUT_COMMAND")
export const INSERT_GALLERY_COMMAND = createCommand("INSERT_GALLERY_COMMAND")
export const INSERT_PRESENTATION_COMMAND = createCommand("INSERT_PRESENTATION_COMMAND")
export const INSERT_SOURCE_COMMAND = createCommand("INSERT_SOURCE_COMMAND")
export const INSERT_YOUTUBE_COMMAND = createCommand<YouTubeData>("INSERT_YOUTUBE_COMMAND")
export const INSERT_SPOTIFY_COMMAND = createCommand<SpotifyData>("INSERT_SPOTIFY_COMMAND")
export const INSERT_SOURCE_CODE_COMMAND = createCommand("INSERT_SOURCE_CODE_COMMAND")

export function FloatingContentInsertPlugin() {
  const [editor] = useLexicalComposerContext()
  const [show, setShow] = useState(false)
  const [showMenu, setShowMenu] = useState(false)
  const [position, setPosition] = useState({ x: 0, y: 0 })
  const [showImageDialog, setShowImageDialog] = useState(false)
  const [showVideoDialog, setShowVideoDialog] = useState(false)
  const [showAudioDialog, setShowAudioDialog] = useState(false)
  const [showYouTubeDialog, setShowYouTubeDialog] = useState(false)
  const [youtubeUrl, setYoutubeUrl] = useState("")
  const [youtubeStartAt, setYoutubeStartAt] = useState("")
  const [youtubeShowControls, setYoutubeShowControls] = useState(true)
  const [youtubeShowRelated, setYoutubeShowRelated] = useState(false)
  const [youtubeShowInfo, setYoutubeShowInfo] = useState(true)
  const [youtubeError, setYoutubeError] = useState("")
  const [showSpotifyDialog, setShowSpotifyDialog] = useState(false)
  const [spotifyUrl, setSpotifyUrl] = useState("")
  const [spotifyShowTheme, setSpotifyShowTheme] = useState(true)
  const [spotifyError, setSpotifyError] = useState("")

  const menuRef = useRef<HTMLDivElement>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)

  const updateMenuPosition = useCallback(() => {
    editor.getEditorState().read(() => {
      const selection = $getSelection()
      if (!selection || !$isRangeSelection(selection)) return

      const node = selection.anchor.getNode()
      const domElement = editor.getElementByKey(node.getKey())

      if (!domElement) return

      const editorElement = editor.getRootElement()
      if (!editorElement) return

      const domRect = domElement.getBoundingClientRect()
      const editorRect = editorElement.getBoundingClientRect()
      const lineHeight = Number.parseInt(window.getComputedStyle(domElement).lineHeight)

      setPosition({
        x: editorRect.left - 45,
        y: domRect.top + (lineHeight - 24) / 2,
      })
    })
  }, [editor])

  const checkEmpty = useCallback(() => {
    editor.getEditorState().read(() => {
      const selection = $getSelection()
      if (!selection || !$isRangeSelection(selection)) return

      const node = selection.anchor.getNode()
      const isTopLevel = node.getTopLevelElement() === node
      const isEmpty = node.getTextContent().trim() === ""
      const isAtStart = selection.anchor.offset === 0

      setShow(isEmpty && isTopLevel && isAtStart)
      if (isEmpty && isAtStart) {
        updateMenuPosition()
      }
    })
  }, [editor, updateMenuPosition])

  useEffect(() => {
    return editor.registerUpdateListener(({ editorState }) => {
      editorState.read(() => {
        checkEmpty()
      })
    })
  }, [editor, checkEmpty])

  useEffect(() => {
    return editor.registerCommand(
      SELECTION_CHANGE_COMMAND,
      () => {
        checkEmpty()
        return false
      },
      1,
    )
  }, [editor, checkEmpty])

  const handleImageSelected = (result: MediaUploadResult | MediaUploadResult[]) => {
    const results = Array.isArray(result) ? result : [result]
    results.forEach((item) => {
      editor.dispatchCommand(INSERT_IMAGE_COMMAND, {
        src: item.data,
        alt: item.name || "Image",
        caption: "",
        size: 100,
      })
    })
    setShowMenu(false)
  }

  const handleVideoSelected = (result: MediaUploadResult | MediaUploadResult[]) => {
    const results = Array.isArray(result) ? result : [result]
    results.forEach((item) => {
      editor.dispatchCommand(INSERT_VIDEO_COMMAND, {
        src: item.data,
        alt: item.name || "Video",
        caption: "",
        size: 100,
      })
    })
    setShowMenu(false)
  }

  const handleAudioSelected = (result: MediaUploadResult | MediaUploadResult[]) => {
    const results = Array.isArray(result) ? result : [result]
    results.forEach((item) => {
      editor.dispatchCommand(INSERT_AUDIO_COMMAND, {
        src: item.data,
        title: item.name || "Audio",
        caption: "",
        size: 100,
      })
    })
    setShowMenu(false)
  }

  const handleImageOption = () => {
    setShowImageDialog(true)
  }

  const handleVideoOption = () => {
    setShowVideoDialog(true)
  }

  const handleAudioOption = () => {
    setShowAudioDialog(true)
  }

  const handleYouTubeOption = () => {
    setYoutubeUrl("")
    setYoutubeStartAt("")
    setYoutubeShowControls(true)
    setYoutubeShowRelated(false)
    setYoutubeShowInfo(true)
    setYoutubeError("")
    setShowYouTubeDialog(true)
  }

  const handleYouTubeSubmit = () => {
    // Validate YouTube URL
    const videoId = extractYouTubeVideoId(youtubeUrl)

    if (!videoId) {
      setYoutubeError("Invalid YouTube URL. Please enter a valid YouTube video URL.")
      return
    }

    // Parse start time
    let startAt: number | undefined = undefined
    if (youtubeStartAt) {
      // Try to parse as seconds
      const seconds = Number.parseInt(youtubeStartAt, 10)
      if (!isNaN(seconds) && seconds >= 0) {
        startAt = seconds
      } else {
        // Try to parse as MM:SS format
        const timeMatch = youtubeStartAt.match(/^(\d+):(\d+)$/)
        if (timeMatch) {
          const minutes = Number.parseInt(timeMatch[1], 10)
          const seconds = Number.parseInt(timeMatch[2], 10)
          if (!isNaN(minutes) && !isNaN(seconds) && seconds < 60) {
            startAt = minutes * 60 + seconds
          } else {
            setYoutubeError("Invalid start time format. Please use seconds or MM:SS format.")
            return
          }
        } else {
          setYoutubeError("Invalid start time format. Please use seconds or MM:SS format.")
          return
        }
      }
    }

    // Create YouTube node
    editor.dispatchCommand(INSERT_YOUTUBE_COMMAND, {
      videoId,
      title: "YouTube video",
      caption: "",
      size: 100,
      startAt,
      showControls: youtubeShowControls,
      showInfo: youtubeShowInfo,
      showRelated: youtubeShowRelated,
    })

    // Close dialog and menu
    setShowYouTubeDialog(false)
    setShowMenu(false)
  }

  const handleSpotifyOption = () => {
    setSpotifyUrl("")
    setSpotifyShowTheme(true)
    setSpotifyError("")
    setShowSpotifyDialog(true)
  }

  const handleSpotifySubmit = () => {
    // Validate Spotify URL
    const spotifyInfo = extractSpotifyInfo(spotifyUrl)

    if (!spotifyInfo) {
      setSpotifyError("Invalid Spotify URL. Please enter a valid Spotify URL for a track, album, playlist, or artist.")
      return
    }

    // Create Spotify node
    editor.dispatchCommand(INSERT_SPOTIFY_COMMAND, {
      ...spotifyInfo,
      title: `Spotify ${spotifyInfo.type}`,
      caption: "",
      size: 100,
      showTheme: spotifyShowTheme,
    })

    // Close dialog and menu
    setShowSpotifyDialog(false)
    setShowMenu(false)
  }

  const primaryOptions = [
    {
      icon: Heading,
      label: "Header",
      action: () => {
        editor.dispatchCommand(INSERT_HEADER_COMMAND, {
          text: "",
          level: 2,
          style: "default",
        })
        setShowMenu(false)
      },
    },
    {
      icon: ImageIcon,
      label: "Image",
      action: handleImageOption,
    },
    {
      icon: Images,
      label: "Gallery",
      action: () => {
        editor.dispatchCommand(INSERT_GALLERY_COMMAND, undefined)
        setShowMenu(false)
      },
    },
    {
      icon: Video,
      label: "Video",
      action: handleVideoOption,
    },
    {
      icon: Youtube,
      label: "YouTube",
      action: handleYouTubeOption,
    },
    {
      icon: Music,
      label: "Audio",
      action: handleAudioOption,
    },
    {
      icon: ClipboardList,
      label: "Quiz",
      action: () => {
        editor.dispatchCommand(INSERT_QUIZ_COMMAND, undefined)
        setShowMenu(false)
      },
    },
    {
      icon: MarkdownIcon,
      label: "Markdown",
      action: () => {
        editor.dispatchCommand(INSERT_MARKDOWN_COMMAND, undefined)
        setShowMenu(false)
      },
    },
    {
      icon: Code2,
      label: "HTML",
      action: () => {
        editor.dispatchCommand(INSERT_HTML_COMMAND, undefined)
        setShowMenu(false)
      },
    },
    {
      icon: SeparatorHorizontal,
      label: "Divider",
      action: () => {
        editor.dispatchCommand(INSERT_DIVIDER_COMMAND, { style: "simple", isNew: true })
        setShowMenu(false)
      },
    },
    {
      icon: ButtonIcon,
      label: "Button",
      action: () => {
        editor.dispatchCommand(INSERT_BUTTON_COMMAND, {})
        setShowMenu(false)
      },
    },
    {
      icon: AlertCircle,
      label: "Callout",
      action: () => {
        editor.dispatchCommand(INSERT_CALLOUT_COMMAND, {})
        setShowMenu(false)
      },
    },
    {
      icon: LayoutPresentationIcon,
      label: "Presentation",
      action: () => {
        editor.dispatchCommand(INSERT_PRESENTATION_COMMAND, undefined)
        setShowMenu(false)
      },
    },
    {
      icon: BookOpen,
      label: "Sources & References",
      action: () => {
        editor.dispatchCommand(INSERT_SOURCE_COMMAND, undefined)
        setShowMenu(false)
      },
    },
    {
      icon: CodeIcon,
      label: "Source Code",
      action: () => {
        editor.dispatchCommand(INSERT_SOURCE_CODE_COMMAND, undefined)
        setShowMenu(false)
      },
    },
    { icon: Bookmark, label: "Bookmark", action: () => console.log("Bookmark clicked") },
    { icon: Mail, label: "Email content", action: () => console.log("Email content clicked") },
    { icon: MousePointerClick, label: "Email call to action", action: () => console.log("Email CTA clicked") },
    { icon: Eye, label: "Public preview", action: () => console.log("Public preview clicked") },
    { icon: ToggleLeft, label: "Toggle", action: () => console.log("Toggle clicked") },
    { icon: FileIcon, label: "File", action: () => console.log("File clicked") },
    { icon: ShoppingBag, label: "Product", action: () => console.log("Product clicked") },
    { icon: UserPlus, label: "Signup", action: () => console.log("Signup clicked") },
  ]

  const embedOptions = [
    { icon: Youtube, label: "YouTube", action: handleYouTubeOption },
    { icon: Music2, label: "Spotify", action: handleSpotifyOption },
    { icon: Twitter, label: "X (formerly Twitter)", action: () => console.log("Twitter clicked") },
    { icon: ImageIcon, label: "Unsplash", action: () => console.log("Unsplash clicked") },
    { icon: Video, label: "Vimeo", action: () => console.log("Vimeo clicked") },
    { icon: CodepenIcon, label: "CodePen", action: () => console.log("CodePen clicked") },
    { icon: Music3, label: "SoundCloud", action: () => console.log("SoundCloud clicked") },
    { icon: MoreHorizontal, label: "Other...", action: () => console.log("Other clicked") },
  ]

  if (
    !show &&
    !showMenu &&
    !showImageDialog &&
    !showVideoDialog &&
    !showAudioDialog &&
    !showYouTubeDialog &&
    !showSpotifyDialog
  )
    return null

  return (
    <>
      <div
        ref={menuRef}
        style={{
          position: "fixed",
          left: `${position.x}px`,
          top: `${position.y}px`,
          zIndex: 100,
        }}
      >
        <Popover open={showMenu} onOpenChange={setShowMenu}>
          <PopoverTrigger asChild>
            <Button ref={buttonRef} variant="ghost" size="icon" className="h-10 w-10 rounded-full hover:bg-muted">
              <Plus className="h-6 w-6" />
            </Button>
          </PopoverTrigger>
          <PopoverContent
            side="left"
            className="w-80 p-0 max-h-[500px] overflow-y-auto shadow-lg border-0 bg-background/95 backdrop-blur-sm"
          >
            <div className="px-2 py-1.5">
              <h4 className="text-xs font-medium text-muted-foreground">PRIMARY</h4>
            </div>
            <div className="grid gap-1 p-2">
              {primaryOptions.slice(0, 8).map((option) => (
                <button
                  key={option.label}
                  onClick={() => {
                    option.action()
                  }}
                  className="flex w-full items-center gap-3 rounded-md px-3 py-2.5 text-sm hover:bg-accent hover:text-accent-foreground transition-colors duration-150 text-left group"
                >
                  <div className="flex h-8 w-8 items-center justify-center rounded-md bg-muted group-hover:bg-background transition-colors duration-150">
                    <option.icon className="h-4 w-4" />
                  </div>
                  <span className="font-medium">{option.label}</span>
                </button>
              ))}

              {primaryOptions.length > 8 && (
                <>
                  <Separator className="my-2" />
                  <div className="px-2 py-1">
                    <h4 className="text-xs font-medium text-muted-foreground">MORE OPTIONS</h4>
                  </div>
                  <div className="grid grid-cols-2 gap-1">
                    {primaryOptions.slice(8).map((option) => (
                      <button
                        key={option.label}
                        onClick={() => {
                          option.action()
                        }}
                        className="flex flex-col items-center gap-2 rounded-md px-2 py-3 text-xs hover:bg-accent hover:text-accent-foreground transition-colors duration-150 group"
                      >
                        <div className="flex h-6 w-6 items-center justify-center rounded-sm bg-muted group-hover:bg-background transition-colors duration-150">
                          <option.icon className="h-3 w-3" />
                        </div>
                        <span className="text-center leading-tight">{option.label}</span>
                      </button>
                    ))}
                  </div>
                </>
              )}
            </div>
            <Separator className="my-1" />
            <div className="px-2 py-1.5">
              <h4 className="text-xs font-medium text-muted-foreground">EMBEDS</h4>
            </div>
            <div className="grid grid-cols-2 gap-1 p-2">
              {embedOptions.map((option) => (
                <button
                  key={option.label}
                  onClick={() => {
                    option.action()
                    setShowMenu(false)
                  }}
                  className="flex flex-col items-center gap-2 rounded-md px-3 py-3 text-xs hover:bg-accent hover:text-accent-foreground transition-colors duration-150 group"
                >
                  <div className="flex h-7 w-7 items-center justify-center rounded-md bg-muted group-hover:bg-background transition-colors duration-150">
                    <option.icon className="h-4 w-4" />
                  </div>
                  <span className="text-center leading-tight font-medium">{option.label}</span>
                </button>
              ))}
            </div>
          </PopoverContent>
        </Popover>
      </div>

      <MediaUploadDialog
        open={showImageDialog}
        onOpenChange={setShowImageDialog}
        onMediaSelected={handleImageSelected}
        title="Insert Image"
        mode={IMAGE_INSERTION_MODE}
        acceptTypes="image/*"
        urlPlaceholder="https://example.com/image.jpg"
        uploadLabel="Select an image from your device"
        urlLabel="Enter the image URL"
        maxSizeKB={MAX_IMAGE_SIZE_KB}
      />

      <MediaUploadDialog
        open={showVideoDialog}
        onOpenChange={setShowVideoDialog}
        onMediaSelected={handleVideoSelected}
        title="Insert Video"
        mode={VIDEO_INSERTION_MODE}
        acceptTypes="video/*"
        urlPlaceholder="https://example.com/video.mp4"
        uploadLabel="Select a video from your device"
        urlLabel="Enter the video URL"
        maxSizeKB={MAX_VIDEO_SIZE_KB}
      />

      <MediaUploadDialog
        open={showAudioDialog}
        onOpenChange={setShowAudioDialog}
        onMediaSelected={handleAudioSelected}
        title="Insert Audio"
        mode={AUDIO_INSERTION_MODE}
        acceptTypes="audio/*"
        urlPlaceholder="https://example.com/audio.mp3"
        uploadLabel="Select an audio file from your device"
        urlLabel="Enter the audio URL or Spotify/SoundCloud link"
        maxSizeKB={MAX_AUDIO_SIZE_KB}
      />

      <Dialog open={showYouTubeDialog} onOpenChange={setShowYouTubeDialog}>
        <DialogContent className="sm:max-w-[500px] max-h-[90vh] overflow-hidden">
          <DialogHeader className="space-y-3 pb-6 border-b">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-red-50 rounded-lg">
                <Youtube className="h-5 w-5 text-red-600" />
              </div>
              <div>
                <DialogTitle className="text-xl font-semibold">Insert YouTube Video</DialogTitle>
                <DialogDescription className="text-sm text-muted-foreground mt-1">
                  Embed a YouTube video with customizable playback options
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          <div className="py-6 space-y-6 max-h-[60vh] overflow-y-auto">
            {/* URL Input Section */}
            <div className="space-y-3">
              <Label htmlFor="youtube-url" className="text-sm font-medium flex items-center gap-2">
                <span className="w-6 h-6 bg-blue-100 text-blue-700 rounded-full flex items-center justify-center text-xs font-bold">
                  1
                </span>
                YouTube URL
              </Label>
              <div className="relative">
                <Input
                  id="youtube-url"
                  placeholder="https://www.youtube.com/watch?v=dQw4w9WgXcQ"
                  value={youtubeUrl}
                  onChange={(e) => setYoutubeUrl(e.target.value)}
                  className={`pl-4 pr-4 py-3 text-sm ${youtubeError ? "border-red-300 focus:border-red-500" : ""}`}
                />
                {youtubeUrl && !youtubeError && (
                  <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                    <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                  </div>
                )}
              </div>
              <p className="text-xs text-muted-foreground">
                Paste any YouTube video URL (youtube.com, youtu.be, or m.youtube.com)
              </p>
            </div>

            {/* Start Time Section */}
            <div className="space-y-3">
              <Label htmlFor="start-at" className="text-sm font-medium flex items-center gap-2">
                <span className="w-6 h-6 bg-blue-100 text-blue-700 rounded-full flex items-center justify-center text-xs font-bold">
                  2
                </span>
                Start Time (Optional)
              </Label>
              <Input
                id="start-at"
                placeholder="0:30 or 30"
                value={youtubeStartAt}
                onChange={(e) => setYoutubeStartAt(e.target.value)}
                className="pl-4 pr-4 py-3 text-sm"
              />
              <p className="text-xs text-muted-foreground">Enter time in seconds (30) or MM:SS format (0:30)</p>
            </div>

            {/* Playback Options Section */}
            <div className="space-y-4">
              <Label className="text-sm font-medium flex items-center gap-2">
                <span className="w-6 h-6 bg-blue-100 text-blue-700 rounded-full flex items-center justify-center text-xs font-bold">
                  3
                </span>
                Playback Options
              </Label>
              <div className="space-y-4 pl-8">
                <div className="flex items-start space-x-3 p-3 rounded-lg border bg-gray-50/50 hover:bg-gray-50 transition-colors">
                  <Checkbox
                    id="show-controls"
                    checked={youtubeShowControls}
                    onCheckedChange={(checked) => setYoutubeShowControls(checked === true)}
                    className="mt-0.5"
                  />
                  <div className="flex-1">
                    <label htmlFor="show-controls" className="text-sm font-medium leading-none cursor-pointer">
                      Show player controls
                    </label>
                    <p className="text-xs text-muted-foreground mt-1">
                      Display play/pause, volume, and timeline controls
                    </p>
                  </div>
                </div>

                <div className="flex items-start space-x-3 p-3 rounded-lg border bg-gray-50/50 hover:bg-gray-50 transition-colors">
                  <Checkbox
                    id="show-info"
                    checked={youtubeShowInfo}
                    onCheckedChange={(checked) => setYoutubeShowInfo(checked === true)}
                    className="mt-0.5"
                  />
                  <div className="flex-1">
                    <label htmlFor="show-info" className="text-sm font-medium leading-none cursor-pointer">
                      Show video information
                    </label>
                    <p className="text-xs text-muted-foreground mt-1">Display video title and channel name</p>
                  </div>
                </div>

                <div className="flex items-start space-x-3 p-3 rounded-lg border bg-gray-50/50 hover:bg-gray-50 transition-colors">
                  <Checkbox
                    id="show-related"
                    checked={youtubeShowRelated}
                    onCheckedChange={(checked) => setYoutubeShowRelated(checked === true)}
                    className="mt-0.5"
                  />
                  <div className="flex-1">
                    <label htmlFor="show-related" className="text-sm font-medium leading-none cursor-pointer">
                      Show related videos
                    </label>
                    <p className="text-xs text-muted-foreground mt-1">Display suggested videos when paused or ended</p>
                  </div>
                </div>
              </div>
            </div>

            {/* Error Display */}
            {youtubeError && (
              <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
                <div className="flex items-start gap-3">
                  <AlertCircle className="h-5 w-5 text-red-600 mt-0.5 flex-shrink-0" />
                  <div>
                    <p className="text-sm font-medium text-red-800">Invalid URL</p>
                    <p className="text-sm text-red-700 mt-1">{youtubeError}</p>
                  </div>
                </div>
              </div>
            )}
          </div>

          <DialogFooter className="pt-6 border-t bg-gray-50/50 -mx-6 -mb-6 px-6 py-4">
            <div className="flex items-center justify-between w-full">
              <Button variant="ghost" onClick={() => setShowYouTubeDialog(false)} className="px-6">
                Cancel
              </Button>
              <Button
                onClick={handleYouTubeSubmit}
                disabled={!youtubeUrl.trim()}
                className="px-8 bg-red-600 hover:bg-red-700"
              >
                <Youtube className="h-4 w-4 mr-2" />
                Insert Video
              </Button>
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      <Dialog open={showSpotifyDialog} onOpenChange={setShowSpotifyDialog}>
        <DialogContent className="sm:max-w-[500px] max-h-[90vh] overflow-hidden">
          <DialogHeader className="space-y-3 pb-6 border-b">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-green-50 rounded-lg">
                <Music2 className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <DialogTitle className="text-xl font-semibold">Insert Spotify</DialogTitle>
                <DialogDescription className="text-sm text-muted-foreground mt-1">
                  Embed Spotify content with customizable display options
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          <div className="py-6 space-y-6 max-h-[60vh] overflow-y-auto">
            {/* URL Input Section */}
            <div className="space-y-3">
              <Label htmlFor="spotify-url" className="text-sm font-medium flex items-center gap-2">
                <span className="w-6 h-6 bg-green-100 text-green-700 rounded-full flex items-center justify-center text-xs font-bold">
                  1
                </span>
                Spotify URL
              </Label>
              <div className="relative">
                <Input
                  id="spotify-url"
                  placeholder="https://open.spotify.com/track/4iV5W9uYEdYUVa79Axb7Rh"
                  value={spotifyUrl}
                  onChange={(e) => setSpotifyUrl(e.target.value)}
                  className={`pl-4 pr-4 py-3 text-sm ${spotifyError ? "border-red-300 focus:border-red-500" : ""}`}
                />
                {spotifyUrl && !spotifyError && (
                  <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                    <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                  </div>
                )}
              </div>
              <div className="bg-green-50 p-3 rounded-lg border border-green-200">
                <p className="text-xs text-green-800 font-medium mb-2">Supported content types:</p>
                <div className="grid grid-cols-2 gap-2 text-xs text-green-700">
                  <div className="flex items-center gap-1">
                    <Music className="h-3 w-3" />
                    <span>Tracks</span>
                  </div>
                  <div className="flex items-center gap-1">
                    <Music2 className="h-3 w-3" />
                    <span>Albums</span>
                  </div>
                  <div className="flex items-center gap-1">
                    <Music3 className="h-3 w-3" />
                    <span>Playlists</span>
                  </div>
                  <div className="flex items-center gap-1">
                    <UserPlus className="h-3 w-3" />
                    <span>Artists</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Display Options Section */}
            <div className="space-y-4">
              <Label className="text-sm font-medium flex items-center gap-2">
                <span className="w-6 h-6 bg-green-100 text-green-700 rounded-full flex items-center justify-center text-xs font-bold">
                  2
                </span>
                Display Options
              </Label>
              <div className="space-y-4 pl-8">
                <div className="flex items-start space-x-3 p-3 rounded-lg border bg-gray-50/50 hover:bg-gray-50 transition-colors">
                  <Checkbox
                    id="show-theme"
                    checked={spotifyShowTheme}
                    onCheckedChange={(checked) => setSpotifyShowTheme(checked === true)}
                    className="mt-0.5"
                  />
                  <div className="flex-1">
                    <label htmlFor="show-theme" className="text-sm font-medium leading-none cursor-pointer">
                      Show Spotify theme color
                    </label>
                    <p className="text-xs text-muted-foreground mt-1">
                      Display the characteristic Spotify green theme in the embed
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* Error Display */}
            {spotifyError && (
              <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
                <div className="flex items-start gap-3">
                  <AlertCircle className="h-5 w-5 text-red-600 mt-0.5 flex-shrink-0" />
                  <div>
                    <p className="text-sm font-medium text-red-800">Invalid URL</p>
                    <p className="text-sm text-red-700 mt-1">{spotifyError}</p>
                  </div>
                </div>
              </div>
            )}

            {/* Preview Section */}
            {spotifyUrl && !spotifyError && (
              <div className="space-y-3">
                <Label className="text-sm font-medium flex items-center gap-2">
                  <Eye className="h-4 w-4" />
                  Preview
                </Label>
                <div className="p-4 bg-green-50 border border-green-200 rounded-lg">
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 bg-green-600 rounded-lg flex items-center justify-center">
                      <Music2 className="h-6 w-6 text-white" />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-green-800">Spotify content ready to embed</p>
                      <p className="text-xs text-green-600">Content will be displayed with interactive player</p>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>

          <DialogFooter className="pt-6 border-t bg-gray-50/50 -mx-6 -mb-6 px-6 py-4">
            <div className="flex items-center justify-between w-full">
              <Button variant="ghost" onClick={() => setShowSpotifyDialog(false)} className="px-6">
                Cancel
              </Button>
              <Button
                onClick={handleSpotifySubmit}
                disabled={!spotifyUrl.trim()}
                className="px-8 bg-green-600 hover:bg-green-700"
              >
                <Music2 className="h-4 w-4 mr-2" />
                Insert Content
              </Button>
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
