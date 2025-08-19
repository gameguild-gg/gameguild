"use client"
import { ImageIcon } from 'lucide-react'
import { cn } from "@/lib/utils"
import type { Slide, SlideTheme } from "./types"

interface SlideRendererProps {
  slide: Slide
  customThemeColor?: string | null
}

export function SlideRenderer({ slide, customThemeColor }: SlideRendererProps) {
  // Check if slide has custom image settings
  const hasCustomImageSettings = slide.filters || slide.imageSize

  // Get background style based on theme
  const getBackgroundStyle = (slideTheme: SlideTheme, backgroundImage?: string, backgroundGradient?: string) => {
    switch (slideTheme) {
      case "light":
        return { backgroundColor: "white", color: "black" }
      case "dark":
        return { backgroundColor: "#1a1a1a", color: "white" }
      case "standard":
        return { backgroundColor: "#f8f9fa", color: "#333333" }
      case "gradient":
        return {
          background: backgroundGradient || "linear-gradient(135deg, #6366f1, #8b5cf6)",
          color: "white",
        }
      case "custom":
        return {
          backgroundColor: customThemeColor || "#3b82f6",
          color: "#ffffff",
        }
      case "image":
        // Only show background image if no custom settings are applied
        if (!hasCustomImageSettings && backgroundImage) {
          return {
            backgroundImage: `url(${backgroundImage})`,
            backgroundSize: "cover",
            backgroundPosition: "center",
            color: "white",
            position: "relative" as const,
          }
        } else {
          // Use solid background when image has custom settings
          return { backgroundColor: "#1a1a1a", color: "white" }
        }
      default:
        return { backgroundColor: "white", color: "black" }
    }
  }

  // Get layout style based on layout type
  const getLayoutStyle = (layout: string) => {
    switch (layout) {
      case "title":
        return "flex flex-col items-center justify-center text-center"
      case "content":
        return "flex flex-col p-8"
      case "title-content":
        return "flex flex-col p-8"
      case "image-text":
        return "flex flex-col gap-4 p-8"
      case "text-image":
        return "flex flex-col gap-4 p-8"
      case "full-image":
        return "relative"
      case "two-column":
        return "flex flex-row gap-8 p-8"
      case "blank":
        return "relative p-8"
      default:
        return "flex flex-col p-8"
    }
  }

  const backgroundStyle = getBackgroundStyle(slide.theme, slide.backgroundImage, slide.backgroundGradient)
  const layoutClass = getLayoutStyle(slide.layout)

  // Add overlay for image backgrounds to ensure text readability (only if no custom settings)
  const hasOverlay = slide.theme === "image" && slide.backgroundImage && !hasCustomImageSettings

  // Get image style with filters and size
  const getImageStyle = () => {
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
      filter: `brightness(${filters.brightness}%) contrast(${filters.contrast}%) saturate(${filters.saturation}%) blur(${filters.blur}px) hue-rotate(${filters.hueRotate}deg) opacity(${filters.opacity}%)`,
      width: `${imageSize.width}%`,
      height: `${imageSize.height}%`,
      objectFit: imageSize.objectFit,
    }
  }

  // Render shapes if present
  const renderShapes = () => {
    if (!slide.shapes || slide.shapes.length === 0) return null

    return (
      <div className="absolute inset-0 pointer-events-none">
        {slide.shapes.map((shape) => (
          <div
            key={shape.id}
            className="absolute"
            style={{
              left: shape.x ? `${shape.x}px` : undefined,
              top: shape.y ? `${shape.y}px` : undefined,
              width: shape.width ? `${shape.width}px` : undefined,
              height: shape.height ? `${shape.height}px` : undefined,
              backgroundColor: shape.fill,
              border: shape.stroke ? `${shape.strokeWidth || 1}px solid ${shape.stroke}` : undefined,
              borderRadius: shape.type === "circle" || shape.type === "ellipse" ? "50%" : undefined,
            }}
          >
            {shape.text && <div className="flex items-center justify-center h-full text-sm p-2">{shape.text}</div>}
          </div>
        ))}
      </div>
    )
  }

  // Render embedded images with custom styling
  const renderEmbeddedImages = () => {
    if (!slide.images || slide.images.length === 0) return null

    return (
      <div className="flex flex-wrap gap-4 justify-center">
        {slide.images.map((image, index) => (
          <img
            key={index}
            src={image || "/placeholder.svg"}
            alt={`Slide image ${index + 1}`}
            className="max-w-full max-h-64 object-contain rounded-lg shadow-lg"
            style={hasCustomImageSettings ? getImageStyle() : undefined}
          />
        ))}
      </div>
    )
  }

  // Render background image with custom styling (when theme is image and has custom settings)
  const renderCustomBackgroundImage = () => {
    if (slide.theme !== "image" || !slide.backgroundImage || !hasCustomImageSettings) return null

    return (
      <div className="absolute inset-0 flex items-center justify-center">
        <img
          src={slide.backgroundImage || "/placeholder.svg"}
          alt="Slide background"
          className="max-w-full max-h-full object-contain"
          style={getImageStyle()}
        />
      </div>
    )
  }

  // Process content with basic markdown support
  const processContent = (content: string) => {
    return content.split("\n").map((line, index) => {
      // Handle bold text
      if (line.includes("**")) {
        const parts = line.split(/(\*\*.*?\*\*)/g)
        return (
          <p key={index} className="mb-2">
            {parts.map((part, partIndex) => {
              if (part.startsWith("**") && part.endsWith("**")) {
                return (
                  <strong key={partIndex} className="font-bold">
                    {part.slice(2, -2)}
                  </strong>
                )
              }
              return part
            })}
          </p>
        )
      }

      // Handle italic text
      if (line.includes("*") && !line.includes("**")) {
        const parts = line.split(/(\*.*?\*)/g)
        return (
          <p key={index} className="mb-2">
            {parts.map((part, partIndex) => {
              if (part.startsWith("*") && part.endsWith("*") && !part.startsWith("**")) {
                return (
                  <em key={partIndex} className="italic">
                    {part.slice(1, -1)}
                  </em>
                )
              }
              return part
            })}
          </p>
        )
      }

      return line.trim() ? (
        <p key={index} className="mb-2">
          {line}
        </p>
      ) : (
        <br key={index} />
      )
    })
  }

  return (
    <div className={cn("w-full h-full overflow-hidden relative aspect-video")} style={backgroundStyle}>
      {hasOverlay && <div className="absolute inset-0 bg-black bg-opacity-30"></div>}

      {/* Render custom background image if applicable */}
      {renderCustomBackgroundImage()}

      {/* Render shapes */}
      {renderShapes()}

      <div className={cn("relative z-10 w-full h-full", layoutClass)}>
        {slide.layout === "title" && (
          <div className="p-8 flex flex-col items-center justify-center h-full">
            <h1 className="text-4xl font-bold mb-4 text-center leading-tight">{slide.title}</h1>
            {slide.images && slide.images.length > 0 && <div className="mt-6">{renderEmbeddedImages()}</div>}
          </div>
        )}

        {slide.layout === "content" && (
          <div className="p-8 flex flex-col justify-center h-full">
            <div className="prose prose-lg max-w-none text-inherit">{processContent(slide.content)}</div>
            {slide.images && slide.images.length > 0 && <div className="mt-6">{renderEmbeddedImages()}</div>}
          </div>
        )}

        {slide.layout === "title-content" && (
          <div className="p-8 flex flex-col h-full">
            <h2 className="text-3xl font-bold mb-6 leading-tight">{slide.title}</h2>
            <div className="prose prose-lg max-w-none text-inherit flex-1">{processContent(slide.content)}</div>
            {slide.images && slide.images.length > 0 && <div className="mt-6">{renderEmbeddedImages()}</div>}
          </div>
        )}

        {slide.layout === "image-text" && (
          <div className="flex flex-col gap-6 p-8 h-full">
            {slide.backgroundImage || (slide.images && slide.images.length > 0) ? (
              <div className="flex-1 flex items-center justify-center">
                {slide.backgroundImage && !hasCustomImageSettings ? (
                  <img
                    src={slide.backgroundImage || "/placeholder.svg"}
                    alt="Slide image"
                    className="max-w-full max-h-full object-contain rounded-lg shadow-lg"
                  />
                ) : slide.backgroundImage && hasCustomImageSettings ? (
                  <img
                    src={slide.backgroundImage || "/placeholder.svg"}
                    alt="Slide image"
                    className="max-w-full max-h-full object-contain rounded-lg shadow-lg"
                    style={getImageStyle()}
                  />
                ) : (
                  renderEmbeddedImages()
                )}
              </div>
            ) : (
              <div className="flex-1 bg-gray-200 dark:bg-gray-700 flex items-center justify-center rounded-lg">
                <ImageIcon className="h-12 w-12 text-gray-400" />
              </div>
            )}
            <div className="prose max-w-none text-inherit">
              <h3 className="text-xl font-semibold mb-2">{slide.title}</h3>
              {processContent(slide.content)}
            </div>
          </div>
        )}

        {slide.layout === "text-image" && (
          <div className="flex flex-col gap-6 p-8 h-full">
            <div className="prose max-w-none text-inherit">
              <h3 className="text-xl font-semibold mb-2">{slide.title}</h3>
              {processContent(slide.content)}
            </div>
            {slide.backgroundImage || (slide.images && slide.images.length > 0) ? (
              <div className="flex-1 flex items-center justify-center">
                {slide.backgroundImage && !hasCustomImageSettings ? (
                  <img
                    src={slide.backgroundImage || "/placeholder.svg"}
                    alt="Slide image"
                    className="max-w-full max-h-full object-contain rounded-lg shadow-lg"
                  />
                ) : slide.backgroundImage && hasCustomImageSettings ? (
                  <img
                    src={slide.backgroundImage || "/placeholder.svg"}
                    alt="Slide image"
                    className="max-w-full max-h-full object-contain rounded-lg shadow-lg"
                    style={getImageStyle()}
                  />
                ) : (
                  renderEmbeddedImages()
                )}
              </div>
            ) : (
              <div className="flex-1 bg-gray-200 dark:bg-gray-700 flex items-center justify-center rounded-lg">
                <ImageIcon className="h-12 w-12 text-gray-400" />
              </div>
            )}
          </div>
        )}

        {slide.layout === "full-image" && (
          <div className="relative w-full h-full">
            {slide.backgroundImage && !hasCustomImageSettings && (
              <img
                src={slide.backgroundImage || "/placeholder.svg"}
                alt="Slide background"
                className="absolute inset-0 w-full h-full object-cover"
              />
            )}
            {slide.backgroundImage && hasCustomImageSettings && (
              <div className="absolute inset-0 flex items-center justify-center">
                <img
                  src={slide.backgroundImage || "/placeholder.svg"}
                  alt="Slide background"
                  className="max-w-full max-h-full object-contain"
                  style={getImageStyle()}
                />
              </div>
            )}
            {slide.images && slide.images.length > 0 && !slide.backgroundImage && (
              <div className="absolute inset-0 flex items-center justify-center">
                <img
                  src={slide.images[0] || "/placeholder.svg"}
                  alt="Slide image"
                  className="max-w-full max-h-full object-contain"
                  style={hasCustomImageSettings ? getImageStyle() : undefined}
                />
              </div>
            )}
            <div className="absolute bottom-0 left-0 right-0 p-8">
              {slide.content && <div className="text-white/90">{processContent(slide.content)}</div>}
            </div>
          </div>
        )}

        {slide.layout === "two-column" && (
          <div className="flex flex-row gap-8 p-8 h-full">
            <div className="flex-1">
              <h3 className="text-xl font-semibold mb-4">{slide.title}</h3>
              <div className="prose max-w-none text-inherit">{processContent(slide.content)}</div>
            </div>
            <div className="flex-1 flex items-center justify-center">
              {slide.images && slide.images.length > 0 ? (
                renderEmbeddedImages()
              ) : (
                <div className="bg-gray-200 dark:bg-gray-700 flex items-center justify-center rounded-lg h-full w-full">
                  <ImageIcon className="h-12 w-12 text-gray-400" />
                </div>
              )}
            </div>
          </div>
        )}

        {slide.layout === "blank" && (
          <div className="relative w-full h-full p-8">
            {slide.title && <h2 className="text-2xl font-bold mb-4">{slide.title}</h2>}
            {slide.content && <div className="prose max-w-none text-inherit">{processContent(slide.content)}</div>}
            {slide.images && slide.images.length > 0 && <div className="mt-6">{renderEmbeddedImages()}</div>}
          </div>
        )}
      </div>
    </div>
  )
}
