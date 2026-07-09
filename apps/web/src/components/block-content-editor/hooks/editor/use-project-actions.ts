"use client"

import { useState } from "react"
import { toast } from "sonner"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"

interface StorageAdapter {
  load: (id: string) => Promise<ProjectData | null>
  save: (id: string, name: string, data: string, tags: string[], storageType?: "local" | "gameguild-cloud" | "google-drive") => Promise<void>
  delete: (id: string) => Promise<void>
}

interface UseProjectActionsProps {
  storageAdapter: StorageAdapter
  onProjectsListUpdate?: () => void
  onProjectUpdate?: () => Promise<void>
}

export function useProjectActions({
  storageAdapter,
  onProjectsListUpdate,
  onProjectUpdate
}: UseProjectActionsProps) {
  // Info dialog state
  const [infoDialogOpen, setInfoDialogOpen] = useState(false)
  const [projectToEdit, setProjectToEdit] = useState<ProjectData | null>(null)

  // Delete dialog state
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [projectToDelete, setProjectToDelete] = useState<{ id: string; name: string } | null>(null)

  // Handle opening info dialog
  const handleOpenInfo = (project: ProjectData) => {
    setProjectToEdit(project)
    setInfoDialogOpen(true)
  }

  // Handle saving project info
  const handleSaveInfo = async (
    projectId: string, 
    newName: string, 
    newTags: string[], 
    storageType: "local" | "gameguild-cloud" | "google-drive"
  ) => {
    const projectToUpdate = await storageAdapter.load(projectId)
    if (!projectToUpdate) {
      toast.error("Error finding project to update.")
      return
    }

    try {
      await storageAdapter.save(projectId, newName, projectToUpdate.data, newTags, storageType)
      toast.success("Projeto atualizado", {
        description: `"${newName}" foi atualizado com sucesso.`,
        duration: 2500,
        icon: "✏️",
      })
      
      if (onProjectsListUpdate) {
        onProjectsListUpdate()
      }
      if (onProjectUpdate) {
        await onProjectUpdate()
      }
    } catch (error) {
      console.error("Failed to save info:", error)
      toast.error("Erro ao atualizar projeto", {
        description: "Não foi possível salvar as alterações.",
        duration: 4000,
        icon: "❌",
      })
      throw error // re-throw to prevent dialog from closing
    }
  }

  // Handle delete confirmation
  const handleConfirmDelete = (projectId: string, projectName: string) => {
    setProjectToDelete({ id: projectId, name: projectName })
    setDeleteDialogOpen(true)
  }

  // Handle actual deletion
  const handleDelete = async () => {
    if (!projectToDelete) return

    try {
      await storageAdapter.delete(projectToDelete.id)
      
      toast.success("Projeto excluído", {
        description: `"${projectToDelete.name}" foi removido permanentemente`,
        duration: 3000,
        icon: "🗑️",
      })

      if (onProjectsListUpdate) {
        onProjectsListUpdate()
      }
      if (onProjectUpdate) {
        await onProjectUpdate()
      }
      
    } catch (error) {
      console.error("Delete error:", error)
      toast.error("Erro ao excluir projeto", {
        description: "Não foi possível excluir o projeto. Tente novamente.",
        duration: 4000,
        icon: "❌",
      })
    } finally {
      setProjectToDelete(null)
      setDeleteDialogOpen(false)
    }
  }

  // Handle download
  const handleDownload = async (
    projectId: string,
    projectName: string,
    projectData: string,
    projectTags: string[],
    createdAt: string,
    updatedAt: string,
    projectPreferences?: any
  ) => {
    try {
      // Dynamic imports to avoid issues if these aren't available
      const [{ HashManager }, { ProjectExporter }] = await Promise.all([
        import("@/components/block-content-editor/lib/sync/editor/hash-manager"),
        import("@/components/block-content-editor/lib/interopAdapter/project-exporter")
      ])

      toast.loading("Preparando download...", { id: `download-${projectId}` })

      // Generate hash for the project
      const hash = await HashManager.generateHash(projectData)

      // Prepare project data for export using ProjectExporter
      const exportProjectData = {
        id: projectId,
        name: projectName,
        data: projectData,
        tags: projectTags,
        size: new Blob([projectData]).size,
        createdAt: createdAt,
        updatedAt: updatedAt,
        hash: hash,
        storageType: "local" as const,
        preferences: projectPreferences
      }

      // Use ProjectExporter to create the ZIP file
      const zipBlob = await ProjectExporter.createZipFile(exportProjectData, hash)

      // Create download link
      const url = URL.createObjectURL(zipBlob)
      const link = document.createElement("a")
      link.href = url
      link.download = ProjectExporter.getDownloadFilename(exportProjectData)
      document.body.appendChild(link)
      link.click()

      // Cleanup
      document.body.removeChild(link)
      URL.revokeObjectURL(url)

      toast.success("Download concluído", {
        id: `download-${projectId}`,
        description: `"${projectName}" foi baixado com sucesso`,
        duration: 2500,
        icon: "📥",
      })
    } catch (error) {
      console.error("Download error:", error)
      toast.error("Erro no download", {
        id: `download-${projectId}`,
        description: "Não foi possível baixar o projeto. Tente novamente.",
        duration: 4000,
        icon: "❌",
      })
    }
  }

  return {
    // Info dialog
    infoDialogOpen,
    setInfoDialogOpen,
    projectToEdit,
    handleOpenInfo,
    handleSaveInfo,

    // Delete dialog
    deleteDialogOpen,
    setDeleteDialogOpen,
    projectToDelete,
    handleConfirmDelete,
    handleDelete,

    // Download
    handleDownload,
  }
}