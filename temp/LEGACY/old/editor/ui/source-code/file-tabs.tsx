'use client';
import { useState } from 'react';
import { Check, ChevronDown, Code, EyeOff, Lock, Moon, Play, Plus, Settings, Sun, Trash2, Unlock, Upload } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import type { FileTabsProps } from './types';

export function FileTabs({
  files,
  activeFileId,
  setActiveFileId,
  isDarkTheme,
  setIsDarkTheme,
  isEditing,
  showFileDialog = () => {},
  showRenameDialog = () => {},
  showImportDialog = () => {},
  showConfirmDialog = () => {},
  showLanguagesDialog = () => {},
  toggleFileVisibility = () => {},
  setMainFile = () => {},
  deleteFile = () => {},
  getBaseName = (name: string) => name,
  getExtensionForSelectedLanguage = () => 'js',
  draggedFileId,
  setDraggedFileId = () => {},
  dragOverFileId,
  setDragOverFileId = () => {},
  reorderFiles = () => {},
  getFileIcon = () => null,
  getStateIcon = () => null,
  setFileReadOnlyState = () => {},
  showBasicFileActionsInReadMode = true,
  showFilePropertiesInReadMode = false,
  setShowBasicFileActionsInReadMode = () => {},
  setShowFilePropertiesInReadMode = () => {},
  showDeleteConfirmDialog: externalShowDeleteConfirmDialog,
  setShowDeleteConfirmDialog: externalSetShowDeleteConfirmDialog,
}: FileTabsProps) {
  // Use local state if props are not provided
  const [localShowDeleteConfirmDialog, setLocalShowDeleteConfirmDialog] = useState(false);

  // Use either the props or local state
  const showDeleteConfirmDialog = externalShowDeleteConfirmDialog !== undefined ? externalShowDeleteConfirmDialog : localShowDeleteConfirmDialog;

  const setShowDeleteConfirmDialog = externalSetShowDeleteConfirmDialog || setLocalShowDeleteConfirmDialog;

  const visibleFiles = isEditing ? files : files.filter((file) => file.isVisible && file.readOnlyState !== 'hidden');
  const activeFile = files.find((file) => file.id === activeFileId) || files[0];

  // Determine if the File button should be shown in read mode
  const showFileButtonInReadMode = showBasicFileActionsInReadMode || showFilePropertiesInReadMode;

  return (
    <>
      <div className={cn('px-4 py-2 flex items-center justify-between', isDarkTheme ? 'bg-gray-800 text-white' : 'bg-muted text-foreground')}>
        <div className="flex items-center">
          <Code className="h-4 w-4 mr-2" />
          <span className="text-sm font-medium">Edit Code</span>
        </div>
        <div className="flex items-center gap-2">
          {(isEditing || showFileButtonInReadMode) && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="h-7">
                  File
                  <ChevronDown className="h-3.5 w-3.5 ml-1" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className={isDarkTheme ? 'bg-gray-800 border-gray-700 text-gray-100' : ''}>
                {isEditing && (
                  <>
                    <DropdownMenuItem
                      onClick={() => {
                        if (setShowBasicFileActionsInReadMode) {
                          setShowBasicFileActionsInReadMode(!showBasicFileActionsInReadMode);
                        }
                      }}
                      onSelect={(e) => e.preventDefault()}
                      className={cn(isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                    >
                      <div className="flex items-center justify-between w-full">
                        <span>Show file actions in view mode</span>
                        <span
                          className={cn(
                            'ml-2 h-4 w-4 border rounded flex items-center justify-center',
                            showBasicFileActionsInReadMode ? 'bg-primary border-primary' : 'border-gray-400',
                          )}
                        >
                          {showBasicFileActionsInReadMode && <Check className="h-3 w-3 text-primary-foreground" />}
                        </span>
                      </div>
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() => {
                        if (setShowFilePropertiesInReadMode) {
                          setShowFilePropertiesInReadMode(!showFilePropertiesInReadMode);
                        }
                      }}
                      onSelect={(e) => e.preventDefault()}
                      className={cn(isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                    >
                      <div className="flex items-center justify-between w-full">
                        <span>Show file properties in view mode</span>
                        <span
                          className={cn(
                            'ml-2 h-4 w-4 border rounded flex items-center justify-center',
                            showFilePropertiesInReadMode ? 'bg-primary border-primary' : 'border-gray-400',
                          )}
                        >
                          {showFilePropertiesInReadMode && <Check className="h-3 w-3 text-primary-foreground" />}
                        </span>
                      </div>
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                  </>
                )}
                {(isEditing || showBasicFileActionsInReadMode) && (
                  <>
                    <DropdownMenuItem
                      onClick={() => {
                        if (showFileDialog) showFileDialog();
                      }}
                      className={cn(isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                    >
                      <Plus className="h-4 w-4 mr-2" />
                      Create new file
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() => {
                        if (showRenameDialog) showRenameDialog(activeFileId);
                      }}
                      className={cn(isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                    >
                      <Code className="h-4 w-4 mr-2" />
                      Rename file
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() => {
                        if (showImportDialog) showImportDialog();
                      }}
                      className={cn(isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                    >
                      <Upload className="h-4 w-4 mr-2" />
                      Import file
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() => setShowDeleteConfirmDialog(true)}
                      disabled={files.length <= 1}
                      className={cn('text-destructive focus:text-destructive', isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                    >
                      <Trash2 className="h-4 w-4 mr-2" />
                      Delete file
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                  </>
                )}
                {isEditing && (
                  <DropdownMenuItem
                    onClick={() => {
                      if (toggleFileVisibility) toggleFileVisibility(activeFileId);
                    }}
                    className={cn(isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                  >
                    {activeFile?.isVisible ? (
                      <>
                        <Lock className="h-4 w-4 mr-2" />
                        Lock file
                      </>
                    ) : (
                      <>
                        <Unlock className="h-4 w-4 mr-2" />
                        Unlock file
                      </>
                    )}
                  </DropdownMenuItem>
                )}
                {isEditing && (
                  <DropdownMenuItem
                    onClick={() => {
                      if (setFileReadOnlyState) setFileReadOnlyState(activeFileId, activeFile?.readOnlyState === 'hidden' ? null : 'hidden');
                    }}
                    className={cn(activeFile?.readOnlyState === 'hidden' && 'bg-primary/20', isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                  >
                    <EyeOff className="h-4 w-4 mr-2" />
                    {activeFile?.readOnlyState === 'hidden' ? 'Show in view mode' : 'Hide in view mode'}
                    <span className="ml-2 text-xs text-muted-foreground">(invisible when not editing)</span>
                  </DropdownMenuItem>
                )}
                {(isEditing || showFilePropertiesInReadMode) && (
                  <>
                    <DropdownMenuItem
                      onClick={() => {
                        if (setMainFile) setMainFile(activeFileId);
                      }}
                      className={cn(isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                    >
                      <Play className="h-4 w-4 mr-2" />
                      {activeFile?.isMain ? 'Unset as main' : 'Set as main'}
                      <span className="ml-2 text-xs text-muted-foreground">{activeFile?.isMain ? '' : '(will execute first)'}</span>
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() => {
                        if (setFileReadOnlyState) setFileReadOnlyState(activeFileId, activeFile?.readOnlyState === 'always' ? null : 'always');
                      }}
                      className={cn(activeFile?.readOnlyState === 'always' && 'bg-primary/20', isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                    >
                      <Lock className="h-4 w-4 mr-2" />
                      Always read-only
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() => {
                        if (setFileReadOnlyState) setFileReadOnlyState(activeFileId, activeFile?.readOnlyState === 'never' ? null : 'never');
                      }}
                      className={cn(activeFile?.readOnlyState === 'never' && 'bg-primary/20', isDarkTheme && 'hover:bg-gray-700 focus:bg-gray-700')}
                    >
                      <Unlock className="h-4 w-4 mr-2" />
                      Never read-only
                    </DropdownMenuItem>
                  </>
                )}
                <DropdownMenuSeparator />
              </DropdownMenuContent>
            </DropdownMenu>
          )}
          {/* Language settings button only available in edit mode */}
          {isEditing && (
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0" onClick={showLanguagesDialog} title="Language Settings">
              <Settings className="h-4 w-4" />
            </Button>
          )}
          <Button
            variant="ghost"
            size="sm"
            className="h-8 w-8 p-0"
            onClick={() => setIsDarkTheme(!isDarkTheme)}
            title={isDarkTheme ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {isDarkTheme ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          </Button>
        </div>
      </div>
      <div className={cn('border-b flex items-center overflow-x-auto', isDarkTheme ? 'bg-gray-900 border-gray-700' : '')}>
        <div className="flex items-center overflow-x-auto w-full">
          {visibleFiles.map((file) => (
            <div
              key={file.id}
              draggable={true}
              data-language={file.language}
              onDragStart={(e) => {
                setDraggedFileId(file.id);
                e.dataTransfer.effectAllowed = 'move';
              }}
              onDragOver={(e) => {
                e.preventDefault();
                if (draggedFileId && draggedFileId !== file.id) {
                  setDragOverFileId(file.id);
                }
              }}
              onDragEnter={(e) => {
                e.preventDefault();
                if (draggedFileId && draggedFileId !== file.id) {
                  setDragOverFileId(file.id);
                }
              }}
              onDragLeave={() => {
                if (dragOverFileId === file.id) {
                  setDragOverFileId(null);
                }
              }}
              onDrop={(e) => {
                e.preventDefault();
                if (draggedFileId && draggedFileId !== file.id) {
                  reorderFiles(draggedFileId, file.id);
                }
                setDraggedFileId(null);
                setDragOverFileId(null);
              }}
              onDragEnd={() => {
                setDraggedFileId(null);
                setDragOverFileId(null);
              }}
              className={cn(
                'px-3 py-2 text-sm font-medium border-r flex items-center gap-1 min-w-24 max-w-36',
                file.id === activeFileId
                  ? isDarkTheme
                    ? 'bg-gray-800 text-gray-100'
                    : 'bg-background text-foreground'
                  : isDarkTheme
                    ? 'bg-gray-900 text-gray-400 hover:bg-gray-800/80 hover:text-gray-300 border-gray-700'
                    : 'bg-muted/50 text-muted-foreground hover:bg-muted/80',
                dragOverFileId === file.id && 'border-l-2 border-l-primary',
                draggedFileId === file.id && 'opacity-50',
                !file.isVisible && 'bg-striped',
                file.readOnlyState === 'always' && 'border-l-2 border-l-yellow-500',
                file.readOnlyState === 'never' && 'border-l-2 border-l-green-500',
                file.readOnlyState === 'hidden' && 'border-l-2 border-l-orange-500',
              )}
              title={file.name} // Add tooltip with full filename
            >
              <button
                onClick={() => {
                  setActiveFileId(file.id);
                  // If this is a language-specific file, update the editor language
                }}
                className="flex items-center gap-1 w-full overflow-hidden"
              >
                {getFileIcon(file)}
                <span className="truncate flex-1">{file.name}</span>
                {getStateIcon(file)}
                {file.isMain && <span className="ml-1 text-xs bg-primary/20 text-primary px-1 rounded flex-shrink-0">main</span>}
                {file.readOnlyState === 'always' && <Lock className="h-3 w-3 ml-1 text-yellow-500 flex-shrink-0" />}
                {file.readOnlyState === 'never' && <Unlock className="h-3 w-3 ml-1 text-green-500 flex-shrink-0" />}
                {file.readOnlyState === 'hidden' && <EyeOff className="h-3 w-3 ml-1 text-orange-500 flex-shrink-0" />}
              </button>
            </div>
          ))}
        </div>
      </div>
      {showDeleteConfirmDialog && (
        <div className="absolute inset-0 bg-background/80 backdrop-blur-sm z-50 flex items-center justify-center">
          <div className={cn('bg-background border rounded-lg shadow-lg p-4 w-96', isDarkTheme ? 'border-gray-700' : 'border-gray-200')}>
            <h3 className="text-lg font-medium mb-4">Confirm Deletion</h3>
            <div className="space-y-4">
              <p className="text-sm">
                Are you sure you want to delete this file?
                <span className="font-semibold text-destructive block mt-2">This action cannot be undone.</span>
              </p>
              <div className="flex justify-end space-x-2 pt-2">
                <Button variant="outline" onClick={() => setShowDeleteConfirmDialog(false)}>
                  Cancel
                </Button>
                <Button
                  variant="destructive"
                  onClick={() => {
                    if (deleteFile) deleteFile(activeFileId);
                    setShowDeleteConfirmDialog(false);
                  }}
                >
                  Delete
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
