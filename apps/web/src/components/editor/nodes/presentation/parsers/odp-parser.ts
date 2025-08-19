import JSZip from "jszip"
import { XMLParser } from "fast-xml-parser"
import type { Slide, ParseProgress } from "../types"

interface ODPParserCallbacks {
  onProgress?: (progress: ParseProgress) => void
}

export class ODPParser {
  private callbacks: ODPParserCallbacks

  constructor(callbacks: ODPParserCallbacks = {}) {
    this.callbacks = callbacks
  }

  private reportProgress(progress: number, status: string) {
    if (this.callbacks.onProgress) {
      this.callbacks.onProgress({ progress, status })
    }
  }

  async parse(file: File): Promise<Slide[]> {
    try {
      this.reportProgress(10, "Reading ODP file...")

      const zip = new JSZip()
      const zipContent = await zip.loadAsync(file)

      this.reportProgress(20, "Extracting content...")

      // Get content.xml which contains the presentation structure
      const contentXml = zipContent.files["content.xml"]
      if (!contentXml) {
        throw new Error("Invalid ODP file: content.xml not found")
      }

      const contentText = await contentXml.async("text")

      this.reportProgress(40, "Parsing XML content...")

      // Configure XML parser for ODF format
      const parser = new XMLParser({
        ignoreAttributes: false,
        attributeNamePrefix: "@_",
        textNodeName: "#text",
        parseAttributeValue: true,
        trimValues: true,
        parseTrueNumberOnly: false,
        arrayMode: (name: string) => {
          // These elements can appear multiple times
          return [
            "draw:page",
            "draw:frame",
            "draw:text-box",
            "text:p",
            "text:span",
            "draw:image",
            "draw:rect",
            "draw:circle",
            "draw:line",
          ].includes(name)
        },
      })

      const contentData = parser.parse(contentText)

      this.reportProgress(60, "Processing slides...")

      // Navigate to presentation slides
      const presentation = contentData["office:document-content"]?.["office:body"]?.["office:presentation"]
      if (!presentation) {
        throw new Error("Invalid ODP structure: presentation not found")
      }

      const pages = presentation["draw:page"] || []
      const pageArray = Array.isArray(pages) ? pages : [pages]

      this.reportProgress(70, "Extracting slide content...")

      // Extract images from the ZIP
      const images = await this.extractImages(zipContent)

      this.reportProgress(80, "Building slides...")

      const slides: Slide[] = []

      for (let i = 0; i < pageArray.length; i++) {
        const page = pageArray[i]
        const slide = await this.processSlide(page, i + 1, images)
        slides.push(slide)

        this.reportProgress(80 + (i / pageArray.length) * 15, `Processing slide ${i + 1}...`)
      }

      this.reportProgress(100, "ODP import completed successfully")

      return slides
    } catch (error) {
      console.error("Error parsing ODP file:", error)
      throw new Error(`Failed to parse ODP file: ${error instanceof Error ? error.message : "Unknown error"}`)
    }
  }

  private async extractImages(zip: JSZip): Promise<Map<string, string>> {
    const images = new Map<string, string>()

    // Look for images in Pictures/ directory
    const picturesFolder = Object.keys(zip.files).filter(
      (name) => name.startsWith("Pictures/") && /\.(jpg|jpeg|png|gif|bmp|svg)$/i.test(name),
    )

    for (const imagePath of picturesFolder) {
      try {
        const imageFile = zip.files[imagePath]
        if (imageFile) {
          const imageBlob = await imageFile.async("blob")
          const dataUrl = await this.blobToDataURL(imageBlob)

          // Store with both full path and just filename
          images.set(imagePath, dataUrl)
          const filename = imagePath.split("/").pop()
          if (filename) {
            images.set(filename, dataUrl)
          }
        }
      } catch (error) {
        console.warn(`Failed to extract image ${imagePath}:`, error)
      }
    }

    return images
  }

