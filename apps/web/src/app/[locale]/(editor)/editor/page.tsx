'use client';

import { Editor } from '@/components/editor/lexical-editor';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { HardDrive, Moon, Save, SaveAll, Settings, Sun } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';
import type { LexicalEditor } from 'lexical';
import { OpenProjectDialog } from '@/components/editor/ui/editor/open-project-dialog';
import { CreateProjectDialog } from '@/components/editor/ui/editor/create-project-dialog';
import { EnhancedStorageAdapter } from '@/lib/storage/enhanced-storage-adapter';
import { syncConfig } from '@/lib/sync/sync-config';

interface ProjectData {
  id: string;
  name: string;
  data: string;
  tags: string[];
  size: number;
  createdAt: string;
  updatedAt: string;
}

// Generate unique ID for projects
function generateProjectId(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  // Fallback for environments without crypto.randomUUID
  return 'proj_' + Date.now().toString(36) + '_' + Math.random().toString(36).substr(2, 9);
}

// Estimate size of data in bytes
function estimateSize(data: string): number {
  return new Blob([data]).size;
}

// IndexedDB configuration
const DB_NAME = 'GGEditorDB';
const DB_VERSION = 1;
const STORE_NAME = 'projects';
const TAGS_STORE_NAME = 'tags';

// IndexedDB utility functions
class IndexedDBStorage {
  private db: IDBDatabase | null = null;

  async init(): Promise<void> {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(DB_NAME, DB_VERSION);

      request.onerror = () => {
        console.error('Failed to open IndexedDB:', request.error);
        reject(request.error);
      };

      request.onsuccess = () => {
        this.db = request.result;
        console.log('IndexedDB initialized successfully');
        resolve();
      };

      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result;

        // Create projects object store if it doesn't exist
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          const store = db.createObjectStore(STORE_NAME, { keyPath: 'id' });
          store.createIndex('name', 'name', { unique: false });
          store.createIndex('tags', 'tags', { unique: false, multiEntry: true });
          store.createIndex('createdAt', 'createdAt', { unique: false });
          store.createIndex('updatedAt', 'updatedAt', { unique: false });
          console.log('Created IndexedDB projects object store');
        }

        // Create tags object store if it doesn't exist
        if (!db.objectStoreNames.contains(TAGS_STORE_NAME)) {
          const tagsStore = db.createObjectStore(TAGS_STORE_NAME, { keyPath: 'name' });
          tagsStore.createIndex('usageCount', 'usageCount', { unique: false });
          tagsStore.createIndex('createdAt', 'createdAt', { unique: false });
          console.log('Created IndexedDB tags object store');
        }
      };
    });
  }

  async save(id: string, name: string, data: string, tags: string[] = []): Promise<void> {
    if (!this.db) {
      throw new Error('IndexedDB not initialized');
    }

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([STORE_NAME], 'readwrite');
      const store = transaction.objectStore(STORE_NAME);

      const projectData = {
        id: id,
        name: name,
        data: data,
        tags: tags,
        size: estimateSize(data),
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };

      // Check if project already exists to preserve createdAt
      const getRequest = store.get(id);
      getRequest.onsuccess = () => {
        if (getRequest.result) {
          projectData.createdAt = getRequest.result.createdAt;
        }

        const putRequest = store.put(projectData);
        putRequest.onsuccess = () => resolve();
        putRequest.onerror = () => reject(putRequest.error);
      };
      getRequest.onerror = () => reject(getRequest.error);
    });
  }

  async load(id: string): Promise<ProjectData | null> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([STORE_NAME], 'readonly');
      const store = transaction.objectStore(STORE_NAME);
      const request = store.get(id);

      request.onsuccess = () => {
        resolve(request.result || null);
      };
      request.onerror = () => reject(request.error);
    });
  }

  async list(): Promise<ProjectData[]> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([STORE_NAME], 'readonly');
      const store = transaction.objectStore(STORE_NAME);
      const request = store.getAll();

      request.onsuccess = () => {
        const projects = request.result as ProjectData[];
        // Sort by updatedAt descending (most recent first)
        projects.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime());
        resolve(projects);
      };
      request.onerror = () => reject(request.error);
    });
  }

  async delete(id: string): Promise<void> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([STORE_NAME], 'readwrite');
      const store = transaction.objectStore(STORE_NAME);
      const request = store.delete(id);

      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  async getStorageInfo(): Promise<{ totalSize: number; projectCount: number }> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([STORE_NAME], 'readonly');
      const store = transaction.objectStore(STORE_NAME);
      const request = store.getAll();

      request.onsuccess = () => {
        const projects = request.result as ProjectData[];
        const totalSize = projects.reduce((sum, project) => sum + project.size, 0);
        resolve({
          totalSize,
          projectCount: projects.length,
        });
      };
      request.onerror = () => reject(request.error);
    });
  }
}

