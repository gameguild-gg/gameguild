"use client"

import type React from "react"
import { useState } from "react"
import { toast } from "sonner"
import { Cloud } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { useGoogleDriveAuth } from "@/components/block-content-editor/hooks/editor/use-google-drive-auth"
import { GoogleDriveAuthDialog } from "@/components/block-content-editor/extras/editor/google-drive-auth-dialog"

interface ProjectPickerShellProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description?: string
  trigger?: React.ReactNode
  /** Filters area (typically <ProjectSearchFilters />) */
  filters: React.ReactNode
  /** Main list area (typically <ProjectList />) */
  list: React.ReactNode
  /** Pagination area (typically <ProjectPagination />) */
  pagination: React.ReactNode
  /** Left-side footer actions */
  footerLeft?: React.ReactNode
  /** Right-side footer actions (always rendered) */
  footerRight: React.ReactNode
  /** Optional hook fired after a successful Google Drive connection */
  onAuthSuccess?: () => void
  /** Toast shown after auth */
  authSuccessToast?: { title: string; description: string }
}

/**
 * Shared chrome for project picker dialogs (Open Project / Open Project for Preview).
 * Houses the layout, header with Google Drive auth control, body slots and footer slots
 * so callers only need to provide the variable content.
 */
export function ProjectPickerShell({
  open,
  onOpenChange,
  title,
  description,
  trigger,
  filters,
  list,
  pagination,
  footerLeft,
  footerRight,
  onAuthSuccess,
  authSuccessToast,
}: ProjectPickerShellProps) {
  const [googleDriveAuthDialogOpen, setGoogleDriveAuthDialogOpen] = useState(false)
  const { isAuthenticated, isLoading, signOut, refreshAuthState } = useGoogleDriveAuth()

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      {trigger && <DialogTrigger asChild>{trigger}</DialogTrigger>}

      <DialogContent
        className="max-w-2xl lg:max-w-4xl w-full h-[92vh] p-0 gap-0 flex flex-col overflow-hidden rounded-xl border-border/60 shadow-2xl"
        onInteractOutside={(e) => e.preventDefault()}
      >
        {/* Header */}
        <DialogHeader className="shrink-0 border-b border-border/40 bg-muted/20 px-6 py-4">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0">
              <DialogTitle className="text-lg font-semibold tracking-tight">{title}</DialogTitle>
              {description && (
                <p className="mt-0.5 text-sm text-muted-foreground">{description}</p>
              )}
            </div>

            {/* Google Drive Auth */}
            <div className="shrink-0">
              {isAuthenticated ? (
                <div className="flex items-center gap-2 rounded-full border border-emerald-500/30 bg-emerald-500/10 pl-3 pr-1 py-1">
                  <div className="flex items-center gap-1.5 text-xs font-medium text-emerald-600 dark:text-emerald-400">
                    <Cloud className="h-3.5 w-3.5" />
                    <span>Google Drive</span>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={signOut}
                    className="h-6 px-2 text-[11px] text-muted-foreground hover:text-red-600"
                    title="Disconnect Google Drive"
                  >
                    Disconnect
                  </Button>
                </div>
              ) : (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setGoogleDriveAuthDialogOpen(true)}
                  disabled={isLoading}
                  className="h-8 gap-1.5 rounded-full text-xs"
                  title="Connect to Google Drive to access your cloud projects"
                >
                  <Cloud className="h-3.5 w-3.5" />
                  {isLoading ? "Connecting…" : "Connect Google Drive"}
                </Button>
              )}
            </div>
          </div>
        </DialogHeader>

        {/* Body */}
        <div className="flex-1 min-h-0 flex flex-col gap-4 px-6 py-5">
          <div className="shrink-0">{filters}</div>

          <div className="flex-1 min-h-0 overflow-y-auto -mx-1 px-1">
            {list}
          </div>

          <div className="shrink-0 flex items-center justify-center pt-1">
            {pagination}
          </div>
        </div>

        {/* Footer */}
        <div className="shrink-0 border-t border-border/40 bg-muted/20 px-6 py-3.5 flex items-center justify-between gap-3">
          <div className="flex items-center gap-2">{footerLeft}</div>
          <div className="flex items-center gap-2">{footerRight}</div>
        </div>
      </DialogContent>

      <GoogleDriveAuthDialog
        open={googleDriveAuthDialogOpen}
        onOpenChange={setGoogleDriveAuthDialogOpen}
        onAuthSuccess={() => {
          setGoogleDriveAuthDialogOpen(false)
          refreshAuthState()
          onAuthSuccess?.()
          if (authSuccessToast) {
            toast.success(authSuccessToast.title, {
              description: authSuccessToast.description,
              duration: 3000,
              icon: "☁️",
            })
          }
        }}
      />
    </Dialog>
  )
}
