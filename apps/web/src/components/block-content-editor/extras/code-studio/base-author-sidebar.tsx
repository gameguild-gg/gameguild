"use client"

import { ComponentProps } from "react"
import { FolderTree } from "lucide-react"
import { FileExplorer } from "./file-system/file-explorer"

/**
 * Author-only sidebar for the Base display.
 *
 * Base is the embed-sized canvas students will see. Because some compact
 * templates hide the file explorer panel from students, authors still need a
 * reliable place to manage files. This sidebar is rendered to the left of the
 * canvas only when:
 *   - the active display is Base (first display), and
 *   - layout edit mode is enabled.
 *
 * It is never shown to students and never visible on secondary displays.
 *
 * Templates are no longer listed here — they live on the inline editor toolbar
 * (DisplayManager) on the Title row.
 */
export interface BaseAuthorSidebarProps {
  fileExplorerProps: ComponentProps<typeof FileExplorer>
}

export function BaseAuthorSidebar({ fileExplorerProps }: BaseAuthorSidebarProps) {
  return (
    <aside className="flex flex-col w-96 shrink-0 h-full overflow-hidden border border-blue-500/30 rounded-lg bg-white dark:bg-gray-900">
      <header className="flex items-center gap-1.5 px-2 pt-2 pb-1 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400 shrink-0">
        <FolderTree className="h-3 w-3" />
        Files
      </header>
      <div className="flex-1 min-h-0 overflow-hidden">
        <FileExplorer {...fileExplorerProps} isPreview={false} />
      </div>
    </aside>
  )
}
