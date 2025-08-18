"use client"

import { useState, useCallback } from "react"

export interface CompressionOptions {
  quality: number // 0.1 to 1.0
  maxWidth?: number
  maxHeight?: number
  format: "webp" | "jpeg" | "png"
}

export interface CompressionResult {
  originalFile: File
  compressedBlob: Blob
  originalSize: number
  compressedSize: number
  compressionRatio: number
  dataUrl: string
}

export interface ImageCompressorProps {
  file: File
  options: CompressionOptions
  onCompressionComplete: (result: CompressionResult) => void
  onError: (error: string) => void
}

export const useImageCompressor = () => {
  const [isCompressing, setIsCompressing] = useState(false)

  const compressImage = useCallback(async (file: File, options: CompressionOptions): Promise<CompressionResult> => {
    setIsCompressing(true)

    try {
      return new Promise((resolve, reject) => {
        const canvas = document.createElement("canvas")
        const ctx = canvas.getContext("2d")
        const img = new Image()

        img.onload = () => {
          // Calculate new dimensions
          let { width, height } = img
          const { maxWidth = 1920, maxHeight = 1080 } = options

          if (width > maxWidth || height > maxHeight) {
            const ratio = Math.min(maxWidth / width, maxHeight / height)
            width *= ratio
            height *= ratio
          }

          canvas.width = width
          canvas.height = height

          // Draw and compress
          ctx?.drawImage(img, 0, 0, width, height)

          canvas.toBlob(
            (blob) => {
              if (!blob) {
                reject(new Error("Failed to compress image"))
                return
              }

              const compressionRatio = ((file.size - blob.size) / file.size) * 100
              const dataUrl = canvas.toDataURL(`image/${options.format}`, options.quality)

              resolve({
                originalFile: file,
                compressedBlob: blob,
                originalSize: file.size,
                compressedSize: blob.size,
                compressionRatio,
                dataUrl,
              })
            },
            `image/${options.format}`,
            options.quality,
          )
        }

        img.onerror = () => reject(new Error("Failed to load image"))
        img.src = URL.createObjectURL(file)
      })
    } catch (error) {
      throw new Error(`Compression failed: ${error}`)
    } finally {
      setIsCompressing(false)
    }
  }, [])

  const getRecommendedQuality = useCallback((fileSize: number): number => {
    // Recommend compression for files larger than 500KB
    if (fileSize > 5 * 1024 * 1024) return 0.7 // Large files (>5MB)
    if (fileSize > 2 * 1024 * 1024) return 0.8 // Medium files (>2MB)
    if (fileSize > 500 * 1024) return 0.85 // Small files (>500KB)
    return 0.9 // Very small files
  }, [])

  const shouldRecommendCompression = useCallback((fileSize: number): boolean => {
    return fileSize > 500 * 1024 // Recommend for files > 500KB
  }, [])

  const formatFileSize = useCallback((bytes: number): string => {
    if (bytes === 0) return "0 Bytes"
    const k = 1024
    const sizes = ["Bytes", "KB", "MB", "GB"]
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Number.parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i]
  }, [])

  return {
    compressImage,
    isCompressing,
    getRecommendedQuality,
    shouldRecommendCompression,
    formatFileSize,
  }
}

// Default compression options
export const DEFAULT_COMPRESSION_OPTIONS: CompressionOptions = {
  quality: 0.85,
  maxWidth: 1920,
  maxHeight: 1080,
  format: "webp",
}

// High quality options for automatic compression
export const AUTO_COMPRESSION_OPTIONS: CompressionOptions = {
  quality: 0.9,
  maxWidth: 2048,
  maxHeight: 2048,
  format: "webp",
}
