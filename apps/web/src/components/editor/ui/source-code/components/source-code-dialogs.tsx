"use client"

import type React from "react"
import type { CodeFile, LanguageType, ProgrammingLanguage } from "../types"
import { NewFileDialog } from "../dialogs/new-file-dialog"
import { ImportFileDialog } from "../dialogs/import-file-dialog"
import { ConfirmDialog } from "../dialogs/confirm-dialog"
import { RenameFileDialog } from "../dialogs/rename-file-dialog"
import { LanguageSettingsDialog } from "../dialogs/language-settings-dialog"

interface SourceCodeDialogsProps {
  // File dialog props
  showFileDialog: boolean
  setShowFileDialog: (show: boolean) => void
  newFileName: string
  setNewFileName: (name: string) => void
  newFileLanguage: LanguageType
  setNewFileLanguage: (lang: LanguageType) => void
  addNewFile: () => void
  getAllowedLanguageTypes: () => LanguageType[]
  getLanguageLabel: (lang: LanguageType) => string

  // Import dialog props
  showImportDialog: boolean
  setShowImportDialog: (show: boolean) => void
  importFileNames: string[]
  importContents: { name: string; content: string }[]
  handleFileUpload: (e: React.ChangeEvent<HTMLInputElement>) => void
  importFile: () => void
  fileInputRef: React.RefObject<HTMLInputElement>

  // Confirm dialog props
  showConfirmDialog: boolean
  setShowConfirmDialog: (show: boolean) => void
  activeFileId: string

  // Rename dialog props
  showRenameDialog: boolean
  setShowRenameDialog: (show: boolean) => void
  renameFileName: string
  setRenameFileName: (name: string) => void
  renameFileLanguage: LanguageType
  setRenameFileLanguage: (lang: LanguageType) => void
  fileToRename: string | null
  renameFile: () => void
  files: CodeFile[]

  // Language settings dialog props
  showLanguagesDialog: boolean
  setShowLanguagesDialog: (show: boolean) => void
  allowedLanguages: Record<LanguageType, boolean>
  setAllowedLanguages: (langs: Record<LanguageType, boolean>) => void
  selectedLanguage: ProgrammingLanguage
  setSelectedLanguage: (lang: ProgrammingLanguage) => void
  updateSourceCode: (data: any) => void
  isAutocompleteEnabled: boolean
  setIsAutocompleteEnabled: (enabled: boolean) => void
  hasConfiguredSettings: boolean
  setHasConfiguredSettings: (configured: boolean) => void
  activeEnvironments: Record<string, boolean>
  setActiveEnvironments: (envs: Record<string, boolean>) => void

  // Common props
  isPreview?: boolean
}

export function SourceCodeDialogs({
  // File dialog
  showFileDialog,
  setShowFileDialog,
  newFileName,
  setNewFileName,
  newFileLanguage,
  setNewFileLanguage,
  addNewFile,
  getAllowedLanguageTypes,
  getLanguageLabel,

  // Import dialog
  showImportDialog,
  setShowImportDialog,
  importFileNames,
  importContents,
  handleFileUpload,
  importFile,
  fileInputRef,

  // Confirm dialog
  showConfirmDialog,
  setShowConfirmDialog,
  activeFileId,

  // Rename dialog
  showRenameDialog,
  setShowRenameDialog,
  renameFileName,
  setRenameFileName,
  renameFileLanguage,
  setRenameFileLanguage,
  fileToRename,
  renameFile,
  files,

  // Language settings dialog
  showLanguagesDialog,
  setShowLanguagesDialog,
  allowedLanguages,
  setAllowedLanguages,
  selectedLanguage,
  setSelectedLanguage,
  updateSourceCode,
  isAutocompleteEnabled,
  setIsAutocompleteEnabled,
  hasConfiguredSettings,
  setHasConfiguredSettings,
  activeEnvironments,
  setActiveEnvironments,

  // Common
  isPreview = false,
}: SourceCodeDialogsProps) {
  return (
    <>
      <NewFileDialog
        showFileDialog={showFileDialog}
        setShowFileDialog={setShowFileDialog}
        newFileName={newFileName}
        setNewFileName={setNewFileName}
        newFileLanguage={newFileLanguage}
        setNewFileLanguage={setNewFileLanguage}
        addNewFile={addNewFile}
        getAllowedLanguageTypes={getAllowedLanguageTypes}
        getLanguageLabel={getLanguageLabel}
        isPreview={isPreview}
      />

      <ImportFileDialog
        showImportDialog={showImportDialog}
        setShowImportDialog={setShowImportDialog}
        importFileNames={importFileNames}
        importContents={importContents}
        handleFileUpload={handleFileUpload}
        importFile={importFile}
        fileInputRef={fileInputRef}
        isPreview={isPreview}
      />

      <ConfirmDialog
        showConfirmDialog={showConfirmDialog}
        setShowConfirmDialog={setShowConfirmDialog}
        activeFileId={activeFileId}
        isPreview={isPreview}
      />

      <RenameFileDialog
        showRenameDialog={showRenameDialog}
        setShowRenameDialog={setShowRenameDialog}
        renameFileName={renameFileName}
        setRenameFileName={setRenameFileName}
        renameFileLanguage={renameFileLanguage}
        setRenameFileLanguage={setRenameFileLanguage}
        fileToRename={fileToRename}
        renameFile={renameFile}
        files={files}
        getAllowedLanguageTypes={getAllowedLanguageTypes}
        getLanguageLabel={getLanguageLabel}
        isPreview={isPreview}
      />

      <LanguageSettingsDialog
        showLanguagesDialog={showLanguagesDialog}
        setShowLanguagesDialog={(show) => {
          setShowLanguagesDialog(show)
          if (!show && !hasConfiguredSettings) {
            setHasConfiguredSettings(true)
            updateSourceCode({
              hasConfiguredSettings: true,
              allowedLanguages: allowedLanguages,
              activeEnvironments: activeEnvironments,
            })
          }
        }}
        allowedLanguages={allowedLanguages}
        setAllowedLanguages={setAllowedLanguages}
        selectedLanguage={selectedLanguage}
        setSelectedLanguage={setSelectedLanguage}
        getLanguageLabel={getLanguageLabel}
        updateSourceCode={(data) => {
          updateSourceCode({
            ...data,
            activeEnvironments: activeEnvironments,
          })
        }}
        isAutocompleteEnabled={isAutocompleteEnabled}
        setIsAutocompleteEnabled={setIsAutocompleteEnabled}
        isPreview={isPreview}
        isInitialSetup={!hasConfiguredSettings}
        activeEnvironments={activeEnvironments}
        setActiveEnvironments={setActiveEnvironments}
      />
    </>
  )
}
