'use client';

import type React from 'react';

import { useState, useRef } from 'react';
import type { CodeFile, LanguageType } from '@/components/ui/source-code/types';
import { getExtension, getExtensionForLanguage, getLanguageFromExtension } from '@/components/ui/source-code/utils';

interface UseFileManagementProps {
  files: CodeFile[];
  setFiles: React.Dispatch<React.SetStateAction<CodeFile[]>>;
  activeFileId: string;
  setActiveFileId: (id: string) => void;
}

interface UseFileManagementReturn {
  // Dialog states
  showFileDialog: boolean;
  setShowFileDialog: (show: boolean) => void;
  newFileName: string;
  setNewFileName: (name: string) => void;
  newFileLanguage: LanguageType;
  setNewFileLanguage: (lang: LanguageType) => void;

  // Import dialog states
  showImportDialog: boolean;
  setShowImportDialog: (show: boolean) => void;
  importContents: { name: string; content: string }[];
  setImportContents: (contents: { name: string; content: string }[]) => void;
  importFileNames: string[];
  setImportFileNames: (names: string[]) => void;
  fileInputRef: React.RefObject<HTMLInputElement | null>;

  // Rename dialog states
  showRenameDialog: boolean;
  setShowRenameDialog: (show: boolean) => void;
  renameFileName: string;
  setRenameFileName: (name: string) => void;
  renameFileLanguage: LanguageType;
  setRenameFileLanguage: (lang: LanguageType) => void;
  fileToRename: string | null;
  setFileToRename: (id: string | null) => void;

  // Drag and drop states
  draggedFileId: string | null;
  setDraggedFileId: (id: string | null) => void;
  dragOverFileId: string | null;
  setDragOverFileId: (id: string | null) => void;

  // File operations
  addNewFile: () => void;
  importFile: () => void;
  handleFileUpload: (e: React.ChangeEvent<HTMLInputElement>) => void;
  deleteFile: (fileId: string) => void;
  renameFile: () => void;
  reorderFiles: (fileId: string, targetFileId: string) => void;
}

