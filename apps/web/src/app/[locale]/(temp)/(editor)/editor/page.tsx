'use client';

import { Editor } from '@/components/content/editor/lexical-editor';
import { Button } from '@/components/editor/ui/button';
import { Input } from '@/components/editor/ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/editor/ui/dialog';
import { Label } from '@/components/editor/ui/label';
import { Save, SaveAll, HardDrive, Settings } from 'lucide-react';
import { useState, useEffect, useRef } from 'react';
import { toast } from 'sonner';
import { Sun, Moon } from 'lucide-react';
import type { LexicalEditor } from 'lexical';
import { OpenProjectDialog } from '@/components/editor/ui/editor/open-project-dialog';
import { CreateProjectDialog } from '@/components/editor/ui/editor/create-project-dialog';
import { EnhancedStorageAdapter } from '@/lib/storage/enhanced-storage-adapter';
import { SyncSettingsDialog } from '@/components/editor/ui/editor/sync-settings-dialog';
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

  async saveTags(tags: string[]): Promise<void> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([TAGS_STORE_NAME], 'readwrite');
      const store = transaction.objectStore(TAGS_STORE_NAME);

      const promises = tags.map((tag) => {
        return new Promise<void>((tagResolve, tagReject) => {
          const getRequest = store.get(tag);
          getRequest.onsuccess = () => {
            const existingTag = getRequest.result;
            const tagData = {
              name: tag,
              usageCount: existingTag ? existingTag.usageCount + 1 : 1,
              createdAt: existingTag ? existingTag.createdAt : new Date().toISOString(),
              updatedAt: new Date().toISOString(),
            };

            const putRequest = store.put(tagData);
            putRequest.onsuccess = () => tagResolve();
            putRequest.onerror = () => tagReject(putRequest.error);
          };
          getRequest.onerror = () => tagReject(getRequest.error);
        });
      });

      Promise.all(promises)
        .then(() => resolve())
        .catch(reject);
    });
  }

  async getAllTags(): Promise<Array<{ name: string; usageCount: number }>> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([TAGS_STORE_NAME], 'readonly');
      const store = transaction.objectStore(TAGS_STORE_NAME);
      const request = store.getAll();

      request.onsuccess = () => {
        const tags = request.result.sort((a, b) => b.usageCount - a.usageCount);
        resolve(tags);
      };
      request.onerror = () => reject(request.error);
    });
  }

  async searchProjects(searchTerm: string, tags: string[], filterMode: 'all' | 'any' = 'any'): Promise<ProjectData[]> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([STORE_NAME], 'readonly');
      const store = transaction.objectStore(STORE_NAME);
      const request = store.getAll();

      request.onsuccess = () => {
        let projects = request.result as ProjectData[];

        // Filter by search term (name)
        if (searchTerm) {
          projects = projects.filter((project) => project.name.toLowerCase().includes(searchTerm.toLowerCase()));
        }

        // Filter by tags
        if (tags.length > 0) {
          projects = projects.filter((project) => {
            if (!project.tags || project.tags.length === 0) return false;

            if (filterMode === 'all') {
              // Project must have ALL selected tags
              return tags.every((tag) => project.tags.includes(tag));
            } else {
              // Project must have ANY of the selected tags
              return tags.some((tag) => project.tags.includes(tag));
            }
          });
        }

        // Sort by updatedAt descending (most recent first)
        projects.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime());
        resolve(projects);
      };
      request.onerror = () => reject(request.error);
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

        putRequest.onsuccess = async () => {
          // Save tags to tags store
          if (tags.length > 0) {
            try {
              await this.saveTags(tags);
            } catch (error) {
              console.error('Failed to save tags:', error);
            }
          }
          console.log(`Saved project "${name}" (${id}) to IndexedDB`);
          resolve();
        };

        putRequest.onerror = () => {
          console.error('Failed to save to IndexedDB:', putRequest.error);
          reject(putRequest.error);
        };
      };

      getRequest.onerror = () => {
        console.error('Failed to check existing project:', getRequest.error);
        reject(getRequest.error);
      };
    });
  }

  async load(id: string): Promise<ProjectData | null> {
    if (!this.db) {
      throw new Error('IndexedDB not initialized');
    }

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([STORE_NAME], 'readonly');
      const store = transaction.objectStore(STORE_NAME);
      const request = store.get(id);

      request.onsuccess = () => {
        if (request.result) {
          console.log(`Loaded project "${request.result.name}" (${id}) from IndexedDB`);
          resolve(request.result);
        } else {
          resolve(null);
        }
      };

      request.onerror = () => {
        console.error('Failed to load from IndexedDB:', request.error);
        reject(request.error);
      };
    });
  }

  async delete(key: string): Promise<void> {
    if (!this.db) {
      throw new Error('IndexedDB not initialized');
    }

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([STORE_NAME], 'readwrite');
      const store = transaction.objectStore(STORE_NAME);
      const request = store.delete(key);

      request.onsuccess = () => {
        console.log(`Deleted project "${key}" from IndexedDB`);
        resolve();
      };

      request.onerror = () => {
        console.error('Failed to delete from IndexedDB:', request.error);
        reject(request.error);
      };
    });
  }

  async list(): Promise<ProjectData[]> {
    if (!this.db) {
      throw new Error('IndexedDB not initialized');
    }

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

      request.onerror = () => {
        console.error('Failed to list projects from IndexedDB:', request.error);
        reject(request.error);
      };
    });
  }

  async getStorageInfo(): Promise<{ totalSize: number; projectCount: number }> {
    if (!this.db) {
      throw new Error('IndexedDB not initialized');
    }

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([STORE_NAME], 'readonly');
      const store = transaction.objectStore(STORE_NAME);
      const request = store.getAll();

      request.onsuccess = () => {
        const projects = request.result;
        let totalSize = 0;

        projects.forEach((project) => {
          totalSize += project.size || estimateSize(project.data);
        });

        resolve({
          totalSize,
          projectCount: projects.length,
        });
      };

      request.onerror = () => {
        console.error('Failed to get storage info from IndexedDB:', request.error);
        reject(request.error);
      };
    });
  }

  async getProjectInfo(id: string): Promise<{ size: number; createdAt: string; updatedAt: string } | null> {
    if (!this.db) {
      throw new Error('IndexedDB not initialized');
    }

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([STORE_NAME], 'readonly');
      const store = transaction.objectStore(STORE_NAME);
      const request = store.get(id);

      request.onsuccess = () => {
        if (request.result) {
          resolve({
            size: request.result.size || estimateSize(request.result.data),
            createdAt: request.result.createdAt || new Date().toISOString(),
            updatedAt: request.result.updatedAt || new Date().toISOString(),
          });
        } else {
          resolve(null);
        }
      };

      request.onerror = () => {
        console.error('Failed to get project info from IndexedDB:', request.error);
        reject(request.error);
      };
    });
  }
}