  private blobToDataURL(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader()
      reader.onload = () => resolve(reader.result as string)
      reader.onerror = reject
      reader.readAsDataURL(blob)
    })
  }

  private async processSlide(page: any, slideNumber: number, images: Map<string, string>): Promise<Slide> {
    const slideId = `odp-slide-${slideNumber}-${Date.now()}`

    // Extract slide name/title
    const slideName = page["@_draw:name"] || `Slide ${slideNumber}`

    // Extract frames (text boxes, images, shapes)
    const frames = page["draw:frame"] || []
    const frameArray = Array.isArray(frames) ? frames : [frames]

    let title = ""
    let content = ""
    const slideImages: any[] = []
    const slideShapes: any[] = []
    let notes = ""

    // Process each frame
    for (const frame of frameArray) {
      if (!frame) continue

      // Check if frame contains text
      const textBox = frame["draw:text-box"]
      if (textBox) {
        const extractedText = this.extractTextFromElement(textBox)

        // Determine if this is a title or content based on position/style
        const frameY = Number.parseFloat(frame["@_svg:y"]) || 0
        const frameHeight = Number.parseFloat(frame["@_svg:height"]) || 0

        // If it's in the upper portion, likely a title
        if (frameY < 2 && frameHeight < 3) {
          if (!title) {
            title = extractedText
          } else {
            content += (content ? "\n\n" : "") + extractedText
          }
        } else {
          content += (content ? "\n\n" : "") + extractedText
        }
      }

      // Check if frame contains an image
      const image = frame["draw:image"]
      if (image) {
        const href = image["@_xlink:href"]
        if (href) {
          // Remove leading "./" if present
          const imagePath = href.replace(/^\.\//, "")
          const imageData = images.get(imagePath) || images.get(`Pictures/${imagePath}`)

          if (imageData) {
            slideImages.push({
              id: `img-${slideImages.length}`,
              src: imageData,
              alt: frame["@_draw:name"] || `Image ${slideImages.length + 1}`,
              x: Number.parseFloat(frame["@_svg:x"]) || 0,
              y: Number.parseFloat(frame["@_svg:y"]) || 0,
              width: Number.parseFloat(frame["@_svg:width"]) || 100,
              height: Number.parseFloat(frame["@_svg:height"]) || 100,
            })
          }
        }
      }

      // Check for shapes (rectangles, circles, etc.)
      if (frame["draw:rect"] || frame["draw:circle"] || frame["draw:ellipse"]) {
        const shape = frame["draw:rect"] || frame["draw:circle"] || frame["draw:ellipse"]
        slideShapes.push({
          id: `shape-${slideShapes.length}`,
          type: frame["draw:rect"] ? "rectangle" : "circle",
          x: Number.parseFloat(frame["@_svg:x"]) || 0,
          y: Number.parseFloat(frame["@_svg:y"]) || 0,
          width: Number.parseFloat(frame["@_svg:width"]) || 100,
          height: Number.parseFloat(frame["@_svg:height"]) || 100,
          fill: this.extractFillColor(frame),
          stroke: this.extractStrokeColor(frame),
          strokeWidth: Number.parseFloat(frame["@_draw:stroke-width"]) || 1,
        })
      }
    }

    // Extract notes if present
    const notesElement = page["presentation:notes"]
    if (notesElement) {
      notes = this.extractTextFromElement(notesElement)
    }

    // Determine layout based on content
    let layout: any = "content-only"
    if (title && content) {
      layout = "title-content"
    } else if (title && !content) {
      layout = "title-slide"
    } else if (slideImages.length > 0 && !content) {
      layout = "image-only"
    } else if (slideImages.length > 0 && content) {
      layout = "two-column"
    }

    return {
      id: slideId,
      title: title || `Slide ${slideNumber}`,
      content: content || "",
      layout,
      theme: "light",
      notes,
      images: slideImages.length > 0 ? slideImages : undefined,
      shapes: slideShapes.length > 0 ? slideShapes : undefined,
    }
  }

  private extractTextFromElement(element: any): string {
    if (!element) return ""

    let text = ""

    // Handle paragraphs
    const paragraphs = element["text:p"] || []
    const paragraphArray = Array.isArray(paragraphs) ? paragraphs : [paragraphs]

    for (const paragraph of paragraphArray) {
      if (!paragraph) continue

      let paragraphText = ""

      // Handle direct text content
      if (paragraph["#text"]) {
        paragraphText += paragraph["#text"]
      }

      // Handle spans with formatting
      const spans = paragraph["text:span"] || []
      const spanArray = Array.isArray(spans) ? spans : [spans]

      for (const span of spanArray) {
        if (!span) continue

        if (span["#text"]) {
          const spanText = span["#text"]

          // Check for formatting attributes
          const styleName = span["@_text:style-name"]
          if (styleName && (styleName.includes("bold") || styleName.includes("Bold"))) {
            paragraphText += `**${spanText}**`
          } else if (styleName && (styleName.includes("italic") || styleName.includes("Italic"))) {
            paragraphText += `*${spanText}*`
          } else {
            paragraphText += spanText
          }
        }
      }

      if (paragraphText.trim()) {
        text += (text ? "\n" : "") + paragraphText.trim()
      }
    }

    return text.trim()
  }

  private extractFillColor(frame: any): string {
    // Try to extract fill color from style attributes
    const fillColor = frame["@_draw:fill-color"] || frame["@_fo:background-color"]
    if (fillColor) {
      return fillColor
    }

    // Default fill color
    return "#ffffff"
  }

  private extractStrokeColor(frame: any): string {
    // Try to extract stroke color from style attributes
    const strokeColor = frame["@_svg:stroke"] || frame["@_draw:stroke-color"]
    if (strokeColor && strokeColor !== "none") {
      return strokeColor
    }

    // Default stroke color
    return "#000000"
  }
}
