import JSZip from "jszip"
import { DOMParser } from "@xmldom/xmldom"
import type { Slide, ParseProgress } from "../types"

export class PPTXParser {
  private callbacks: {
    onProgress: (progress: ParseProgress) => void
  }

  constructor(callbacks: { onProgress: (progress: ParseProgress) => void }) {
    this.callbacks = callbacks
  }

  async parse(file: File): Promise<Slide[]> {
    return new Promise(async (resolve, reject) => {
      this.callbacks.onProgress({ progress: 10, status: "Reading PowerPoint file..." })

      try {
        const arrayBuffer = await file.arrayBuffer()
        const zip = new JSZip()
        const zipContent = await zip.loadAsync(arrayBuffer)

        this.callbacks.onProgress({ progress: 20, status: "Extracting slide structure..." })

        // Extract images from media folder
        const mediaFiles: Record<string, string> = {}
        const mediaFolder = Object.keys(zipContent.files).filter((name) => name.startsWith("ppt/media/"))

        for (const mediaPath of mediaFolder) {
          const mediaFile = zipContent.files[mediaPath]
          if (mediaFile) {
            try {
              const imageData = await mediaFile.async("base64")
              const extension = mediaPath.split(".").pop()?.toLowerCase()
              const mimeType =
                extension === "png"
                  ? "image/png"
                  : extension === "jpg" || extension === "jpeg"
                    ? "image/jpeg"
                    : extension === "gif"
                      ? "image/gif"
                      : "image/png"
              const fileName = mediaPath.split("/").pop() || `image.${extension}`
              mediaFiles[fileName] = `data:${mimeType};base64,${imageData}`
            } catch (e) {
              console.warn(`Could not extract image ${mediaPath}:`, e)
            }
          }
        }

        this.callbacks.onProgress({ progress: 30, status: "Processing slides..." })

        // Get slide relationships
        const slideRels = zipContent.files["ppt/_rels/presentation.xml.rels"]
        if (!slideRels) {
          throw new Error("Invalid PowerPoint file structure")
        }

        const relsXml = await slideRels.async("text")
        const parser = new DOMParser()
        const relsDoc = parser.parseFromString(relsXml, "text/xml")

        // Extract slide file paths in correct order
        const relationships = Array.from(relsDoc.getElementsByTagName("Relationship"))
        const slideFiles = relationships
          .filter(
            (rel) => rel.getAttribute("Type")?.includes("slide") && !rel.getAttribute("Type")?.includes("slideMaster"),
          )
          .sort((a, b) => {
            const aTarget = a.getAttribute("Target") || ""
            const bTarget = b.getAttribute("Target") || ""
            const aNum = Number.parseInt(aTarget.match(/slide(\d+)\.xml/)?.[1] || "0")
            const bNum = Number.parseInt(bTarget.match(/slide(\d+)\.xml/)?.[1] || "0")
            return aNum - bNum
          })

        const extractedSlides: Slide[] = []

        for (let i = 0; i < slideFiles.length; i++) {
          const slideRel = slideFiles[i]
          const slideTarget = slideRel.getAttribute("Target")?.replace("../", "")

          if (!slideTarget) continue

          const slideFile = zipContent.files[`ppt/${slideTarget}`]
          if (!slideFile) continue

          this.callbacks.onProgress({
            progress: 40 + (i / slideFiles.length) * 50,
            status: `Processing slide ${i + 1} of ${slideFiles.length}...`,
          })

          const slideXml = await slideFile.async("text")
          const slideDoc = parser.parseFromString(slideXml, "text/xml")

          // Extract text content
          const shapes = Array.from(slideDoc.getElementsByTagName("p:sp"))
          let title = ""
          let content = ""
          let isFirstText = true

          shapes.forEach((shape) => {
            const textElements = Array.from(shape.getElementsByTagName("a:t"))
            textElements.forEach((textEl) => {
              const text = textEl.textContent?.trim() || ""
              if (!text) return

              if (isFirstText && text.length < 100) {
                title = text
                isFirstText = false
              } else {
                if (content) content += "\n"
                content += text
              }
            })
          })

          extractedSlides.push({
            id: `slide-${Date.now()}-${i}`,
            title: title || `Slide ${i + 1}`,
            content: content || "",
            layout: "title-content",
            theme: "light",
          })
        }

        this.callbacks.onProgress({ progress: 100, status: "Processing complete" })

        resolve(
          extractedSlides.length > 0
            ? extractedSlides
            : [
                {
                  id: `slide-${Date.now()}-0`,
                  title: "Imported Slide",
                  content: "PowerPoint content was imported. Some formatting may need manual adjustment.",
                  layout: "title-content",
                  theme: "light",
                },
              ],
        )
      } catch (error) {
        console.error("PPTX parsing error:", error)
        reject(
          new Error(`Failed to parse PowerPoint file: ${error instanceof Error ? error.message : "Unknown error"}`),
        )
      }
    })
  }
}
