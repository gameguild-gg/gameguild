'use client';

import { useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { Code, Play, Settings } from 'lucide-react';
import type { EditMenuOption } from '@/components/ui/content-edit-menu';
import type { CodeFile, LanguageType, ProgrammingLanguage } from '../../../editor/ui/source-code/types';
import { SourceCodeRenderer } from '../../../editor/ui/source-code/source-code-renderer';
import { EditorLoadingContext } from '../lexical-editor';

// Import hooks
import { useResize } from '@/hooks/editor/use-resize';
import { useTerminal } from '@/hooks/editor/use-terminal';
import { useCodeExecution } from '@/hooks/editor/use-code-execution';
import { useFileManagement } from '@/hooks/editor/use-file-management';
import { useFileState } from '@/hooks/editor/use-file-state';
import { insertSolutionTemplate, useFileContent } from '@/hooks/editor/use-file-content';
import { useLanguageSettings } from '@/hooks/editor/use-language-settings';
import { useEditorStyles } from '@/hooks/editor/use-editor-styles';

// Update the SourceCodeNodeData interface to include the predicate test type
export interface SourceCodeNodeData {
  files: CodeFile[];
  readonly: boolean;
  showExecution: boolean;
  isDarkTheme?: boolean;
  isNew?: boolean;
  activeFileId?: string;
  selectedLanguage?: ProgrammingLanguage;
  clearTerminalOnRun?: boolean;
  allowedLanguages?: Record<LanguageType, boolean>;
  showBasicFileActionsInReadMode?: boolean;
  showFilePropertiesInReadMode?: boolean;
  showTests?: boolean;
  testCases?: Record<
    string,
    {
      type: 'simple' | 'inout' | 'predicate' | 'custom' | 'function';
      input?: string;
      expectedOutput?: string;
      args?: any[];
      expectedReturn?: any[];
      predicate?: string;
    }[]
  >;
  activeTab?: 'terminal' | 'tests';
}

interface SourceCodeCoreProps {
  data: SourceCodeNodeData;
  isPreview?: boolean;
  onUpdateSourceCode?: (newData: Partial<SourceCodeNodeData>) => void;
  onSave?: () => void;
}

export function SourceCodeCore({ data, isPreview = false, onUpdateSourceCode, onSave }: SourceCodeCoreProps) {
  // Adicionar esta linha para usar o contexto de carregamento
  const isLoadingProject = useContext(EditorLoadingContext);

  // Helper function to get the initial active file
  const getInitialActiveFileId = useCallback(
    (files: CodeFile[], isEditingMode: boolean) => {
      if (files.length === 0) return '';

      // In editing mode, use the stored activeFileId or first file
      if (isEditingMode) {
        return data.activeFileId || files[0]?.id || '';
      }

      // In view/preview mode, prioritize main file or first visible file
      // 1. Look for main file
      const mainFile = files.find((file) => file.isMain);
      if (mainFile) {
        return mainFile.id;
      }

      // 2. Look for first visible file (not hidden in view mode)
      const visibleFile = files.find((file) => file.isVisible !== false);
      if (visibleFile) {
        return visibleFile.id;
      }

      // 3. Fallback to first file
      return files[0]?.id || '';
    },
    [data.activeFileId],
  );

  // Force non-editing mode for preview OR when loading a project
  const initialIsEditing = isPreview ? false : isLoadingProject ? false : data.isNew || false;
  const [isEditing, setIsEditing] = useState(initialIsEditing);
  const [files, setFiles] = useState<CodeFile[]>(data.files || []);
  const [activeFileId, setActiveFileId] = useState<string>(getInitialActiveFileId(data.files || [], initialIsEditing));
  const [readonly, setReadonly] = useState(data.readonly ?? false);
  const [showExecution, setShowExecution] = useState(data.showExecution ?? false);
  const [isDarkTheme, setIsDarkTheme] = useState(data.isDarkTheme ?? false);
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [clearTerminalOnRun, setClearTerminalOnRun] = useState(data.clearTerminalOnRun ?? false);
  const [showBasicFileActionsInReadMode, setShowBasicFileActionsInReadMode] = useState(data.showBasicFileActionsInReadMode ?? true);
  const [showFilePropertiesInReadMode, setShowFilePropertiesInReadMode] = useState(data.showFilePropertiesInReadMode ?? false);
  const [showSettings, setShowSettings] = useState(false);
  const [showTests, setShowTests] = useState(data.showTests ?? false);
  const [testCases, setTestCases] = useState<
    Record<
      string,
      {
        type: 'simple' | 'inout' | 'predicate' | 'custom' | 'function';
        input?: string;
        expectedOutput?: string;
        args?: any[];
        expectedReturn?: any[];
        predicate?: string;
      }[]
    >
  >(data.testCases ?? {});
  const [testResults, setTestResults] = useState<
    Record<
      string,
      {
        passed: boolean;
        actual: string;
        expected: string;
      }[]
    >
  >({});
  const [activeTab, setActiveTab] = useState<'terminal' | 'tests'>(data.activeTab ?? 'terminal');

  // Track mode sessions to control automatic file selection
  const lastModeSessionRef = useRef<string>('');
  const getCurrentModeSession = useCallback(() => {
    return isPreview ? 'preview' : isEditing ? 'editing' : 'viewing';
  }, [isPreview, isEditing]);

  // Use the editor styles hook
  useEditorStyles();

  // Use language settings hook
  const { selectedLanguage, setSelectedLanguage, allowedLanguages, setAllowedLanguages, showLanguagesDialog, setShowLanguagesDialog, getAllowedLanguageTypes, getAllowedProgrammingLanguages } = useLanguageSettings({
    initialAllowedLanguages: data.allowedLanguages,
    initialSelectedLanguage: data.selectedLanguage,
  });

  // Use file content hook
  const { activeFile, activeFileContent, updateActiveFileContent, getVisibleFiles, addSolutionTemplate } = useFileContent({
    files,
    setFiles,
    activeFileId,
    selectedLanguage,
  });

  // Use file state hook
  const { toggleFileVisibility, setMainFile, setFileReadOnlyState, isFileReadOnly } = useFileState({
    files,
    setFiles,
  });

  // Use file management hook
  const {
    showFileDialog,
    setShowFileDialog,
    newFileName,
    setNewFileName,
    newFileLanguage,
    setNewFileLanguage,
    newFileHasStates,
    setNewFileHasStates,
    showImportDialog,
    setShowImportDialog,
    importContents,
    setImportContents,
    importFileNames,
    setImportFileNames,
    importFileHasStates,
    setImportFileHasStates,
    fileInputRef,
    showConfirmDialog: showConfirmFileDialogManagement,
    setShowConfirmDialog: setShowConfirmDialogManagement,
    draggedFileId,
    setDraggedFileId,
    dragOverFileId,
    setDragOverFileId,
    showRenameDialog,
    setShowRenameDialog,
    renameFileName,
    setRenameFileName,
    renameFileLanguage,
    setRenameFileLanguage,
    fileToRename,
    setFileToRename,
    addNewFile,
    importFile,
    handleFileUpload,
    deleteFile,
    reorderFiles,
    renameFile,
  } = useFileManagement({
    files,
    setFiles,
    activeFileId,
    setActiveFileId,
  });

  const scrollTerminalToBottom = () => {
    const terminalElement = fileInputRef?.current?.parentElement?.parentElement?.parentElement;
    if (terminalElement) {
      terminalElement.scrollTop = terminalElement.scrollHeight;
    }
  };

  // Use the terminal hook
  const {
    output,
    setOutput,
    input,
    setInput,
    commandHistory,
    setCommandHistory,
    historyIndex,
    setHistoryIndex,
    waitingForInput,
    setWaitingForInput,
    inputPrompt,
    setInputPrompt,
    inputCallback,
    setInputCallback,
    terminalInputRef,
    handleTerminalInput,
    clearTerminal,
    addOutput,
  } = useTerminal({
    onCommand: (command) => {
      return true;
    },
    scrollToBottom: scrollTerminalToBottom,
  });

  // Use the resize hooks with renamed setters to avoid conflicts
  const { height: codeEditorHeight, setHeight: setCodeEditorHeight, handleResize: handleCodeEditorResize } = useResize(200, { minHeight: 100 });

  const { height: terminalHeight, setHeight: setTerminalHeight, handleResize: handleTerminalResize } = useResize(128, { minHeight: 50 });

  // Use the code execution hook
  const {
    isExecuting,
    setIsExecuting,
    handleExecute: executeCode,
    handleStopExecution,
    handleTerminalCommand,
  } = useCodeExecution({
    files,
    selectedLanguage,
    clearTerminalOnRun,
    addOutput,
    clearTerminal,
  });

  // Add a wrapper function for handleExecute
  const handleExecute = () => {
    executeCode(activeFileId);
  };

  // Get visible files based on editing mode
  const visibleFiles = useMemo(() => {
    return getVisibleFiles(isEditing);
  }, [getVisibleFiles, isEditing]);

  // Update active file when needed
  useEffect(() => {
    if (activeFile && activeFile.id !== activeFileId) {
      setActiveFileId(activeFile.id);
    }
  }, [activeFile, activeFileId]);

  // Handle automatic file selection only once per mode session
  useEffect(() => {
    const currentModeSession = getCurrentModeSession();

    // Only perform automatic selection if we're entering a new mode session
    if (currentModeSession !== lastModeSessionRef.current) {
      const newActiveFileId = getInitialActiveFileId(files, isEditing);
      if (newActiveFileId && newActiveFileId !== activeFileId) {
        setActiveFileId(newActiveFileId);
      }

      // Update the last mode session
      lastModeSessionRef.current = currentModeSession;
    }
  }, [isEditing, files, getInitialActiveFileId, activeFileId, getCurrentModeSession]);

  // Prevent editing mode in preview
  const handleSetIsEditing = useCallback(
    (editing: boolean) => {
      if (isPreview && editing) {
        // Don't allow editing in preview mode
        return;
      }
      setIsEditing(editing);
    },
    [isPreview],
  );

  // Update the addTestCase function to handle the predicate test type
  const addTestCase = useCallback(
    (fileId: string, type: 'simple' | 'inout' | 'predicate' | 'custom' | 'function' = 'simple') => {
      setTestCases((prev) => {
        const fileCases = prev[fileId] || [];

        // If there are existing tests, use their type
        const testType = fileCases.length > 0 ? fileCases[0].type : type;

        // If this is the first In/Out or Predicate test, add the solution template to the file
        if ((testType === 'inout' || testType === 'predicate') && fileCases.length === 0) {
          const file = files.find((f) => f.id === fileId);
          if (file) {
            const content = file.content;
            const updatedContent = insertSolutionTemplate(content, selectedLanguage);

            // Update the file content
            setFiles((prevFiles) =>
              prevFiles.map((f) => {
                if (f.id === fileId) {
                  return {
                    ...f,
                    content: updatedContent,
                  };
                }
                return f;
              }),
            );
          }
        }

        return {
          ...prev,
          [fileId]: [
            ...fileCases,
            {
              type: testType,
              ...(testType === 'simple' ? { expectedOutput: '' } : testType === 'predicate' ? { args: [], predicate: "result => typeof result === 'number'" } : { args: [], expectedReturn: [] }),
            },
          ],
        };
      });
    },
    [files, selectedLanguage, setFiles],
  );

  const removeTestCase = useCallback((fileId: string, index: number) => {
    setTestCases((prev) => {
      const fileCases = [...(prev[fileId] || [])];
      fileCases.splice(index, 1);
      return {
        ...prev,
        [fileId]: fileCases,
      };
    });

    // Also remove test results if they exist
    setTestResults((prev) => {
      if (!prev[fileId]) return prev;
      const fileResults = [...prev[fileId]];
      fileResults.splice(index, 1);
      return {
        ...prev,
        [fileId]: fileResults,
      };
    });
  }, []);

  const updateTestCase = useCallback(
    (
      fileId: string,
      index: number,
      testData: Partial<{
        type: 'simple' | 'inout' | 'predicate' | 'custom' | 'function';
        input?: string;
        expectedOutput?: string;
        args?: any[];
        expectedReturn?: any[];
        predicate?: string;
      }>,
    ) => {
      setTestCases((prev) => {
        const fileCases = [...(prev[fileId] || [])];
        fileCases[index] = { ...fileCases[index], ...testData };
        return {
          ...prev,
          [fileId]: fileCases,
        };
      });
    },
    [],
  );

  const runTests = useCallback(
    (fileId: string) => {
      const fileCases = testCases[fileId] || [];
      if (fileCases.length === 0) return;

      // Clear previous test results
      setTestResults((prev) => ({
        ...prev,
        [fileId]: [],
      }));

      // Run each test case
      fileCases.forEach((testCase, index) => {
        // Simulate test execution
        const actualOutput = 'Test output for ' + fileId + ' test case ' + index;
        const passed = actualOutput.trim() === testCase.expectedOutput?.trim();

        setTestResults((prev) => {
          const fileResults = [...(prev[fileId] || [])];
          fileResults[index] = {
            passed,
            actual: actualOutput,
            expected: testCase.expectedOutput || '',
          };
          return {
            ...prev,
            [fileId]: fileResults,
          };
        });
      });
    },
    [testCases],
  );

  // Update source code node
  const updateSourceCode = (newData: Partial<SourceCodeNodeData>) => {
    console.log('Updating source code with data:', newData);

    // Create a deep copy of current files
    const filesCopy = files.map((file) => {
      const fileCopy = { ...file };
      return fileCopy;
    });

    const updatedData = {
      ...data,
      ...newData,
      files: newData.files || filesCopy,
      activeFileId,
      isDarkTheme,
      selectedLanguage,
      clearTerminalOnRun,
      allowedLanguages: newData.allowedLanguages || allowedLanguages,
      showBasicFileActionsInReadMode,
      showFilePropertiesInReadMode,
      showTests,
      testCases: newData.testCases || testCases,
      activeTab,
    };

    if (onUpdateSourceCode) {
      onUpdateSourceCode(updatedData);
    }
  };

  // Save changes
  const handleSave = () => {
    console.log('Saving files:', files);

    // Create a deep copy of files
    const filesCopy = files.map((file) => {
      const fileCopy = { ...file };
      return fileCopy;
    });

    updateSourceCode({
      readonly,
      showExecution,
      files: filesCopy,
      testCases: testCases,
    });

    setIsEditing(false);

    if (onSave) {
      onSave();
    }
  };

  // Edit menu options - different for preview vs normal mode
  const editMenuOptions: EditMenuOption[] = useMemo(() => {
    if (isPreview) {
      // Preview mode - only show view/execution options
      return [
        {
          id: 'settings',
          icon: <Settings className="h-4 w-4" />,
          label: 'Settings',
          action: () => setShowSettings(!showSettings),
        },
        {
          id: 'execute',
          icon: <Play className="h-4 w-4" />,
          label: 'Run Code',
          action: handleExecute,
        },
      ];
    } else {
      // Normal mode - show all options including edit
      return [
        {
          id: 'edit',
          icon: <Code className="h-4 w-4" />,
          label: 'Edit Code',
          action: () => {
            // Reset state to original data from the node when entering edit mode
            setFiles(
              data.files || [
                {
                  id: crypto.randomUUID(),
                  name: 'index.js',
                  content: '',
                  language: 'javascript',
                  isMain: false,
                  isVisible: true,
                },
              ],
            );
            setActiveFileId(data.activeFileId || data.files?.[0]?.id);
            setReadonly(data.readonly ?? false);
            setShowExecution(data.showExecution ?? false);
            setIsDarkTheme(data.isDarkTheme ?? false);
            setSelectedLanguage(data.selectedLanguage || 'javascript');
            setClearTerminalOnRun(data.clearTerminalOnRun ?? false);
            setShowBasicFileActionsInReadMode(data.showBasicFileActionsInReadMode ?? true);
            setShowFilePropertiesInReadMode(data.showFilePropertiesInReadMode ?? false);

            // Set editing mode to true
            setIsEditing(true);
          },
        },
        {
          id: 'settings',
          icon: <Settings className="h-4 w-4" />,
          label: 'Settings',
          action: () => setShowSettings(!showSettings),
        },
        {
          id: 'execute',
          icon: <Play className="h-4 w-4" />,
          label: 'Run Code',
          action: () => executeCode(activeFileId),
        },
      ];
    }
  }, [isPreview, showSettings, data]);

  // Render the component
  return (
    <SourceCodeRenderer
      data={data}
      isEditing={isEditing}
      setIsEditing={handleSetIsEditing}
      files={files}
      setFiles={setFiles}
      activeFileId={activeFileId}
      setActiveFileId={setActiveFileId}
      readonly={readonly}
      setReadonly={setReadonly}
      showExecution={showExecution}
      setShowExecution={setShowExecution}
      isExecuting={isExecuting}
      setIsExecuting={setIsExecuting}
      output={output}
      setOutput={setOutput}
      input={input}
      setInput={setInput}
      showSettings={showSettings}
      setShowSettings={setShowSettings}
      waitingForInput={waitingForInput}
      setWaitingForInput={setWaitingForInput}
      inputPrompt={inputPrompt}
      setInputPrompt={setInputPrompt}
      inputCallback={inputCallback}
      setInputCallback={setInputCallback}
      terminalInputRef={terminalInputRef}
      commandHistory={commandHistory}
      setCommandHistory={setCommandHistory}
      historyIndex={historyIndex}
      setHistoryIndex={setHistoryIndex}
      isDarkTheme={isDarkTheme}
      setIsDarkTheme={setIsDarkTheme}
      codeEditorHeight={codeEditorHeight}
      setCodeEditorHeight={setCodeEditorHeight}
      terminalHeight={terminalHeight}
      setTerminalHeight={setTerminalHeight}
      showFileDialog={showFileDialog}
      setShowFileDialog={setShowFileDialog}
      newFileName={newFileName}
      setNewFileName={setNewFileName}
      newFileLanguage={newFileLanguage}
      setNewFileLanguage={setNewFileLanguage}
      newFileHasStates={newFileHasStates}
      setNewFileHasStates={setNewFileHasStates}
      showImportDialog={showImportDialog}
      setShowImportDialog={setShowImportDialog}
      importContents={importContents}
      setImportContents={setImportContents}
      importFileNames={importFileNames}
      setImportFileNames={setImportFileNames}
      importFileHasStates={importFileHasStates}
      setImportFileHasStates={setImportFileHasStates}
      fileInputRef={fileInputRef}
      showConfirmDialog={showConfirmFileDialogManagement}
      setShowConfirmDialog={setShowConfirmDialog}
      draggedFileId={draggedFileId}
      setDraggedFileId={setDraggedFileId}
      dragOverFileId={dragOverFileId}
      setDragOverFileId={setDragOverFileId}
      showRenameDialog={showRenameDialog}
      setShowRenameDialog={setShowRenameDialog}
      renameFileName={renameFileName}
      setRenameFileName={setRenameFileName}
      renameFileLanguage={renameFileLanguage}
      setRenameFileLanguage={setRenameFileLanguage}
      fileToRename={fileToRename}
      setFileToRename={setFileToRename}
      selectedLanguage={selectedLanguage}
      setSelectedLanguage={setSelectedLanguage}
      clearTerminalOnRun={clearTerminalOnRun}
      setClearTerminalOnRun={setClearTerminalOnRun}
      allowedLanguages={allowedLanguages}
      setAllowedLanguages={setAllowedLanguages}
      showLanguagesDialog={showLanguagesDialog}
      setShowLanguagesDialog={setShowLanguagesDialog}
      showBasicFileActionsInReadMode={showBasicFileActionsInReadMode}
      setShowBasicFileActionsInReadMode={setShowBasicFileActionsInReadMode}
      showFilePropertiesInReadMode={showFilePropertiesInReadMode}
      setShowFilePropertiesInReadMode={setShowFilePropertiesInReadMode}
      showTests={showTests}
      setShowTests={setShowTests}
      activeTab={activeTab}
      setActiveTab={setActiveTab}
      testCases={testCases}
      setTestCases={setTestCases}
      testResults={testResults}
      setTestResults={setTestResults}
      addTestCase={addTestCase}
      removeTestCase={removeTestCase}
      updateTestCase={updateTestCase}
      runTests={runTests}
      isPreview={isPreview}
      // Functions
      addNewFile={addNewFile}
      importFile={importFile}
      handleFileUpload={handleFileUpload}
      deleteFile={deleteFile}
      toggleFileVisibility={toggleFileVisibility}
      setMainFile={setMainFile}
      reorderFiles={reorderFiles}
      renameFile={renameFile}
      updateSourceCode={updateSourceCode}
      handleSave={handleSave}
      handleExecute={handleExecute}
      handleStopExecution={handleStopExecution}
      handleTerminalInput={handleTerminalInput}
      handleCodeEditorResize={handleCodeEditorResize}
      handleTerminalResize={handleTerminalResize}
      isFileReadOnly={(file) => isFileReadOnly(file, readonly)}
      updateActiveFileContent={updateActiveFileContent}
      setFileReadOnlyState={setFileReadOnlyState}
      addSolutionTemplate={addSolutionTemplate}
      getAllowedLanguageTypes={getAllowedLanguageTypes}
      getAllowedProgrammingLanguages={getAllowedProgrammingLanguages}
    />
  );
}
