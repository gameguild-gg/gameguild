"use client"

export interface WebPConversionOptions {
  quality: number // 0.1 to 1.0
  lossless?: boolean
  maxWidth?: number
  maxHeight?: number
  maintainAspectRatio?: boolean
  backgroundColor?: string
}

export interface WebPConversionResult {
  success: boolean
  blob?: Blob
  dataUrl?: string
  originalSize: number
  compressedSize: number
  compressionRatio: number
  error?: string
}

export class WebPConverter {
  private static isWebPSupported(): boolean {
    const canvas = document.createElement("canvas")
    canvas.width = 1
    canvas.height = 1
    return canvas.toDataURL("image/webp").indexOf("data:image/webp") === 0
  }

  static async convertToWebP(
    file: File,
    options: WebPConversionOptions = { quality: 0.85 },
  ): Promise<WebPConversionResult> {
    if (!this.isWebPSupported()) {
      return {
        success: false,
        originalSize: file.size,
        compressedSize: 0,
        compressionRatio: 0,
        error: "WebP format is not supported in this browser",
      }
    }

    try {
      const result = await this.processImage(file, options)
      return result
    } catch (error) {
      return {
        success: false,
        originalSize: file.size,
        compressedSize: 0,
        compressionRatio: 0,
        error: error instanceof Error ? error.message : "Unknown error occurred",
      }
    }
  }

  private static processImage(file: File, options: WebPConversionOptions): Promise<WebPConversionResult> {
    return new Promise((resolve, reject) => {
      const img = new Image()
      const canvas = document.createElement("canvas")
      const ctx = canvas.getContext("2d")

      if (!ctx) {
        reject(new Error("Could not get canvas context"))
        return
      }

      img.onload = () => {
        try {
          // Calculate dimensions
          const dimensions = this.calculateDimensions(img.width, img.height, options)

          canvas.width = dimensions.width
          canvas.height = dimensions.height

          // Set background color if specified
          if (options.backgroundColor) {
            ctx.fillStyle = options.backgroundColor
            ctx.fillRect(0, 0, canvas.width, canvas.height)
          }

          // Draw image
          ctx.drawImage(img, 0, 0, dimensions.width, dimensions.height)

          // Convert to WebP
          const mimeType = options.lossless ? "image/webp" : "image/webp"
          const quality = options.lossless ? undefined : options.quality

          canvas.toBlob(
            (blob) => {
              if (!blob) {
                reject(new Error("Failed to convert image to WebP"))
                return
              }

              const compressionRatio = ((file.size - blob.size) / file.size) * 100
              const dataUrl = canvas.toDataURL(mimeType, quality)

              resolve({
                success: true,
                blob,
                dataUrl,
                originalSize: file.size,
                compressedSize: blob.size,
                compressionRatio: Math.max(0, compressionRatio),
              })
            },
            mimeType,
            quality,
          )
        } catch (error) {
          reject(error)
        }
      }

      img.onerror = () => {
        reject(new Error("Failed to load image"))
      }

      // Create object URL for the image
      const objectUrl = URL.createObjectURL(file)
      img.src = objectUrl

      // Clean up object URL after image loads
      const originalOnLoad = img.onload
      img.onload = (event) => {
        URL.revokeObjectURL(objectUrl)
        if (originalOnLoad) {
          originalOnLoad.call(img, event)
        }
      }
    })
  }

  private static calculateDimensions(
    originalWidth: number,
    originalHeight: number,
    options: WebPConversionOptions,
  ): { width: number; height: number } {
    let { width, height } = { width: originalWidth, height: originalHeight }
    const { maxWidth, maxHeight, maintainAspectRatio = true } = options

    if (maxWidth && width > maxWidth) {
      if (maintainAspectRatio) {
        height = (height * maxWidth) / width
      }
      width = maxWidth
    }

    if (maxHeight && height > maxHeight) {
      if (maintainAspectRatio) {
        width = (width * maxHeight) / height
      }
      height = maxHeight
    }

    return {
      width: Math.round(width),
      height: Math.round(height),
    }
  }

  static getOptimalQuality(fileSize: number): number {
    // Return optimal quality based on file size
    if (fileSize > 10 * 1024 * 1024) return 0.7 // >10MB
    if (fileSize > 5 * 1024 * 1024) return 0.75 // >5MB
    if (fileSize > 2 * 1024 * 1024) return 0.8 // >2MB
    if (fileSize > 1 * 1024 * 1024) return 0.85 // >1MB
    return 0.9 // <1MB
  }

  static shouldCompress(file: File): boolean {
    // Recommend compression for files larger than 500KB
    return file.size > 500 * 1024
  }

  static formatFileSize(bytes: number): string {
    if (bytes === 0) return "0 Bytes"
    const k = 1024
    const sizes = ["Bytes", "KB", "MB", "GB"]
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Number.parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i]
  }

  static async batchConvert(
    files: File[],
    options: WebPConversionOptions,
    onProgress?: (completed: number, total: number) => void,
  ): Promise<WebPConversionResult[]> {
    const results: WebPConversionResult[] = []

    for (let i = 0; i < files.length; i++) {
      const result = await this.convertToWebP(files[i], options)
      results.push(result)

      if (onProgress) {
        onProgress(i + 1, files.length)
      }
    }

    return results
  }
}

// Preset configurations
export const WEBP_PRESETS = {
  HIGH_QUALITY: {
    quality: 0.95,
    maxWidth: 2048,
    maxHeight: 2048,
    maintainAspectRatio: true,
  } as WebPConversionOptions,

  BALANCED: {
    quality: 0.85,
    maxWidth: 1920,
    maxHeight: 1080,
    maintainAspectRatio: true,
  } as WebPConversionOptions,

  COMPRESSED: {
    quality: 0.75,
    maxWidth: 1280,
    maxHeight: 720,
    maintainAspectRatio: true,
  } as WebPConversionOptions,

  THUMBNAIL: {
    quality: 0.8,
    maxWidth: 400,
    maxHeight: 400,
    maintainAspectRatio: true,
  } as WebPConversionOptions,
}