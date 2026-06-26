"use client"

/**
 * useProjectDbInit
 *
 * Owns the {@link EnhancedStorageAdapter} singleton ref and tracks DB
 * initialization. The `readOnlyRef` is exposed so the editor page can
 * suppress auto-save while showing historical commits.
 */

import { useEffect, useRef, useState } from "react"
import { toast } from "sonner"
import { EnhancedStorageAdapter } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"

export interface UseProjectDbInitReturn {
  db: EnhancedStorageAdapter
  isDbInitialized: boolean
  readOnlyRef: React.MutableRefObject<boolean>
}

export function useProjectDbInit(): UseProjectDbInitReturn {
  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())
  const [isDbInitialized, setIsDbInitialized] = useState(false)
  const readOnlyRef = useRef(false)

  useEffect(() => {
    const initDB = async () => {
      try {
        await dbStorage.current.init()
        setIsDbInitialized(true)
      } catch (error) {
        console.error("Failed to initialize IndexedDB:", error)
        toast.error("Storage error", {
          description: "Unable to initialize database. Some features may not work.",
          duration: 5000,
          icon: "⚠️",
        })
      }
    }
    initDB()
  }, [])

  return { db: dbStorage.current, isDbInitialized, readOnlyRef }
}