export function useFileManagement({ files, setFiles, activeFileId, setActiveFileId }: UseFileManagementProps): UseFileManagementReturn {
  // New file dialog states
  const [showFileDialog, setShowFileDialog] = useState(false);
  const [newFileName, setNewFileName] = useState('');
  const [newFileLanguage, setNewFileLanguage] = useState<LanguageType>('javascript');
  const [newFileHasStates, setNewFileHasStates] = useState(false);

  // Import dialog states
  const [showImportDialog, setShowImportDialog] = useState(false);
  const [importContents, setImportContents] = useState<{ name: string; content: string }[]>([]);
  const [importFileNames, setImportFileNames] = useState<string[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Rename dialog states
  const [showRenameDialog, setShowRenameDialog] = useState(false);
  const [renameFileName, setRenameFileName] = useState('');
  const [renameFileLanguage, setRenameFileLanguage] = useState<LanguageType>('javascript');
  const [fileToRename, setFileToRename] = useState<string | null>(null);

  // Drag and drop states
  const [draggedFileId, setDraggedFileId] = useState<string | null>(null);
  const [dragOverFileId, setDragOverFileId] = useState<string | null>(null);

  // Add a new file
  const addNewFile = () => {
    if (!newFileName.trim()) return;

    // Determine file extension based on whether it has one already or needs to use the language default
    let fileName = newFileName.trim();
    let fileExtension = getExtension(fileName);

    if (!fileExtension) {
      fileExtension = getExtensionForLanguage(newFileLanguage);
      fileName = `${fileName}.${fileExtension}`;
    }

    // Check if file with same name already exists
    if (files.some((file) => file.name.toLowerCase() === fileName.toLowerCase())) {
      alert(`A file named "${fileName}" already exists.`);
      return;
    }

    const newFile: CodeFile = {
      id: crypto.randomUUID(),
      name: fileName,
      content: '',
      language: newFileLanguage,
      isMain: false,
      isVisible: true,
    };

    setFiles((prev) => [...prev, newFile]);
    setActiveFileId(newFile.id);
    setNewFileName('');
    setNewFileHasStates(false);
    setShowFileDialog(false);
  };

  // Import a file
  const importFile = () => {
    if (importContents.length === 0) return;

    const newFiles: CodeFile[] = importContents.map((fileData) => {
      // Detect language from file extension
      const extension = getExtension(fileData.name);
      const language = getLanguageFromExtension(extension);

      const newFile: CodeFile = {
        id: crypto.randomUUID(),
        name: fileData.name,
        content: fileData.content,
        language,
        isMain: false,
        isVisible: true,
      };

      return newFile;
    });

    setFiles((prev) => [...prev, ...newFiles]);

    // Set the active file to the first imported file
    if (newFiles.length > 0) {
      setActiveFileId(newFiles[0].id);
    }

    setImportFileNames([]);
    setImportContents([]);
    setShowImportDialog(false);
  };

  // Handle file upload
  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    const fileNames: string[] = [];
    const filePromises: Promise<{ name: string; content: string }>[] = [];

    Array.from(files).forEach((file) => {
      fileNames.push(file.name);

      const promise = new Promise<{ name: string; content: string }>((resolve) => {
        const reader = new FileReader();
        reader.onload = (event) => {
          const content = event.target?.result as string;
          resolve({ name: file.name, content });
        };
        reader.readAsText(file);
      });

      filePromises.push(promise);
    });

    setImportFileNames(fileNames);

    Promise.all(filePromises).then((results) => {
      setImportContents(results);
    });
  };

  // Delete a file
  const deleteFile = (fileId: string) => {
    // Don't allow deleting the last file
    if (files.length <= 1) return;

    // If deleting active file, switch to another file
    if (fileId === activeFileId) {
      const visibleFiles = files.filter((f) => f.isVisible);
      const fileIndex = visibleFiles.findIndex((f) => f.id === fileId);
      const newActiveIndex = fileIndex > 0 ? fileIndex - 1 : fileIndex + 1;
      if (visibleFiles[newActiveIndex]) {
        setActiveFileId(visibleFiles[newActiveIndex].id);
      }
    }

    setFiles(files.filter((f) => f.id !== fileId));
  };

  // Rename a file
  const renameFile = () => {
    if (!fileToRename || !renameFileName.trim()) return;

    setFiles((prevFiles) =>
      prevFiles.map((file) => {
        if (file.id !== fileToRename) return file;

        // Get the current extension
        const currentExt = getExtension(file.name);

        // Make sure the new name has the same extension
        let newName = renameFileName.trim();
        const newExt = getExtension(newName);

        // If no extension provided, use the extension for the selected language
        if (!newExt) {
          // For language-specific files, use the current extension
          // For regular files, use the extension for the selected language
          const newExtension = currentExt ? currentExt : getExtensionForLanguage(renameFileLanguage);
          newName = `${newName}.${newExtension}`;
        }

        // Check if file with same name already exists
        if (files.some((f) => f.id !== file.id && f.name.toLowerCase() === newName.toLowerCase())) {
          alert(`A file named "${newName}" already exists.`);
          return file;
        }

        // Return updated file with new name and language (only update language for non-language-specific files)
        return {
          ...file,
          name: newName,
          language: renameFileLanguage,
        };
      }),
    );

    setFileToRename(null);
    setRenameFileName('');
    setRenameFileLanguage('javascript');
    setShowRenameDialog(false);
  };

  // Reorder files (drag and drop)
  const reorderFiles = (fileId: string, targetFileId: string) => {
    if (fileId === targetFileId) return;

    setFiles((prevFiles) => {
      const newFiles = [...prevFiles];
      const fileIndex = newFiles.findIndex((f) => f.id === fileId);
      const targetIndex = newFiles.findIndex((f) => f.id === targetFileId);

      if (fileIndex === -1 || targetIndex === -1) return prevFiles;

      // Remove the file from its current position
      const [movedFile] = newFiles.splice(fileIndex, 1);

      // Insert it at the target position
      newFiles.splice(targetIndex, 0, movedFile);

      return newFiles;
    });
  };

  return {
    // Dialog states
    showFileDialog,
    setShowFileDialog,
    newFileName,
    setNewFileName,
    newFileLanguage,
    setNewFileLanguage,

    // Import dialog states
    showImportDialog,
    setShowImportDialog,
    importContents,
    setImportContents,
    importFileNames,
    setImportFileNames,
    fileInputRef,

    // Rename dialog states
    showRenameDialog,
    setShowRenameDialog,
    renameFileName,
    setRenameFileName,
    renameFileLanguage,
    setRenameFileLanguage,
    fileToRename,
    setFileToRename,

    // Drag and drop states
    draggedFileId,
    setDraggedFileId,
    dragOverFileId,
    setDragOverFileId,

    // File operations
    addNewFile,
    importFile,
    handleFileUpload,
    deleteFile,
    renameFile,
    reorderFiles,
  };
}