// Função para estimar o tamanho dos dados em KB
function estimateSize(data: string): number {
  return new Blob([data]).size / 1024;
}

// Função para formatar tamanho em KB/MB
function formatSize(sizeInKB: number): string {
  if (sizeInKB < 1024) {
    return `${sizeInKB.toFixed(1)}KB`;
  } else {
    return `${(sizeInKB / 1024).toFixed(1)}MB`;
  }
}

export default function Page() {
  const [editorState, setEditorState] = useState<string>('');
  const [currentProjectId, setCurrentProjectId] = useState<string>('');
  const [currentProjectName, setCurrentProjectName] = useState<string>('');
  const [saveAsDialogOpen, setSaveAsDialogOpen] = useState(false);
  const [openDialogOpen, setOpenDialogOpen] = useState(false);
  const [newProjectName, setNewProjectName] = useState('');
  const [savedProjects, setSavedProjects] = useState<ProjectData[]>([]);
  const [isLoadingProject, setIsLoadingProject] = useState(false);
  const [currentProjectSize, setCurrentProjectSize] = useState<number>(0);
  const [totalStorageUsed, setTotalStorageUsed] = useState<number>(0);
  const setLoadingRef = useRef<((loading: boolean) => void) | null>(null);

  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [projectToDelete, setProjectToDelete] = useState<{ id: string; name: string } | null>(null);

  // Estado para modo escuro
  const [isDark, setIsDark] = useState(typeof window !== 'undefined' ? document.documentElement.classList.contains('dark') : false);

  // Alternar tema
  const toggleTheme = () => {
    if (typeof window === 'undefined') return;
    const root = document.documentElement;
    if (root.classList.contains('dark')) {
      root.classList.remove('dark');
      setIsDark(false);
      localStorage.setItem('theme', 'light');
    } else {
      root.classList.add('dark');
      setIsDark(true);
      localStorage.setItem('theme', 'dark');
    }
  };

  // Sincronizar com localStorage (opcional)
  useEffect(() => {
    if (typeof window === 'undefined') return;
    const saved = localStorage.getItem('theme');
    if (saved === 'dark') {
      document.documentElement.classList.add('dark');
      setIsDark(true);
    } else if (saved === 'light') {
      document.documentElement.classList.remove('dark');
      setIsDark(false);
    }
  }, []);

  // Storage limit system
  const [storageLimit, setStorageLimit] = useState<number | null>(100); // null = unlimited, number = MB limit
  const [showStorageLimitDialog, setShowStorageLimitDialog] = useState(false);
  const [newStorageLimit, setNewStorageLimit] = useState('');

  // Calculate storage usage percentage
  const getStorageUsagePercentage = (): number => {
    if (!storageLimit) return 0;
    return (totalStorageUsed / 1024 / storageLimit) * 100; // Convert KB to MB
  };

  // Format storage size with appropriate units
  const formatStorageSize = (sizeInKB: number): string => {
    const sizeInMB = sizeInKB / 1024;
    const sizeInGB = sizeInMB / 1024;

    if (sizeInGB >= 1) {
      return `${sizeInGB.toFixed(1)}GB`;
    } else if (sizeInMB >= 1) {
      return `${sizeInMB.toFixed(1)}MB`;
    } else {
      return `${sizeInKB.toFixed(1)}KB`;
    }
  };

  // Check if storage operations should be blocked
  const isStorageNearLimit = (): boolean => {
    if (!storageLimit) return false;
    return getStorageUsagePercentage() >= 90;
  };

  const isStorageAtLimit = (): boolean => {
    if (!storageLimit) return false;
    return getStorageUsagePercentage() >= 100;
  };

  // Get storage limit display
  const getStorageLimitDisplay = (): string => {
    if (!storageLimit) return '∞';
    return `${storageLimit}MB`;
  };

  // Replace this line:
  // const dbStorage = useRef<IndexedDBStorage>(new IndexedDBStorage())
  // With this:
  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter());
  const [isDbInitialized, setIsDbInitialized] = useState(false);

  // Add these state variables after the existing ones:
  const [syncStats, setSyncStats] = useState<any>(null);
  const [showSyncStatus, setShowSyncStatus] = useState(false);
  const [showSyncSettingsDialog, setShowSyncSettingsDialog] = useState(false);

  const editorRef = useRef<LexicalEditor | null>(null);

  // Tamanho recomendado em KB (500KB)
  const RECOMMENDED_SIZE_KB = 500;

  const [isFirstTime, setIsFirstTime] = useState(true);

  const [projectTags, setProjectTags] = useState<string[]>([]);
  const [availableTags, setAvailableTags] = useState<Array<{ name: string; usageCount: number }>>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(10);
  const [filteredProjects, setFilteredProjects] = useState<ProjectData[]>([]);
  const [totalProjects, setTotalProjects] = useState(0);

  const [tagInput, setTagInput] = useState('');
  const [showTagDropdown, setShowTagDropdown] = useState(false);

  // Add new state for tag search
  const [tagSearchInput, setTagSearchInput] = useState('');

  const [tagFilterMode, setTagFilterMode] = useState<'all' | 'any'>('any');

  const [createDialogOpen, setCreateDialogOpen] = useState(false);

  // Initialize IndexedDB and load projects
  useEffect(() => {
    const initDB = async () => {
      try {
        await dbStorage.current.init();
        setIsDbInitialized(true);
        await loadSavedProjectsList();
        await loadAvailableTags();
        await updateStorageInfo();
      } catch (error) {
        console.error('Failed to initialize IndexedDB:', error);
        toast.error('Storage error', {
          description: 'Unable to initialize database. Some features may not work.',
          duration: 5000,
          icon: '⚠️',
        });
      }
    };

    initDB();
  }, []);

  // Force open dialog on first visit
  useEffect(() => {
    if (!isDbInitialized) return;

    // Check if it's the first time (no current project and no saved projects)
    if (isFirstTime && !currentProjectName && savedProjects.length === 0) {
      setOpenDialogOpen(true);
    } else if (isFirstTime && savedProjects.length > 0) {
      // If there are saved projects but no current project, also show dialog
      setOpenDialogOpen(true);
    }
    setIsFirstTime(false);
  }, [savedProjects, currentProjectName, isFirstTime, isDbInitialized]);

  // Atualizar informações de armazenamento sempre que o editor mudar
  useEffect(() => {
    if (editorState) {
      const size = estimateSize(editorState);
      setCurrentProjectSize(size);
    }
  }, [editorState]);

  // Atualizar informações de armazenamento
  const updateStorageInfo = async () => {
    if (!isDbInitialized) return;

    try {
      const storageInfo = await dbStorage.current.getStorageInfo();
      setTotalStorageUsed(storageInfo.totalSize);
    } catch (error) {
      console.error('Error updating storage info:', error);
    }
  };

  const storageAdapter = {
    save: async (id: string, name: string, data: string, tags: string[] = []) => {
      if (!id || !name || !data) {
        console.warn('Invalid id, name or data');
        return;
      }

      if (!isDbInitialized) {
        throw new Error('Database not initialized');
      }

      const originalSize = estimateSize(data);
      console.log(`Saving project "${name}" (${id}) - Size: ${formatSize(originalSize)}`);

      try {
        await dbStorage.current.save(id, name, data, tags);
        await updateStorageInfo();
        console.log(`Saved project "${name}" (${id}) successfully`);
      } catch (error) {
        console.error('Failed to save project:', error);
        throw error;
      }
    },

    load: async (id: string): Promise<ProjectData | null> => {
      if (!isDbInitialized) {
        throw new Error('Database not initialized');
      }

      try {
        const projectData = await dbStorage.current.load(id);
        return projectData;
      } catch (error) {
        console.error('Failed to load project:', error);
        return null;
      }
    },

    delete: async (id: string) => {
      if (!isDbInitialized) {
        throw new Error('Database not initialized');
      }

      try {
        await dbStorage.current.delete(id);
        await updateStorageInfo();
      } catch (error) {
        console.error('Failed to delete project:', error);
        throw error;
      }
    },

    list: async (): Promise<ProjectData[]> => {
      if (!isDbInitialized) {
        return [];
      }

      try {
        const projects = await dbStorage.current.list();
        return projects;
      } catch (error) {
        console.error('Failed to list projects:', error);
        return [];
      }
    },

    getProjectInfo: async (id: string) => {
      if (!isDbInitialized) {
        return null;
      }

      try {
        return await dbStorage.current.getProjectInfo(id);
      } catch (error) {
        console.error('Failed to get project info:', error);
        return null;
      }
    },

    searchProjects: async (searchTerm: string, tags: string[], filterMode: 'all' | 'any' = 'any'): Promise<ProjectData[]> => {
      if (!isDbInitialized) {
        return [];
      }

      try {
        return await dbStorage.current.searchProjects(searchTerm, tags, filterMode);
      } catch (error) {
        console.error('Failed to search projects:', error);
        return [];
      }
    },
  };

  const loadSavedProjectsList = async () => {
    try {
      const projects = await storageAdapter.list();
      setSavedProjects(projects);
    } catch (error) {
      console.error('Failed to load projects list:', error);
    }
  };

  const loadAvailableTags = async () => {
    try {
      const tags = await dbStorage.current.getAllTags();
      setAvailableTags(tags);
    } catch (error) {
      console.error('Failed to load tags:', error);
    }
  };

  useEffect(() => {
    const filterProjects = async () => {
      if (!isDbInitialized) return;

      try {
        let projects: ProjectData[];

        if (searchTerm || selectedTags.length > 0) {
          projects = await storageAdapter.searchProjects(searchTerm, selectedTags, tagFilterMode);
        } else {
          projects = await storageAdapter.list();
        }

        setTotalProjects(projects.length);
        setFilteredProjects(projects);
        setCurrentPage(1); // Reset to first page when filtering
      } catch (error) {
        console.error('Failed to filter projects:', error);
      }
    };

    filterProjects();
  }, [searchTerm, selectedTags, savedProjects, isDbInitialized, tagFilterMode]);

  // Add this useEffect after the existing ones:
  useEffect(() => {
    if (!isDbInitialized) return;

    const updateSyncStats = async () => {
      try {
        const stats = await dbStorage.current.getSyncStats();
        setSyncStats(stats);
      } catch (error) {
        console.error('Failed to get sync stats:', error);
      }
    };

    // Update sync stats every 5 seconds
    const interval = setInterval(updateSyncStats, 5000);
    updateSyncStats(); // Initial update

    // Setup sync event listeners
    dbStorage.current.onSyncStart(() => {
      console.log('Sync started');
      updateSyncStats();
    });

    dbStorage.current.onSyncComplete((stats) => {
      console.log('Sync completed:', stats);
      updateSyncStats();
      if (stats.processed > 0) {
        toast.success('Synchronization completed', {
          description: `${stats.processed} synchronized projects`,
          duration: 3000,
          icon: '🔄',
        });
      }
    });

    dbStorage.current.onSyncError((error) => {
      console.error('Sync error:', error);
      updateSyncStats();
      toast.error('Synchronization error', {
        description: 'Some projects may not be synchronized',
        duration: 4000,
        icon: '⚠️',
      });
    });

    return () => {
      clearInterval(interval);
    };
  }, [isDbInitialized]);

  const handleSave = async () => {
    if (!currentProjectId) {
      setSaveAsDialogOpen(true);
      return;
    }

    // Check storage limit before saving
    if (isStorageAtLimit()) {
      toast.error('Overcrowded storage', {
        description: 'Storage limit reached. Delete projects to free up space.',
        duration: 5000,
        icon: '🚫',
      });
      return;
    }

    // Get current editor state if editorState is empty
    let stateToSave = editorState;
    if (!stateToSave && editorRef.current) {
      try {
        const currentState = editorRef.current.getEditorState();
        stateToSave = JSON.stringify(currentState.toJSON());
      } catch (error) {
        console.error('Failed to get editor state:', error);
        toast.error('Error in editor', {
          description: 'Could not get publisher content',
          duration: 4000,
          icon: '⚠️',
        });
        return;
      }
    }

    if (!stateToSave || stateToSave.trim() === '') {
      toast.error('Nothing to save', {
        description: 'The editor is empty. Add content before saving.',
        duration: 3000,
        icon: '📄',
      });
      return;
    }

    try {
      await storageAdapter.save(currentProjectId, currentProjectName, stateToSave, projectTags);

      // Check storage usage after save and show appropriate notification
      const usagePercentage = getStorageUsagePercentage();

      if (storageLimit && usagePercentage >= 90) {
        toast.warning('Saved Project - Little Space', {
          description: `"${currentProjectName}" saved. Space remaining: ${(100 - usagePercentage).toFixed(1)}%`,
          duration: 4000,
          icon: '⚠️',
        });
      } else {
        toast.success('Project saved successfully', {
          description: `"${currentProjectName}" was saved in the database.`,
          duration: 3000,
          icon: '💾',
        });
      }
    } catch (error: any) {
      console.error('Save error:', error);
      toast.error('Error saving', {
        description: 'Could not save project. Please try again.',
        duration: 4000,
        icon: '❌',
      });
    }
  };

  const handleSaveAs = async () => {
    if (!newProjectName.trim()) {
      toast.error('Name required', {
        description: 'Please enter a name for the project',
        duration: 3000,
        icon: '✏️',
      });
      return;
    }

    // Check storage limit before saving new project
    if (isStorageAtLimit()) {
      toast.error('Storage Full', {
        description: 'Storage limit reached. Delete projects to free up space.',
        duration: 5000,
        icon: '🚫',
      });
      return;
    }

    // Check if project with same name already exists
    const existingProjects = await storageAdapter.list();
    if (existingProjects.some((p) => p.name === newProjectName.trim())) {
      // Generate suggested name with version number
      let suggestedName = `${newProjectName.trim()}-v2`;
      let counter = 2;

      // Keep incrementing until we find an available name
      while (existingProjects.some((p) => p.name === suggestedName)) {
        counter++;
        suggestedName = `${newProjectName.trim()}-v${counter}`;
      }

      toast.error('Name already exists', {
        description: `There is already a project named "${newProjectName.trim()}". Suggestion: ${suggestedName}`,
        duration: 5000,
        icon: '🚫',
      });
      return;
    }

    // Get current editor state if editorState is empty
    let stateToSave = editorState;
    if (!stateToSave && editorRef.current) {
      try {
        const currentState = editorRef.current.getEditorState();
        stateToSave = JSON.stringify(currentState.toJSON());
      } catch (error) {
        console.error('Failed to get editor state:', error);
        toast.error('Error in editor', {
          description: 'Could not get publisher content.',
          duration: 4000,
          icon: '⚠️',
        });
        return;
      }
    }

    if (!stateToSave || stateToSave.trim() === '') {
      toast.error('Nothing to save', {
        description: 'The editor is empty. Add content before saving.',
        duration: 3000,
        icon: '📄',
      });
      return;
    }

    try {
      const newProjectId = generateProjectId();
      await storageAdapter.save(newProjectId, newProjectName, stateToSave, projectTags);
      setCurrentProjectId(newProjectId);
      setCurrentProjectName(newProjectName);
      setNewProjectName('');
      setSaveAsDialogOpen(false);
      await loadSavedProjectsList();

      // Check storage usage after save
      const usagePercentage = getStorageUsagePercentage();

      if (storageLimit && usagePercentage >= 90) {
        toast.warning('Project created - Little space', {
          description: `"${newProjectName}" created. Remaining space: ${(100 - usagePercentage).toFixed(1)}%`,
          duration: 4000,
          icon: '⚠️',
        });
      } else {
        toast.success('New project created', {
          description: `"${newProjectName}" was created and saved successfully.`,
          duration: 3000,
          icon: '🎉',
        });
      }
    } catch (error: any) {
      console.error('Save as error:', error);
      toast.error('Error creating project', {
        description: 'Could not create project. Please try again.',
        duration: 4000,
        icon: '❌',
      });
    }
  };

  const handleOpen = async (projectId: string) => {
    try {
      const projectData = await storageAdapter.load(projectId);
      if (projectData && editorRef.current) {
        try {
          // Ativar o estado de carregamento
          if (setLoadingRef.current) {
            setLoadingRef.current(true);
          }

          const editorState = editorRef.current.parseEditorState(projectData.data);
          editorRef.current.setEditorState(editorState);

          // Aguardar um pouco para que os nós sejam renderizados
          await new Promise((resolve) => setTimeout(resolve, 100));

          // Desativar o estado de carregamento
          if (setLoadingRef.current) {
            setLoadingRef.current(false);
          }

          setCurrentProjectId(projectData.id);
          setCurrentProjectName(projectData.name);
          setProjectTags(projectData.tags || []);
          setOpenDialogOpen(false);
          setIsFirstTime(false); // Mark as no longer first time
          toast.success('Project loaded', {
            description: `"${projectData.name}" was opened successfully`,
            duration: 2500,
            icon: '📂',
          });
        } catch (error) {
          // Desativar o estado de carregamento em caso de erro
          if (setLoadingRef.current) {
            setLoadingRef.current(false);
          }
          toast.error('Error loading project', {
            description: 'The project file is corrupt or in an invalid format',
            duration: 4000,
            icon: '❌',
          });
        }
      } else {
        toast.error('Project not found', {
          description: 'Unable to locate project file',
          duration: 3000,
          icon: '🔍',
        });
      }
    } catch (error) {
      console.error('Open error:', error);
      toast.error('Error opening project', {
        description: 'Could not open project. Please try again',
        duration: 4000,
        icon: '❌',
      });
    }
  };

  const handleConfirmDelete = (projectId: string, projectName: string) => {
    setProjectToDelete({ id: projectId, name: projectName });
    setDeleteDialogOpen(true);
  };

  const handleDelete = async (projectId: string, projectName: string) => {
    try {
      await storageAdapter.delete(projectId);
      await loadSavedProjectsList();

      // If we're deleting the currently active project, reset the editor
      if (currentProjectId === projectId) {
        setCurrentProjectId('');
        setCurrentProjectName('');
        setProjectTags([]);
        setEditorState('');

        // Reset the editor to empty state
        if (editorRef.current) {
          editorRef.current.setEditorState(
            editorRef.current.parseEditorState(
              '{"root":{"children":[{"children":[],"direction":null,"format":"","indent":0,"type":"paragraph","version":1}],"direction":null,"format":"","indent":0,"type":"root","version":1}}',
            ),
          );
        }
      }

      toast.success('Deleted project', {
        description: `"${projectName}" has been permanently removed`,
        duration: 3000,
        icon: '🗑️',
      });
    } catch (error) {
      console.error('Delete error:', error);
      toast.error('Error deleting project', {
        description: 'Could not delete project. Please try again.',
        duration: 4000,
        icon: '❌',
      });
    }
  };

  const handleNewProject = () => {
    if (editorRef.current) {
      editorRef.current.setEditorState(
        editorRef.current.parseEditorState(
          '{"root":{"children":[{"children":[],"direction":null,"format":"","indent":0,"type":"paragraph","version":1}],"direction":null,"format":"","indent":0,"type":"root","version":1}}',
        ),
      );
      setCurrentProjectId('');
      setCurrentProjectName('');
      setProjectTags([]);
      setOpenDialogOpen(false);
      setIsFirstTime(false); // Mark as no longer first time
      toast.success('New project started', {
        description: 'Clean editor ready to create content',
        duration: 2500,
        icon: '📝',
      });
    }
  };

  // Determinar a cor do indicador de tamanho
  const getSizeIndicatorColor = () => {
    if (currentProjectSize > RECOMMENDED_SIZE_KB * 2) return 'text-red-600';
    if (currentProjectSize > RECOMMENDED_SIZE_KB) return 'text-amber-600';
    return 'text-green-600';
  };

  const [autoSaveEnabled, setAutoSaveEnabled] = useState(false);

  // Auto-save functionality
  useEffect(() => {
    if (!autoSaveEnabled || !currentProjectId || !editorState || !isDbInitialized) return;

    const autoSaveTimer = setTimeout(async () => {
      try {
        await storageAdapter.save(currentProjectId, currentProjectName, editorState, projectTags);
        // Show a very subtle auto-save notification
        toast.success('Auto-saved', {
          description: 'Changes saved automatically',
          duration: 1500,
          icon: '💾',
          style: {
            opacity: 0.8,
            fontSize: '0.875rem',
          },
        });
        console.log('Auto-saved project:', currentProjectName);
      } catch (error) {
        console.error('Auto-save failed:', error);
        toast.error('Auto-save failed', {
          description: 'Save manually to ensure',
          duration: 2000,
          icon: '⚠️',
        });
      }
    }, 2000); // Auto-save after 2 seconds of inactivity

    return () => clearTimeout(autoSaveTimer);
  }, [editorState, autoSaveEnabled, currentProjectId, currentProjectName, projectTags, isDbInitialized]);

  const handleSetStorageLimit = () => {
    const limitValue = newStorageLimit.trim();

    if (limitValue === '' || limitValue === '0') {
      // Remove limit (unlimited)
      setStorageLimit(null);
      setNewStorageLimit('');
      setShowStorageLimitDialog(false);
      toast.success('Limit removed', {
        description: 'Storage is now unlimited',
        duration: 3000,
        icon: '∞',
      });
      return;
    }

    const limit = Number.parseFloat(limitValue);
    if (isNaN(limit) || limit <= 0) {
      toast.error('Invalid value', {
        description: 'Enter a valid number in MB or leave blank for unlimited',
        duration: 3000,
        icon: '❌',
      });
      return;
    }

    // Check if current usage exceeds new limit
    const currentUsageMB = totalStorageUsed / 1024;
    if (currentUsageMB > limit) {
      toast.error('Very low limit', {
        description: `Current usage (${formatStorageSize(totalStorageUsed)}) exceeds the proposed limit of ${limit}MB`,
        duration: 4000,
        icon: '⚠️',
      });
      return;
    }

    setStorageLimit(limit);
    setNewStorageLimit('');
    setShowStorageLimitDialog(false);
    toast.success('Configured limit', {
      description: `Storage limit set to ${limit}MB`,
      duration: 3000,
      icon: '📊',
    });
  };

  // Close tag dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showTagDropdown) {
        const target = event.target as Element;
        if (!target.closest('.relative')) {
          setShowTagDropdown(false);
        }
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [showTagDropdown]);

  return (
    <>
      {/* Botão de alternância no topo direito */}
      <button
        onClick={toggleTheme}
        className="fixed top-4 right-4 z-50 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-full p-2 shadow hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
        aria-label="Alternar modo claro/escuro"
        type="button"
      >
        {isDark ? <Sun className="w-5 h-5 text-yellow-400" /> : <Moon className="w-5 h-5 text-gray-300" />}
      </button>

      <div className="container mx-auto py-10">
        <div className="mx-auto max-w-4xl space-y-8">
          {/* Header Section */}
          <div className="text-center space-y-4">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-50 dark:bg-blue-900 text-blue-700 dark:text-blue-200 text-sm font-medium">
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M3 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z"
                  clipRule="evenodd"
                />
              </svg>
              Rich Text Editor
            </div>
            <h1 className="text-4xl font-bold tracking-tight text-gray-900 dark:text-gray-100">Create Your Content</h1>
            <p className="text-lg text-muted-foreground max-w-2xl mx-auto dark:text-gray-300">
              Use our powerful editor to create engaging content with rich formatting, media, quizzes, and more
            </p>
          </div>

          {/* Project Management Toolbar */}
          <div className="bg-white dark:bg-gray-900 rounded-xl border dark:border-gray-700 shadow-sm">
            <div className="flex items-center justify-between p-6">
              {/* Left Section - Project Info */}
              <div className="flex items-center gap-8">
                <div className="flex flex-col">
                  <div className="flex items-center gap-3 mb-1">
                    <button
                      onClick={() => setAutoSaveEnabled(!autoSaveEnabled)}
                      className="flex items-center gap-2 px-2 py-1 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors cursor-pointer"
                      disabled={!isDbInitialized}
                    >
                      <div
                        className={`w-3 h-3 rounded-full ${autoSaveEnabled && isDbInitialized ? 'bg-green-500 animate-pulse' : 'bg-gray-400 dark:bg-gray-600'}`}
                      ></div>
                      <span
                        className={`text-sm font-medium ${
                          autoSaveEnabled && isDbInitialized ? 'text-green-700 dark:text-green-400' : 'text-gray-500 dark:text-gray-300'
                        }`}
                      >
                        {autoSaveEnabled && isDbInitialized ? 'Auto-save' : 'Manual'}
                      </span>
                    </button>
                    <div className="h-4 w-px bg-gray-300 dark:bg-gray-700"></div>
                    <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{currentProjectName || 'Untitled Project'}</h2>
                  </div>
                  <div className="flex items-center gap-4 text-sm text-gray-500 dark:text-gray-300">
                    <div className="flex items-center gap-2">
                      <HardDrive className="w-4 h-4" />
                      <span className={getSizeIndicatorColor()}>{formatSize(currentProjectSize)}</span>
                      <span className="text-gray-400 dark:text-gray-500">/</span>
                      <span className="text-gray-400 dark:text-gray-500">{formatSize(RECOMMENDED_SIZE_KB)} recommended</span>
                    </div>
                    {!isDbInitialized && (
                      <>
                        <div className="h-3 w-px bg-gray-300 dark:bg-gray-700"></div>
                        <span className="inline-flex items-center px-2 py-1 rounded-full text-xs bg-amber-100 dark:bg-amber-800 text-amber-800 dark:text-amber-100">
                          Initializing DB...
                        </span>
                      </>
                    )}
                  </div>
                </div>
              </div>

              {/* Right Section - Actions */}
              <div className="flex items-center gap-3">
                <div className="flex items-center gap-2 px-3 py-2 bg-gray-50 dark:bg-gray-800 rounded-lg">
                  <span className="text-xs text-gray-500 dark:text-gray-300">Storage:</span>
                  <span className="text-xs font-medium text-gray-700 dark:text-gray-100">{formatStorageSize(totalStorageUsed)}</span>
                  <span className="text-xs text-gray-400 dark:text-gray-500">/</span>
                  <button
                    onClick={() => setShowStorageLimitDialog(true)}
                    className="text-xs font-medium text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 cursor-pointer"
                  >
                    {getStorageLimitDisplay()}
                  </button>
                  {storageLimit && (
                    <>
                      <span className="text-xs text-gray-400 dark:text-gray-500">•</span>
                      <span
                        className={`text-xs font-medium ${
                          getStorageUsagePercentage() >= 90 ? 'text-red-600' : getStorageUsagePercentage() >= 70 ? 'text-amber-600' : 'text-green-600'
                        }`}
                      >
                        {getStorageUsagePercentage().toFixed(1)}%
                      </span>
                    </>
                  )}
                </div>
                {syncStats && (
                  <>
                    <div className="h-8 w-px bg-gray-200 dark:bg-gray-700"></div>
                    <button
                      onClick={() => setShowSyncStatus(!showSyncStatus)}
                      className="flex items-center gap-2 px-3 py-2 bg-gray-50 dark:bg-gray-800 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                    >
                      <div
                        className={`w-2 h-2 rounded-full ${
                          syncStats.isOnline
                            ? syncStats.isSyncing
                              ? 'bg-blue-500 animate-pulse'
                              : 'bg-green-500'
                            : syncConfig.isEnabled()
                              ? 'bg-red-500'
                              : 'bg-gray-400'
                        }`}
                      ></div>
                      <span className="text-xs text-gray-500 dark:text-gray-300">
                        {!syncConfig.isEnabled() ? 'Sync Off' : syncStats.isOnline ? (syncStats.isSyncing ? 'Syncing...' : 'Online') : 'Offline'}
                      </span>
                      {syncStats.queue.pending > 0 && (
                        <span className="text-xs bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 px-1.5 py-0.5 rounded-full">
                          {syncStats.queue.pending}
                        </span>
                      )}
                    </button>
                    <button
                      onClick={() => setShowSyncSettingsDialog(true)}
                      className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg transition-colors"
                      title="Configurações de Sincronização"
                    >
                      <Settings className="w-4 h-4 text-gray-500 dark:text-gray-400" />
                    </button>
                  </>
                )}
                <div className="h-8 w-px bg-gray-200 dark:bg-gray-700"></div>

                <Button variant="outline" size="sm" onClick={handleSave} className="gap-2 bg-transparent" disabled={!isDbInitialized}>
                  <Save className="w-4 h-4" />
                  Save
                </Button>

                <Dialog open={saveAsDialogOpen} onOpenChange={setSaveAsDialogOpen}>
                  <DialogTrigger asChild>
                    <Button variant="outline" size="sm" className="gap-2 bg-transparent" disabled={!isDbInitialized}>
                      <SaveAll className="w-4 h-4" />
                      Save As
                    </Button>
                  </DialogTrigger>
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle>Save Project As</DialogTitle>
                    </DialogHeader>
                    <div className="space-y-4">
                      <div>
                        <Label htmlFor="project-name">Project Name</Label>
                        <Input
                          id="project-name"
                          value={newProjectName}
                          onChange={(e) => setNewProjectName(e.target.value)}
                          placeholder="Enter project name..."
                          onKeyDown={(e) => e.key === 'Enter' && handleSaveAs()}
                          className="mt-1"
                        />
                      </div>
                      <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                        <span className="text-sm text-gray-600">Project size:</span>
                        <span className={`text-sm font-medium ${getSizeIndicatorColor()}`}>{formatSize(currentProjectSize)}</span>
                      </div>
                      <div className="flex justify-end gap-2">
                        <Button variant="outline" onClick={() => setSaveAsDialogOpen(false)}>
                          Cancel
                        </Button>
                        <Button onClick={handleSaveAs}>Save Project</Button>
                      </div>
                    </div>
                  </DialogContent>
                </Dialog>

                <OpenProjectDialog
                  open={openDialogOpen}
                  onOpenChange={setOpenDialogOpen}
                  isFirstTime={isFirstTime}
                  isDbInitialized={isDbInitialized}
                  storageAdapter={storageAdapter}
                  availableTags={availableTags}
                  editorRef={editorRef}
                  setLoadingRef={setLoadingRef}
                  onProjectLoad={(projectData) => {
                    setCurrentProjectId(projectData.id);
                    setCurrentProjectName(projectData.name);
                    setProjectTags(projectData.tags || []);
                    setIsFirstTime(false);
                  }}
                  onProjectsListUpdate={loadSavedProjectsList}
                  onCreateNew={() => setCreateDialogOpen(true)}
                  currentProjectName={currentProjectName}
                  isStorageNearLimit={isStorageNearLimit}
                  getStorageUsagePercentage={getStorageUsagePercentage}
                  storageLimit={storageLimit}
                  totalStorageUsed={totalStorageUsed}
                  formatStorageSize={formatStorageSize}
                />

                <Dialog open={showStorageLimitDialog} onOpenChange={setShowStorageLimitDialog}>
                  <DialogContent className="max-w-md">
                    <DialogHeader>
                      <DialogTitle>Configure Storage Limit</DialogTitle>
                    </DialogHeader>
                    <div className="space-y-4">
                      <div>
                        <Label htmlFor="storage-limit">Limit in MB (leave empty for unlimited)</Label>
                        <Input
                          id="storage-limit"
                          type="number"
                          value={newStorageLimit}
                          onChange={(e) => setNewStorageLimit(e.target.value)}
                          placeholder="Ex: 100 (para 100MB)"
                          onKeyDown={(e) => e.key === 'Enter' && handleSetStorageLimit()}
                          className="mt-1"
                        />
                      </div>

                      <div className="p-3 bg-gray-50 rounded-lg space-y-2">
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Current use:</span>
                          <span className="font-medium">{formatStorageSize(totalStorageUsed)}</span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Current limit:</span>
                          <span className="font-medium">{getStorageLimitDisplay()}</span>
                        </div>
                        {storageLimit && (
                          <div className="flex justify-between text-sm">
                            <span className="text-gray-600">Use:</span>
                            <span
                              className={`font-medium ${
                                getStorageUsagePercentage() >= 90 ? 'text-red-600' : getStorageUsagePercentage() >= 70 ? 'text-amber-600' : 'text-green-600'
                              }`}
                            >
                              {getStorageUsagePercentage().toFixed(1)}%
                            </span>
                          </div>
                        )}
                      </div>

                      <div className="text-xs text-gray-500 space-y-1">
                        <p>• Leave blank or enter 0 for unlimited storage</p>
                        <p>• Values in MB (1024 KB = 1 MB)</p>
                        <p>• Upon reaching 90%, new project creation will be blocked</p>
                        <p>• Upon reaching 100%, saving will be blocked</p>
                      </div>

                      <div className="flex justify-end gap-2">
                        <Button variant="outline" onClick={() => setShowStorageLimitDialog(false)}>
                          Cancel
                        </Button>
                        <Button onClick={handleSetStorageLimit}>Configuration</Button>
                      </div>
                    </div>
                  </DialogContent>
                </Dialog>

                <CreateProjectDialog
                  open={createDialogOpen}
                  onOpenChange={setCreateDialogOpen}
                  isDbInitialized={isDbInitialized}
                  storageAdapter={storageAdapter}
                  availableTags={availableTags}
                  onProjectCreate={(projectData) => {
                    // Reset editor to empty state
                    if (editorRef.current) {
                      const emptyState =
                        '{"root":{"children":[{"children":[],"direction":null,"format":"","indent":0,"type":"paragraph","version":1}],"direction":null,"format":"","indent":0,"type":"root","version":1}}';
                      editorRef.current.setEditorState(editorRef.current.parseEditorState(emptyState));
                    }

                    setCurrentProjectId(projectData.id);
                    setCurrentProjectName(projectData.name);
                    setProjectTags(projectData.tags);
                    setEditorState(
                      '{"root":{"children":[{"children":[],"direction":null,"format":"","indent":0,"type":"paragraph","version":1}],"direction":null,"format":"","indent":0,"type":"root","version":1}}',
                    );
                    setIsFirstTime(false);
                  }}
                  onProjectsListUpdate={loadSavedProjectsList}
                  onAvailableTagsUpdate={loadAvailableTags}
                  isStorageAtLimit={isStorageAtLimit}
                  generateProjectId={generateProjectId}
                />
              </div>
            </div>
          </div>

          {/* Editor Container */}
          <div className="bg-white dark:bg-gray-900 rounded-xl border dark:border-gray-700 shadow-sm">
            <div className="p-6 px-12 py-12">
              <Editor
                editorRef={editorRef}
                initialState={editorState}
                onChange={setEditorState}
                onLoadingChange={(setLoading) => {
                  setLoadingRef.current = setLoading;
                }}
              />
            </div>
          </div>

          {/* Help Section */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 pt-4">
            <div className="p-4 bg-blue-50 dark:bg-blue-900 rounded-lg">
              <h3 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Rich Formatting</h3>
              <p className="text-sm text-blue-700 dark:text-blue-200">Add headings, lists, links, and text formatting</p>
            </div>
            <div className="p-4 bg-green-50 dark:bg-green-900 rounded-lg">
              <h3 className="font-semibold text-green-900 dark:text-green-100 mb-2">Media & Content</h3>
              <p className="text-sm text-green-700 dark:text-green-200">Insert images, videos, code blocks, and more</p>
            </div>
            <div className="p-4 bg-purple-50 dark:bg-purple-900 rounded-lg">
              <h3 className="font-semibold text-purple-900 dark:text-purple-100 mb-2">Interactive Elements</h3>
              <p className="text-sm text-purple-700 dark:text-purple-200">Create quizzes, callouts, and interactive content</p>
            </div>
          </div>
        </div>
      </div>
      {/* Sync Status Dialog */}
      <Dialog open={showSyncStatus} onOpenChange={setShowSyncStatus}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Synchronization Status</DialogTitle>
          </DialogHeader>
          {syncStats && (
            <div className="space-y-4">
              <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                <span className="text-sm text-gray-600 dark:text-gray-300">Conection:</span>
                <div className="flex items-center gap-2">
                  <div className={`w-2 h-2 rounded-full ${syncStats.isOnline ? 'bg-green-500' : 'bg-red-500'}`}></div>
                  <span className="text-sm font-medium">{syncStats.isOnline ? 'Online' : 'Offline'}</span>
                </div>
              </div>

              <div className="space-y-2">
                <h4 className="text-sm font-medium">Sync Queue</h4>
                <div className="grid grid-cols-2 gap-2 text-xs">
                  <div className="p-2 bg-blue-50 dark:bg-blue-900 rounded">
                    <div className="font-medium text-blue-800 dark:text-blue-200">Pending</div>
                    <div className="text-blue-600 dark:text-blue-300">{syncStats.queue.pending}</div>
                  </div>
                  <div className="p-2 bg-yellow-50 dark:bg-yellow-900 rounded">
                    <div className="font-medium text-yellow-800 dark:text-yellow-200">Processing</div>
                    <div className="text-yellow-600 dark:text-yellow-300">{syncStats.queue.processing}</div>
                  </div>
                  <div className="p-2 bg-green-50 dark:bg-green-900 rounded">
                    <div className="font-medium text-green-800 dark:text-green-200">Completed</div>
                    <div className="text-green-600 dark:text-green-300">{syncStats.queue.completed}</div>
                  </div>
                  <div className="p-2 bg-red-50 dark:bg-red-900 rounded">
                    <div className="font-medium text-red-800 dark:text-red-200">Failed</div>
                    <div className="text-red-600 dark:text-red-300">{syncStats.queue.failed}</div>
                  </div>
                </div>
              </div>

              {syncStats.lastSync && (
                <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                  <span className="text-sm text-gray-600 dark:text-gray-300">Last sync:</span>
                  <div className="text-sm font-medium">{new Date(syncStats.lastSync).toLocaleString()}</div>
                </div>
              )}

              {syncStats.queue.failed > 0 && (
                <Button
                  onClick={async () => {
                    try {
                      await dbStorage.current.retryFailedSync();
                      toast.success('Trying again', {
                        description: 'Failed items have been re-queued',
                        duration: 3000,
                        icon: '🔄',
                      });
                    } catch (error) {
                      toast.error('Error trying again', {
                        description: 'Unable to reprocess items',
                        duration: 4000,
                        icon: '❌',
                      });
                    }
                  }}
                  className="w-full"
                  variant="outline"
                >
                  Retry Failed Items
                </Button>
              )}

              <div className="flex justify-end">
                <Button variant="outline" onClick={() => setShowSyncStatus(false)}>
                  Close
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
      {/* Sync Settings Dialog */}
      <SyncSettingsDialog open={showSyncSettingsDialog} onOpenChange={setShowSyncSettingsDialog} />
    </>
  );
}