export default function EditorPage() {
  const [currentProjectId, setCurrentProjectId] = useState<string>('');
  const [currentProjectName, setCurrentProjectName] = useState<string>('Untitled Project');
  const [projectTags, setProjectTags] = useState<string[]>([]);
  const [editorState, setEditorState] = useState<string>('');
  const [autoSaveEnabled, setAutoSaveEnabled] = useState<boolean>(true);
  const [isDarkMode, setIsDarkMode] = useState<boolean>(false);
  const [isDbInitialized, setIsDbInitialized] = useState<boolean>(false);

  // Storage and sync
  const [storageLimit, setStorageLimit] = useState<number | null>(null);
  const [totalStorageUsed, setTotalStorageUsed] = useState<number>(0);
  const [projectCount, setProjectCount] = useState<number>(0);
  const [showStorageLimitDialog, setShowStorageLimitDialog] = useState<boolean>(false);
  const [newStorageLimit, setNewStorageLimit] = useState<string>('');

  // Dialog states
  const [showProjectDialog, setShowProjectDialog] = useState<boolean>(false);
  const [showCreateDialog, setShowCreateDialog] = useState<boolean>(false);
  const [showSaveAsDialog, setShowSaveAsDialog] = useState<boolean>(false);
  const [newProjectName, setNewProjectName] = useState<string>('');

  // Refs
  const editorRef = useRef<LexicalEditor | null>(null);
  const dbStorage = useRef<IndexedDBStorage>(new IndexedDBStorage());
  const autoSaveTimeoutRef = useRef<NodeJS.Timeout>();
  const storageAdapter = new EnhancedStorageAdapter({
    storageQuotaKB: storageLimit ? storageLimit * 1024 : undefined,
    compressionEnabled: true,
    encryptionEnabled: false,
  });

  // Initialize IndexedDB
  useEffect(() => {
    const initDB = async () => {
      try {
        await dbStorage.current.init();
        setIsDbInitialized(true);
        await updateStorageInfo();
      } catch (error) {
        console.error('Failed to initialize IndexedDB:', error);
        toast.error('Storage error', {
          description: 'Failed to initialize local storage. Some features may not work.',
        });
      }
    };

    initDB();
  }, []);

  // Update storage information
  const updateStorageInfo = async () => {
    if (!isDbInitialized) return;

    try {
      const info = await dbStorage.current.getStorageInfo();
      setTotalStorageUsed(info.totalSize);
      setProjectCount(info.projectCount);
    } catch (error) {
      console.error('Failed to get storage info:', error);
    }
  };

  // Check if storage is at limit
  const isStorageAtLimit = (): boolean => {
    if (!storageLimit) return false;
    const limitBytes = storageLimit * 1024 * 1024; // Convert MB to bytes
    return totalStorageUsed >= limitBytes;
  };

  // Save current project
  const handleSave = async (editorState?: string) => {
    if (!currentProjectName.trim()) {
      toast.error('Name required', {
        description: 'Please enter a project name before saving.',
      });
      return;
    }

    if (isStorageAtLimit()) {
      toast.error('Overcrowded storage', {
        description: 'Storage limit reached. Please delete some projects or increase the limit.',
      });
      return;
    }

    let stateToSave = editorState;
    if (!stateToSave && editorRef.current) {
      try {
        const currentState = editorRef.current.getEditorState();
        stateToSave = JSON.stringify(currentState.toJSON());
      } catch (error) {
        console.error('Failed to get editor state:', error);
        toast.error('Error in editor', {
          description: 'Failed to get current editor state.',
        });
        return;
      }
    }

    if (!stateToSave || stateToSave.trim() === '') {
      toast.error('Nothing to save', {
        description: 'The editor is empty. Add content before saving.',
      });
      return;
    }

    try {
      await storageAdapter.save(currentProjectId, currentProjectName, stateToSave, projectTags);
      await updateStorageInfo();

      if (isStorageAtLimit()) {
        toast.warning('Saved Project - Little Space', {
          description: 'Project saved, but storage is nearly full.',
        });
      } else {
        toast.success('Project saved successfully', {
          description: `"${currentProjectName}" was saved in the database.`,
        });
      }
    } catch (error: any) {
      console.error('Save error:', error);
      toast.error('Error saving', {
        description: error.message || 'Failed to save project.',
      });
    }
  };

  // Save as new project
  const handleSaveAs = async () => {
    if (!newProjectName.trim()) {
      toast.error('Name required', {
        description: 'Please enter a project name.',
      });
      return;
    }

    if (isStorageAtLimit()) {
      toast.error('Storage Full', {
        description: 'Storage limit reached. Cannot create new projects.',
      });
      return;
    }

    try {
      const existingProjects = await storageAdapter.list();
      const nameExists = existingProjects.some((project) => project.name.toLowerCase() === newProjectName.toLowerCase());

      if (nameExists) {
        toast.error('Name already exists', {
          description: 'A project with this name already exists. Please choose a different name.',
        });
        return;
      }

      const newProjectId = generateProjectId();
      let stateToSave = editorState;
      if (!stateToSave && editorRef.current) {
        try {
          const currentState = editorRef.current.getEditorState();
          stateToSave = JSON.stringify(currentState.toJSON());
        } catch (error) {
          console.error('Failed to get editor state:', error);
          toast.error('Error in editor', {
            description: 'Failed to get current editor state.',
          });
          return;
        }
      }

      if (!stateToSave || stateToSave.trim() === '') {
        toast.error('Nothing to save', {
          description: 'The editor is empty. Add content before saving.',
        });
        return;
      }

      try {
        await storageAdapter.save(newProjectId, newProjectName, stateToSave, projectTags);

        // Update current project info
        setCurrentProjectId(newProjectId);
        setCurrentProjectName(newProjectName);
        await updateStorageInfo();

        if (isStorageAtLimit()) {
          toast.warning('Project created - Little space', {
            description: 'Project created successfully, but storage is nearly full.',
          });
        } else {
          toast.success('New project created', {
            description: `"${newProjectName}" was created and saved successfully.`,
          });
        }

        setShowSaveAsDialog(false);
        setNewProjectName('');
      } catch (error: any) {
        console.error('Save as error:', error);
        toast.error('Error creating project', {
          description: error.message || 'Failed to create new project.',
        });
      }
    } catch (error: any) {
      console.error('Save as error:', error);
      toast.error('Error creating project', {
        description: error.message || 'Failed to create new project.',
      });
    }
  };

  // Handle opening a project
  const handleOpen = async (projectId: string) => {
    try {
      const projectData = await storageAdapter.load(projectId);
      if (projectData) {
        // Update editor state
        setEditorState(projectData.data);
        setCurrentProjectId(projectData.id);
        setCurrentProjectName(projectData.name);
        setProjectTags(projectData.tags || []);

        // Load data into editor
        if (editorRef.current && projectData.data) {
          try {
            const parsedState = JSON.parse(projectData.data);
            editorRef.current.setEditorState(editorRef.current.parseEditorState(parsedState));
          } catch (error) {
            console.error('Failed to parse editor state:', error);
            toast.error('Error loading project', {
              description: 'Failed to load project content.',
            });
          }
        }

        toast.success('Project loaded', {
          description: `"${projectData.name}" has been loaded successfully.`,
        });
      } else {
        toast.error('Project not found', {
          description: 'The selected project could not be found.',
        });
      }
    } catch (error) {
      console.error('Open error:', error);
      toast.error('Error opening project', {
        description: 'Failed to open the selected project.',
      });
    }
  };

  // Delete a project
  const handleDelete = async (projectId: string, projectName: string) => {
    try {
      await storageAdapter.delete(projectId);
      await updateStorageInfo();

      // Clear current project if it was deleted
      if (currentProjectId === projectId) {
        setCurrentProjectId('');
        setCurrentProjectName('Untitled Project');
        setProjectTags([]);
        setEditorState('');
        if (editorRef.current) {
          editorRef.current.setEditorState(editorRef.current.parseEditorState(null));
        }
      }

      toast.success('Deleted project', {
        description: `"${projectName}" has been deleted successfully.`,
      });
    } catch (error) {
      console.error('Delete error:', error);
      toast.error('Error deleting project', {
        description: 'Failed to delete the project.',
      });
    }
  };

  // Create new project
  const handleNewProject = () => {
    setCurrentProjectId('');
    setCurrentProjectName('Untitled Project');
    setProjectTags([]);
    setEditorState('');
    if (editorRef.current) {
      editorRef.current.setEditorState(editorRef.current.parseEditorState(null));
    }

    toast.success('New project created', {
      description: 'You can now start working on your new project.',
    });
  };

  // Auto-save functionality
  useEffect(() => {
    if (!autoSaveEnabled || !currentProjectId || !isDbInitialized) return;

    if (autoSaveTimeoutRef.current) {
      clearTimeout(autoSaveTimeoutRef.current);
    }

    autoSaveTimeoutRef.current = setTimeout(async () => {
      if (currentProjectId && currentProjectName) {
        try {
          await handleSave(editorState);
          console.log('Auto-saved project:', currentProjectName);
        } catch (error) {
          console.error('Auto-save failed:', error);
        }
      }
    }, 2000); // Auto-save after 2 seconds of inactivity

    return () => {
      if (autoSaveTimeoutRef.current) {
        clearTimeout(autoSaveTimeoutRef.current);
      }
    };
  }, [editorState, autoSaveEnabled, currentProjectId, currentProjectName, projectTags, isDbInitialized]);

  // Storage limit management
  const handleSetStorageLimit = (limitValue: string) => {
    if (!limitValue.trim()) {
      setStorageLimit(null);
      setNewStorageLimit('');
      setShowStorageLimitDialog(false);
      toast.success('Limit removed', {
        description: 'Storage limit has been removed.',
      });
      return;
    }

    const limit = Number.parseFloat(limitValue);
    if (isNaN(limit) || limit <= 0) {
      toast.error('Invalid value', {
        description: 'Please enter a valid positive number.',
      });
      return;
    }

    const currentUsageMB = totalStorageUsed / 1024 / 1024;
    if (limit < currentUsageMB + 1) {
      toast.error('Very low limit', {
        description: `Limit too low. Current usage is ${currentUsageMB.toFixed(2)} MB.`,
      });
      return;
    }

    setStorageLimit(limit);
    setNewStorageLimit('');
    setShowStorageLimitDialog(false);
    toast.success('Configured limit', {
      description: `Storage limit set to ${limit} MB.`,
    });
  };

  return (
    <div className="h-screen flex flex-col">
      {/* Header */}
      <div className="border-b bg-background px-4 py-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <h1 className="text-lg font-semibold">Game Guild Editor</h1>
            <div className="text-sm text-muted-foreground">
              {currentProjectName}
              {currentProjectId && <span className="ml-2 text-xs">({currentProjectId.slice(0, 8)}...)</span>}
            </div>
          </div>

          <div className="flex items-center space-x-2">
            {/* Storage info */}
            <div className="text-xs text-muted-foreground">
              {projectCount} projects • {(totalStorageUsed / 1024 / 1024).toFixed(2)} MB used
              {storageLimit && (
                <span className="ml-1">
                  / {storageLimit} MB ({((totalStorageUsed / 1024 / 1024 / storageLimit) * 100).toFixed(1)}%)
                </span>
              )}
            </div>

            {/* Storage limit button */}
            <Button variant="ghost" size="sm" onClick={() => setShowStorageLimitDialog(true)} className="h-8 px-2">
              <HardDrive className="h-4 w-4" />
            </Button>

            {/* New project */}
            <Button variant="ghost" size="sm" onClick={handleNewProject} className="h-8 px-2">
              New
            </Button>

            {/* Open project */}
            <OpenProjectDialog onProjectSelect={handleOpen} onProjectDelete={handleDelete} />

            {/* Save project */}
            <Button variant="ghost" size="sm" onClick={() => handleSave()} disabled={!currentProjectName.trim()} className="h-8 px-2">
              <Save className="h-4 w-4" />
            </Button>

            {/* Save as */}
            <Button variant="ghost" size="sm" onClick={() => setShowSaveAsDialog(true)} className="h-8 px-2">
              <SaveAll className="h-4 w-4" />
            </Button>

            {/* Create project dialog */}
            <CreateProjectDialog
              onProjectCreate={(name, tags) => {
                setCurrentProjectId(generateProjectId());
                setCurrentProjectName(name);
                setProjectTags(tags);
                handleSave();
              }}
            />

            {/* Theme toggle */}
            <Button variant="ghost" size="sm" onClick={() => setIsDarkMode(!isDarkMode)} className="h-8 px-2">
              {isDarkMode ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
            </Button>

            {/* Settings */}
            <Button variant="ghost" size="sm" className="h-8 px-2">
              <Settings className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>

      {/* Editor */}
      <div className="flex-1 overflow-hidden">
        <Editor
          ref={editorRef}
          initialState={editorState}
          onChange={(state) => setEditorState(state)}
          placeholder="Start writing your content..."
          className="h-full"
        />
      </div>

      {/* Save As Dialog */}
      <Dialog open={showSaveAsDialog} onOpenChange={setShowSaveAsDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Save As New Project</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="project-name">Project Name</Label>
              <Input
                id="project-name"
                value={newProjectName}
                onChange={(e) => setNewProjectName(e.target.value)}
                placeholder="Enter project name"
                className="mt-1"
              />
            </div>
            <div className="flex justify-end space-x-2">
              <Button variant="outline" onClick={() => setShowSaveAsDialog(false)}>
                Cancel
              </Button>
              <Button onClick={handleSaveAs} disabled={!newProjectName.trim()}>
                Save As
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Storage Limit Dialog */}
      <Dialog open={showStorageLimitDialog} onOpenChange={setShowStorageLimitDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Storage Limit</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="storage-limit">Storage Limit (MB)</Label>
              <Input
                id="storage-limit"
                value={newStorageLimit}
                onChange={(e) => setNewStorageLimit(e.target.value)}
                placeholder="Enter limit in MB (leave empty for no limit)"
                type="number"
                min="1"
                className="mt-1"
              />
              <p className="text-sm text-muted-foreground mt-1">Current usage: {(totalStorageUsed / 1024 / 1024).toFixed(2)} MB</p>
            </div>
            <div className="flex justify-end space-x-2">
              <Button variant="outline" onClick={() => setShowStorageLimitDialog(false)}>
                Cancel
              </Button>
              <Button onClick={() => handleSetStorageLimit(newStorageLimit)}>Set Limit</Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
