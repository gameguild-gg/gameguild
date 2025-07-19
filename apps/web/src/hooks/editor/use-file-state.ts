import type React from "react"
import type { CodeFile } from "@/components/editor/ui/source-code/types"

interface UseFileStateProps {
  files: CodeFile[]
  setFiles: React.Dispatch<React.SetStateAction<CodeFile[]>>
}

interface UseFileStateReturn {
  toggleFileVisibility: (fileId: string) => void
  setMainFile: (fileId: string) => void
  setFileReadOnlyState: (fileId: string, state: "always" | "never" | null) => void
  isFileReadOnly: (file: CodeFile, globalReadOnly: boolean) => boolean
}

export function useFileState({ files, setFiles }: UseFileStateProps): UseFileStateReturn {
  // Toggle file visibility
  const toggleFileVisibility = (fileId: string) => {
    setFiles((prevFiles) =>
      prevFiles.map((file) => (file.id === fileId ? { ...file, isVisible: !file.isVisible } : file)),
    )
  }

  // Set main file
  const setMainFile = (fileId: string) => {
    // Modified to toggle main status - if the file is already main, unset it
    setFiles((prevFiles) =>
      prevFiles.map((file) => ({
        ...file,
        isMain: file.id === fileId ? !file.isMain : false,
      })),
    )
  }

  // Set file read-only state
  const setFileReadOnlyState = (fileId: string, state: "always" | "never" | null) => {
    setFiles((prevFiles) => prevFiles.map((file) => (file.id === fileId ? { ...file, readOnlyState: state } : file)))
  }

  // Check if file is read-only
  const isFileReadOnly = (file: CodeFile, globalReadOnly: boolean): boolean => {
    if (file.readOnlyState === "always") return true
    if (file.readOnlyState === "never") return false
    if (file.readOnlyState === "hidden") return globalReadOnly // Use global setting for hidden files
    return globalReadOnly // Use global setting
  }

  return {
    toggleFileVisibility,
    setMainFile,
    setFileReadOnlyState,
    isFileReadOnly,
  }
}
