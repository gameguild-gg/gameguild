"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import React, { useState } from 'react'
import { AssetPickerDialog, AssetsProvider } from "@game-guild/assets/react"
import {
  ManagerLayout,
  ManagerFilters,
  GridView,
  ListView,
  type FilterConfig,
} from "@/components/block-content-editor/extras/manager-page"
import { DeleteConfirmDialog } from "@/components/block-content-editor/extras/dialogs/delete-confirm-dialog"
import { InfoDialog } from "@/components/block-content-editor/extras/editor/info-dialog"
import { CreateProjectDialog } from "@/components/block-content-editor/extras/editor/create-project-dialog"
import { ProjectPagination } from "@/components/block-content-editor/extras/project-dialog/project-pagination"

import { useHomeStorage } from "@/components/block-content-editor/hooks/useHomeStorage"
import { useProjectManager } from "@/components/block-content-editor/hooks/useProjectManager"
import { useAssetManager } from "@/components/block-content-editor/hooks/useAssetManager"
import { useCollectionManager } from "@/components/block-content-editor/hooks/useCollectionManager"

export default function HomePage() {
  // Active context/view
  const [activeContext, setActiveContext] = useState<'projects' | 'assets' | 'collections'>('projects')

  // View state
  const [viewMode, setViewMode] = useState<'list' | 'grid'>('grid')
  const [gridColumns, setGridColumns] = useState(4)
  const [listColumns, setListColumns] = useState(1)

  // Unified filters
  const [filters, setFilters] = useState<FilterConfig>({
    searchTerm: '',
    tags: [],
    tagFilterMode: 'all',
    storageType: 'all',
    mimeTypes: [],
    assetType: 'all',
    projectFilter: 'all',
    usageFilter: 'all',
    sortOrder: []
  })

  const [itemsPerPage, setItemsPerPage] = useState(24)
  const [currentPage, setCurrentPage] = useState(1)

  // Storage hook
  const { isDbInitialized, availableTags, loadAvailableTags, storageAdapter, generateProjectId } = useHomeStorage()

  // Project manager hook
  const {
    projectCards,
    projectPrimaryActions,
    projectSecondaryActions,
    projectActions,
    filteredCount: projectFilteredCount,
    additionalFilteredProjects,
    createDialogOpen,
    setCreateDialogOpen,
    handleCreateNewProject,
    handleProjectCreate,
    refreshProjects,
  } = useProjectManager({
    isDbInitialized,
    storageAdapter,
    availableTags,
    loadAvailableTags,
    filters,
    currentPage,
    itemsPerPage,
  })

  // Asset manager hook
  const {
    assetCards,
    assetPrimaryActions,
    assetSecondaryActions,
    filteredCount: assetFilteredCount,
    uploadDialogOpen,
    setUploadDialogOpen,
    assetToDelete,
    setAssetToDelete,
    assetToEdit,
    setAssetToEdit,
    newAssetName,
    setNewAssetName,
    handleConfirmAssetDelete,
    handleConfirmAssetEdit,
    handleUploadComplete,
  } = useAssetManager({
    isDbInitialized,
    activeContext,
    filters,
    currentPage,
    itemsPerPage,
    additionalFilteredProjects,
  })

  // Collection manager hook
  const {
    collectionCards,
    collectionPrimaryActions,
    collectionSecondaryActions,
    filteredCount: collectionFilteredCount,
    collectionToDelete,
    setCollectionToDelete,
    collectionToEdit,
    setCollectionToEdit,
    newCollectionName,
    setNewCollectionName,
    handleConfirmCollectionDelete,
    handleConfirmCollectionEdit,
  } = useCollectionManager({
    isDbInitialized,
    activeContext,
    filters,
    currentPage,
    itemsPerPage,
  })

  const totalItems = activeContext === 'projects' ? projectFilteredCount : activeContext === 'assets' ? assetFilteredCount : collectionFilteredCount
  const totalPages = Math.ceil(totalItems / itemsPerPage)

  return (
    <AssetsProvider>
      <ManagerLayout
        activeContext={activeContext}
        viewMode={viewMode}
        gridColumns={gridColumns}
        listColumns={listColumns}
        onContextChange={setActiveContext}
        onViewModeChange={setViewMode}
        onGridColumnsChange={setGridColumns}
        onListColumnsChange={setListColumns}
        onCreateNew={() => {
          if (activeContext === 'projects') {
            handleCreateNewProject()
          } else {
            setUploadDialogOpen(true)
          }
        }}
        filterSection={
          <ManagerFilters
            filters={filters}
            onFilterChange={(newFilters) => setFilters({ ...filters, ...newFilters })}
            availableTags={availableTags}
            availableProjects={additionalFilteredProjects.map(p => ({ id: p.id, name: p.name }))}
            contextType={activeContext}
            itemsPerPage={itemsPerPage}
            onItemsPerPageChange={setItemsPerPage}
          />
        }
        paginationSection={
          totalPages > 1 ? (
            <ProjectPagination
              currentPage={currentPage}
              totalProjects={totalItems}
              itemsPerPage={itemsPerPage}
              onPageChange={setCurrentPage}
            />
          ) : undefined
        }
      >
        {!isDbInitialized ? (
          <div className="flex items-center justify-center h-64">
            <div className="text-center">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-4"></div>
              <p className="text-gray-500 dark:text-gray-400">
                Loading {activeContext === 'projects' ? 'projects' : activeContext === 'assets' ? 'assets' : 'collections'}...
              </p>
            </div>
          </div>
        ) : activeContext === 'projects' ? (
          viewMode === 'grid' ? (
            <GridView
              cards={projectCards}
              columns={gridColumns}
              viewMode="grid"
              primaryActions={projectPrimaryActions}
              secondaryActions={projectSecondaryActions}
            />
          ) : (
            <ListView
              cards={projectCards}
              columns={listColumns}
              viewMode="list"
              primaryActions={projectPrimaryActions}
              secondaryActions={projectSecondaryActions}
            />
          )
        ) : activeContext === 'assets' ? (
          viewMode === 'grid' ? (
            <GridView
              cards={assetCards}
              columns={gridColumns}
              viewMode="grid"
              primaryActions={assetPrimaryActions}
              secondaryActions={assetSecondaryActions}
            />
          ) : (
            <ListView
              cards={assetCards}
              columns={listColumns}
              viewMode="list"
              primaryActions={assetPrimaryActions}
              secondaryActions={assetSecondaryActions}
            />
          )
        ) : (
          viewMode === 'grid' ? (
            <GridView
              cards={collectionCards}
              columns={gridColumns}
              viewMode="grid"
              primaryActions={collectionPrimaryActions}
              secondaryActions={collectionSecondaryActions}
            />
          ) : (
            <ListView
              cards={collectionCards}
              columns={listColumns}
              viewMode="list"
              primaryActions={collectionPrimaryActions}
              secondaryActions={collectionSecondaryActions}
            />
          )
        )}
      </ManagerLayout>

      {/* Create New Project Dialog */}
      <CreateProjectDialog
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        isDbInitialized={isDbInitialized}
        storageAdapter={storageAdapter}
        availableTags={availableTags}
        onProjectCreate={handleProjectCreate}
        onProjectsListUpdate={refreshProjects}
        onAvailableTagsUpdate={loadAvailableTags}
        generateProjectId={generateProjectId}
      />

      {/* Project Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        open={projectActions.deleteDialogOpen}
        onOpenChange={projectActions.setDeleteDialogOpen}
        itemName={projectActions.projectToDelete?.name}
        itemType="project"
        onConfirm={projectActions.handleDelete}
        title="Confirm Deletion"
      />

      {/* Asset Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        open={!!assetToDelete}
        onOpenChange={(open) => !open && setAssetToDelete(null)}
        itemName={assetToDelete?.name}
        itemType="asset"
        onConfirm={handleConfirmAssetDelete}
        title="Confirm Asset Deletion"
        description={
          assetToDelete?.projects && assetToDelete.projects.length > 0
            ? `This asset is used by ${assetToDelete.projects.length} project${assetToDelete.projects.length > 1 ? 's' : ''}${assetToDelete.projects.length <= 5 ? ': ' + assetToDelete.projects.map(pid => additionalFilteredProjects.find(p => p.id === pid)?.name || pid).join(', ') : ''}. Deleting it will affect all projects that use it.`
            : `Are you sure you want to delete "${assetToDelete?.name}"? This action cannot be undone.`
        }
      />

      {/* Asset Edit Dialog */}
      <Dialog open={!!assetToEdit} onOpenChange={(open) => !open && setAssetToEdit(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Rename Asset</DialogTitle>
            <DialogDescription>
              Enter a new name for this asset. The file extension will be preserved.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label htmlFor="asset-name" className="text-sm font-medium">
                Asset Name
              </label>
              <Input
                id="asset-name"
                value={newAssetName}
                onChange={(e) => setNewAssetName(e.target.value)}
                placeholder="Enter asset name"
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && newAssetName.trim()) {
                    handleConfirmAssetEdit()
                  }
                }}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setAssetToEdit(null)
                setNewAssetName("")
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleConfirmAssetEdit}
              disabled={!newAssetName.trim()}
            >
              Rename
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Info Dialog */}
      <InfoDialog
        open={projectActions.infoDialogOpen}
        onOpenChange={projectActions.setInfoDialogOpen}
        project={projectActions.projectToEdit}
        onSave={projectActions.handleSaveInfo}
        availableTags={availableTags}
        storageAdapter={storageAdapter}
      />

      {/* Upload Assets Dialog */}
      <AssetPickerDialog
        open={uploadDialogOpen}
        onOpenChange={setUploadDialogOpen}
        onSelect={() => void handleUploadComplete()}
        title="Upload Assets"
        multiple={true}
      />

      {/* Collection Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        open={!!collectionToDelete}
        onOpenChange={(open) => !open && setCollectionToDelete(null)}
        itemName={collectionToDelete?.name}
        itemType="collection"
        onConfirm={handleConfirmCollectionDelete}
        title="Confirm Collection Deletion"
        description={`Are you sure you want to delete "${collectionToDelete?.name}"? This will not delete the individual assets, only the collection. This action cannot be undone.`}
      />

      {/* Collection Edit Dialog */}
      <Dialog open={!!collectionToEdit} onOpenChange={(open) => !open && setCollectionToEdit(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Rename Collection</DialogTitle>
            <DialogDescription>
              Enter a new name for this collection.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label htmlFor="collection-name" className="text-sm font-medium">
                Collection Name
              </label>
              <Input
                id="collection-name"
                value={newCollectionName}
                onChange={(e) => setNewCollectionName(e.target.value)}
                placeholder="Enter collection name"
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && newCollectionName.trim()) {
                    handleConfirmCollectionEdit()
                  }
                }}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setCollectionToEdit(null)
                setNewCollectionName("")
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleConfirmCollectionEdit}
              disabled={!newCollectionName.trim()}
            >
              Rename
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </AssetsProvider>
  )
}
