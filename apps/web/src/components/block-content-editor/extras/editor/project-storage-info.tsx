"use client"

import { HardDrive } from "lucide-react"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"

interface ProjectStorageInfoProps {
  storageType: StorageType
}

export function ProjectStorageInfo({ storageType }: ProjectStorageInfoProps) {
  return (
    <div className="ml-6 flex items-center gap-4 pl-6 border-l border-gray-300 dark:border-gray-600">
      <div className="flex items-center gap-2 text-sm">
        <span className="text-gray-600 dark:text-gray-400">Storage:</span>
        <span className="font-medium text-gray-800 dark:text-gray-200 capitalize bg-gray-100 dark:bg-gray-800 px-3 py-1 flex items-center gap-1">
          <HardDrive className="h-3 w-3" />
          {storageType}
        </span>
      </div>
    </div>
  )
}
