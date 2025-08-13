"use client"
import { useState, useEffect, useRef, useCallback } from "react"
import { ChevronLeft, ChevronRight, Expand, X, LayoutGrid } from 'lucide-react'
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"
import type { Slide, TransitionEffect, SlideTheme } from "../nodes/presentation/types"
import { SlideRenderer } from "../nodes/presentation/slide-renderer"

interface SlidePlayerProps {
  slides: Slide[]
  title?: string
  currentSlideIndex: number
  onSlideChange: (index: number) => void

  // Auto-advance settings
  autoAdvance?: boolean
  autoAdvanceDelay?: number
  autoAdvanceLoop?: boolean

  // Display settings
  showControls?: boolean
  showThumbnails?: boolean
  showHeader?: boolean
  showFullscreenButton?: boolean

  // Transition settings
  transitionEffect?: TransitionEffect

  // Theme settings
  theme?: SlideTheme
  customThemeColor?: string | null

  // Layout settings
  aspectRatio?: "video" | "square" | "wide"
  size?: "sm" | "md" | "lg" | "xl"

  // Event handlers
  onFullscreen?: () => void
  onExitFullscreen?: () => void

  // State flags
  isPresenting?: boolean
  isEditing?: boolean
  className?: string
}

export function SlidePlayer({
  slides,
  title,
  currentSlideIndex,
  onSlideChange,
  autoAdvance = false,
  autoAdvanceDelay = 5,
  autoAdvanceLoop = false,
  showControls = true,
  showThumbnails = true,
  showHeader = true,
  showFullscreenButton = false,
  transitionEffect = "fade",
  theme = "light",
  customThemeColor,
  aspectRatio = "video",
  size = "md",
  onFullscreen,
  onExitFullscreen,
  isPresenting = false,
  isEditing = false,
  className,
}: SlidePlayerProps) {
  const [isFullscreen, setIsFullscreen] = useState(false)
  const [showMenu, setShowMenu] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const playerRef = useRef<HTMLDivElement>(null)

  // Memoize navigation functions to ensure stability for event listeners
  const handlePrevSlide = useCallback(() => {
    if (currentSlideIndex > 0) {
      onSlideChange(currentSlideIndex - 1)
    } else if (autoAdvanceLoop) {
      onSlideChange(slides.length - 1)
    }
  }, [currentSlideIndex, onSlideChange, autoAdvanceLoop, slides.length])

  const handleNextSlide = useCallback(() => {
    if (currentSlideIndex < slides.length - 1) {
      onSlideChange(currentSlideIndex + 1)
    } else if (autoAdvanceLoop) {
      onSlideChange(0)
    }
  }, [currentSlideIndex, onSlideChange, autoAdvanceLoop, slides.length])

  // Handle fullscreen mode
  useEffect(() => {
    const handleFullscreenChange = () => {
      const fullscreenElement = !!document.fullscreenElement
      setIsFullscreen(fullscreenElement)
      if (!fullscreenElement && onExitFullscreen) {
        onExitFullscreen()
      }
    }

    document.addEventListener("fullscreenchange", handleFullscreenChange)
    return () => {
      document.removeEventListener("fullscreenchange", handleFullscreenChange)
    }
  }, [onExitFullscreen])

  // Handle auto-advance
  useEffect(() => {
    let timer: NodeJS.Timeout | null = null

    if (autoAdvance && !isEditing && slides.length > 0 && !isFullscreen) {
      timer = setTimeout(() => {
        if (currentSlideIndex < slides.length - 1) {
          onSlideChange(currentSlideIndex + 1)
        } else if (autoAdvanceLoop) {
          onSlideChange(0)
        }
      }, autoAdvanceDelay * 1000)
    }

    return () => {
      if (timer) clearTimeout(timer)
    }
  }, [
    autoAdvance,
    autoAdvanceDelay,
    autoAdvanceLoop,
    currentSlideIndex,
    slides.length,
    isEditing,
    isFullscreen,
    onSlideChange,
  ])

  // Handle keyboard navigation
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (isFullscreen) {
        switch (event.key) {
          case "ArrowLeft":
            event.preventDefault()
            handlePrevSlide()
            break
          case "ArrowRight":
            event.preventDefault()
            handleNextSlide()
            break
          case "Escape":
            event.preventDefault()
            if (document.fullscreenElement) {
              document.exitFullscreen()
            }
            break
        }
      }
    }

    document.addEventListener("keydown", handleKeyDown)
    return () => {
      document.removeEventListener("keydown", handleKeyDown)
    }
  }, [isFullscreen, handlePrevSlide, handleNextSlide])

  // Handle mouse position for bottom bar in fullscreen
  const [showBottomBar, setShowBottomBar] = useState(false)
  const [mouseY, setMouseY] = useState(0)

  useEffect(() => {
    const handleMouseMove = (event: MouseEvent) => {
      if (isFullscreen) {
        const windowHeight = window.innerHeight
        const bottomThreshold = windowHeight - 50 // 50px from bottom
        setMouseY(event.clientY)
        setShowBottomBar(event.clientY > bottomThreshold)
      }
    }

    if (isFullscreen) {
      document.addEventListener("mousemove", handleMouseMove)
      return () => {
        document.removeEventListener("mousemove", handleMouseMove)
      }
    }
  }, [isFullscreen])

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (isFullscreen && showBottomBar) {
        const target = event.target as HTMLElement
        const bottomBar = document.querySelector("[data-bottom-bar]")
        if (bottomBar && !bottomBar.contains(target)) {
          setShowBottomBar(false)
        }
      }
    }

    if (isFullscreen) {
      document.addEventListener("click", handleClickOutside)
      return () => {
        document.removeEventListener("click", handleClickOutside)
      }
    }
  }, [isFullscreen, showBottomBar])

  const toggleFullscreen = () => {
    if (!isFullscreen && playerRef.current) {
      if (playerRef.current.requestFullscreen) {
        playerRef.current.requestFullscreen()
        if (onFullscreen) onFullscreen()
      }
    } else if (isFullscreen && document.fullscreenElement) {
      document.exitFullscreen().catch((err) => {
        console.error("Error exiting fullscreen:", err)
      })
    }
  }

  // Get thumbnail style for slide with custom settings
  const getThumbnailStyle = (slide: Slide) => {
    const hasCustomImageSettings = slide.filters || slide.imageSize
    
    if (slide.theme === "image" && slide.backgroundImage) {
      if (hasCustomImageSettings) {
        // Apply custom filters and sizing to thumbnail
        const filters = slide.filters || {
          brightness: 100,
          contrast: 100,
          saturation: 100,
          blur: 0,
          hueRotate: 0,
          opacity: 100,
        }
        
        const imageSize = slide.imageSize || {
          width: 100,
          height: 100,
          objectFit: "cover" as const,
        }

        return {
          backgroundImage: `url(${slide.backgroundImage})`,
          backgroundSize: imageSize.objectFit === "cover" ? "cover" : "contain",
          backgroundPosition: "center",
          backgroundRepeat: "no-repeat",
          filter: `brightness(${filters.brightness}%) contrast(${filters.contrast}%) saturate(${filters.saturation}%) blur(${Math.max(0, filters.blur * 0.5)}px) hue-rotate(${filters.hueRotate}deg) opacity(${filters.opacity}%)`,
        }
      } else {
        // Default background image style
        return {
          backgroundImage: `url(${slide.backgroundImage})`,
          backgroundSize: "cover",
          backgroundPosition: "center",
          backgroundRepeat: "no-repeat",
        }
      }
    }
    
    // Default solid background based on theme
    switch (slide.theme) {
      case "light":
        return { backgroundColor: "white" }
      case "dark":
        return { backgroundColor: "#1a1a1a" }
      case "standard":
        return { backgroundColor: "#f8f9fa" }
      case "gradient":
        return { background: "linear-gradient(135deg, #6366f1, #8b5cf6)" }
      case "custom":
        return { backgroundColor: customThemeColor || "#3b82f6" }
      default:
        return { backgroundColor: "white" }
    }
  }

  const getThemeColor = () => {
    switch (theme) {
      case "light":
        return "border-gray-200 bg-gray-100 text-gray-800"
      case "dark":
        return "border-gray-700 bg-gray-800 text-gray-200"
      case "standard":
        return "border-blue-200 bg-blue-100 text-blue-800"
      case "custom":
        return `border-[${customThemeColor || "#3b82f6"}] bg-opacity-10 bg-[${customThemeColor || "#3b82f6"}] text-[${customThemeColor || "#3b82f6"}]`
      case "gradient":
        return "border-purple-200 bg-purple-100 text-purple-800"
      case "image":
        return "border-gray-300 bg-gray-200 text-gray-800"
      default:
        return "border-gray-200 bg-gray-100 text-gray-800"
    }
  }

  const getSlideInfo = (slide: Slide) => {
    const info = []
    if (slide.images && slide.images.length > 0) {
      info.push(`${slide.images.length} image${slide.images.length > 1 ? "s" : ""}`)
    }
    if (slide.shapes && slide.shapes.length > 0) {
      info.push(`${slide.shapes.length} shape${slide.shapes.length > 1 ? "s" : ""}`)
    }
    if (slide.notes && slide.notes.trim()) {
      info.push("notes")
    }
    return info
  }

  const getAspectRatioClass = () => {
    switch (aspectRatio) {
      case "square":
        return "aspect-square"
      case "wide":
        return "aspect-[21/9]"
      case "video":
      default:
        return "aspect-video"
    }
  }

  const getSizeClass = () => {
    switch (size) {
      case "sm":
        return "max-w-md"
      case "md":
        return "max-w-2xl"
      case "lg":
        return "max-w-4xl"
      case "xl":
        return "max-w-6xl"
      default:
        return "max-w-2xl"
    }
  }

  if (slides.length === 0) {
    return (
      <div
        className={cn("flex items-center justify-center h-40 bg-muted/20 rounded-lg border border-dashed", className)}
      >
        <div className="text-center">
          <LayoutGrid className="h-8 w-8 mx-auto text-muted-foreground" />
          <p className="mt-2 text-sm text-muted-foreground">No slides available</p>
        </div>
      </div>
    )
  }

  const currentSlide = slides[currentSlideIndex] || slides[0]
  const slideInfo = getSlideInfo(currentSlide)

  return (
    <div
      ref={containerRef}
      className={cn(
        "relative group focus-within:ring-2 focus-within:ring-primary/20 rounded-lg",
        getSizeClass(),
        className,
      )}
      onMouseEnter={() => setShowMenu(true)}
      onMouseLeave={() => setShowMenu(false)}
    >
      <div
        ref={playerRef}
        className="relative focus:outline-none"
        tabIndex={0}
        onKeyDown={(event) => {
          switch (event.key) {
            case "ArrowLeft":
              event.preventDefault()
              handlePrevSlide()
              break
            case "ArrowRight":
              event.preventDefault()
              handleNextSlide()
              break
          }
        }}
      >
        <div className={`rounded-lg overflow-hidden border shadow-sm ${getThemeColor()}`}>
          {/* Header */}
          {showHeader && title && (
            <div className="bg-gray-100 dark:bg-gray-800 p-2 flex items-center justify-between">
              <span className="text-sm font-medium truncate">{title}</span>
              <span className="flex items-center gap-2">
                {slideInfo.length > 0 && (
                  <span className="flex gap-1">
                    {slideInfo.map((info, index) => (
                      <Badge key={index} variant="secondary" className="text-xs">
                        {info}
                      </Badge>
                    ))}
                  </span>
                )}
                <span className="text-xs text-muted-foreground">
                  {currentSlideIndex + 1} / {slides.length}
                </span>
              </span>
            </div>
          )}

          {/* Main slide area */}
          <div
            className={cn(
              "relative transition-all duration-500",
              getAspectRatioClass(),
              transitionEffect === "fade"
                ? "transition-opacity"
                : transitionEffect === "slide"
                  ? "transition-transform"
                  : transitionEffect === "zoom"
                    ? "transition-transform"
                    : "",
            )}
          >
            <div
              key={currentSlide.id}
              className={cn(
                "w-full h-full",
                transitionEffect === "fade"
                  ? "animate-in fade-in duration-500"
                  : transitionEffect === "slide"
                    ? "animate-in slide-in-from-right duration-500"
                    : transitionEffect === "zoom"
                      ? "animate-in zoom-in duration-500"
                      : "",
              )}
            >
              <SlideRenderer slide={currentSlide} customThemeColor={customThemeColor} />
            </div>

            {/* Navigation controls */}
            {showControls && slides.length > 1 && !isPresenting && (
              <>
                <Button
                  variant="ghost"
                  size="icon"
                  className="absolute left-2 top-1/2 -translate-y-1/2 bg-black/20 hover:bg-black/40 text-white rounded-full h-8 w-8 z-20 opacity-0 group-hover:opacity-100 transition-opacity"
                  onClick={handlePrevSlide}
                  disabled={!autoAdvanceLoop && currentSlideIndex === 0}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  className="absolute right-2 top-1/2 -translate-y-1/2 bg-black/20 hover:bg-black/40 text-white rounded-full h-8 w-8 z-20 opacity-0 group-hover:opacity-100 transition-opacity"
                  onClick={handleNextSlide}
                  disabled={!autoAdvanceLoop && currentSlideIndex === slides.length - 1}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </>
            )}

            {/* Fullscreen button */}
            {showFullscreenButton && !isPresenting && (
              <Button
                variant="ghost"
                size="icon"
                className="absolute top-2 right-2 bg-black/20 hover:bg-black/40 text-white rounded-full h-8 w-8 z-20 opacity-0 group-hover:opacity-100 transition-opacity"
                onClick={toggleFullscreen}
              >
                <Expand className="h-4 w-4" />
              </Button>
            )}
          </div>

          {/* Thumbnails */}
          {showThumbnails && !isPresenting && (
            <div className={`p-2 overflow-x-auto ${getThemeColor()}`}>
              <div className="flex gap-2">
                {slides.map((slide, index) => {
                  const info = getSlideInfo(slide)
                  return (
                    <button
                      key={slide.id}
                      className={cn(
                        "flex-shrink-0 w-16 h-10 rounded overflow-hidden border-2 transition-all relative group",
                        index === currentSlideIndex
                          ? "border-primary ring-2 ring-primary/20"
                          : "border-transparent hover:border-gray-300",
                      )}
                      onClick={() => onSlideChange(index)}
                    >
                      <div className="w-full h-full relative" style={getThumbnailStyle(slide)}>
                        {slide.theme === "image" && slide.backgroundImage && (
                          <div className="absolute inset-0 bg-black/40"></div>
                        )}
                        <div className="w-full h-full flex items-center justify-center">
                          <span className="text-xs font-medium text-white drop-shadow-sm">{index + 1}</span>
                        </div>

                        {/* Slide info indicators */}
                        {info.length > 0 && (
                          <span className="absolute top-0 right-0 flex gap-0.5 p-0.5">
                            {slide.images && slide.images.length > 0 && (
                              <span className="w-1.5 h-1.5 bg-blue-500 rounded-full block" title="Has images" />
                            )}
                            {slide.shapes && slide.shapes.length > 0 && (
                              <span className="w-1.5 h-1.5 bg-green-500 rounded-full block" title="Has shapes" />
                            )}
                            {slide.notes && slide.notes.trim() && (
                              <span className="w-1.5 h-1.5 bg-yellow-500 rounded-full block" title="Has notes" />
                            )}
                          </span>
                        )}

                        {/* Edited indicator */}
                        {(slide.filters || slide.imageSize) && (
                          <span
                            className="absolute bottom-0 left-0 w-2 h-2 bg-orange-500 rounded-tr-sm block"
                            title="Edited"
                          />
                        )}
                      </div>
                    </button>
                  )
                })}
              </div>
            </div>
          )}
        </div>

        {/* Fullscreen presentation mode */}
        {isFullscreen && (
          <div className="fixed inset-0 bg-black z-50 flex flex-col">
            {/* Exit fullscreen button */}
            <Button
              variant="ghost"
              size="icon"
              className="absolute top-4 right-4 bg-black/20 hover:bg-black/40 text-white rounded-full h-10 w-10 z-50 opacity-80 hover:opacity-100 transition-opacity"
              onClick={() => document.exitFullscreen()}
            >
              <X className="h-5 w-5" />
            </Button>

            <div className="flex-1 flex items-center justify-center relative">
              <SlideRenderer slide={currentSlide} customThemeColor={customThemeColor} />

              {/* Navigation arrows in fullscreen */}
              {showControls && slides.length > 1 && (
                <>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="absolute left-8 top-1/2 -translate-y-1/2 bg-black/20 hover:bg-black/40 text-white rounded-full h-12 w-12 z-40 opacity-60 hover:opacity-100 transition-opacity"
                    onClick={handlePrevSlide}
                    disabled={!autoAdvanceLoop && currentSlideIndex === 0}
                  >
                    <ChevronLeft className="h-6 w-6" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="absolute right-8 top-1/2 -translate-y-1/2 bg-black/20 hover:bg-black/40 text-white rounded-full h-12 w-12 z-40 opacity-60 hover:opacity-100 transition-opacity"
                    onClick={handleNextSlide}
                    disabled={!autoAdvanceLoop && currentSlideIndex === slides.length - 1}
                  >
                    <ChevronRight className="h-6 w-6" />
                  </Button>
                </>
              )}
            </div>

            {/* Hidden clickable area at bottom */}
            <div
              className="absolute bottom-0 left-0 right-0 h-[50px] z-30"
              onMouseEnter={() => setShowBottomBar(true)}
            />

            {/* Bottom slide navigation bar */}
            {showThumbnails && (
              <div
                data-bottom-bar
                className={`absolute bottom-0 left-0 right-0 bg-black/80 backdrop-blur-sm transition-transform duration-300 z-40 ${
                  showBottomBar ? "translate-y-0" : "translate-y-full"
                }`}
              >
                <div className="p-4">
                  <div className="flex items-center justify-center gap-2 mb-2">
                    <span className="text-white text-sm">
                      {currentSlideIndex + 1} / {slides.length}
                    </span>
                  </div>
                  <div className="flex justify-center">
                    <div className="flex gap-2 overflow-x-auto max-w-4xl">
                      {slides.map((slide, index) => {
                        const info = getSlideInfo(slide)
                        return (
                          <button
                            key={slide.id}
                            className={cn(
                              "flex-shrink-0 w-20 h-12 rounded overflow-hidden border-2 transition-all relative group",
                              index === currentSlideIndex
                                ? "border-white ring-2 ring-white/40"
                                : "border-transparent hover:border-white/60",
                            )}
                            onClick={() => onSlideChange(index)}
                          >
                            <div className="w-full h-full relative" style={getThumbnailStyle(slide)}>
                              {slide.theme === "image" && slide.backgroundImage && (
                                <div className="absolute inset-0 bg-black/40"></div>
                              )}
                              <div className="w-full h-full flex items-center justify-center">
                                <span className="text-xs font-medium text-white drop-shadow-sm">{index + 1}</span>
                              </div>

                              {/* Slide info indicators */}
                              {info.length > 0 && (
                                <span className="absolute top-0 right-0 flex gap-0.5 p-0.5">
                                  {slide.images && slide.images.length > 0 && (
                                    <span className="w-1.5 h-1.5 bg-blue-400 rounded-full block" title="Has images" />
                                  )}
                                  {slide.shapes && slide.shapes.length > 0 && (
                                    <span className="w-1.5 h-1.5 bg-green-400 rounded-full block" title="Has shapes" />
                                  )}
                                  {slide.notes && slide.notes.trim() && (
                                    <span className="w-1.5 h-1.5 bg-yellow-400 rounded-full block" title="Has notes" />
                                  )}
                                </span>
                              )}

                              {/* Edited indicator */}
                              {(slide.filters || slide.imageSize) && (
                                <span
                                  className="absolute bottom-0 left-0 w-2 h-2 bg-orange-400 rounded-tr-sm block"
                                  title="Edited"
                                />
                              )}
                            </div>
                          </button>
                        )
                      })}
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
