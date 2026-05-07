import React from 'react'
import { 
  FileText, 
  Image as ImageIcon,
  Video,
  Music,
  File,
  HardDrive,
  Cloud,
  Package,
  type LucideIcon
} from 'lucide-react'

export const getMimeTypeIcon = (mimeType: string): LucideIcon => {
  if (mimeType.startsWith('image/')) return ImageIcon
  if (mimeType.startsWith('video/')) return Video
  if (mimeType.startsWith('audio/')) return Music
  if (mimeType.startsWith('text/')) return FileText
  return File
}

export const getMimeTypeBadgeColor = (mimeType: string): string => {
  if (mimeType.startsWith('image/')) return 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
  if (mimeType.startsWith('video/')) return 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200'
  if (mimeType.startsWith('audio/')) return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
  if (mimeType.startsWith('text/')) return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200'
  return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200'
}

export const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${(bytes / Math.pow(k, i)).toFixed(1)} ${sizes[i]}`
}

export const getStorageIcon = (storageType?: string): LucideIcon => {
  switch (storageType) {
    case 'gameguild-cloud':
      return Cloud
    case 'google-drive':
      return Package
    case 'local':
    default:
      return HardDrive
  }
}
