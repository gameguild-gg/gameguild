import { SyncManager } from '../sync/sync-manager';
import { HashManager } from '../sync/hash-manager';

interface ProjectData {
  id: string;
  name: string;
  data: string;
  tags: string[];
  size: number;
  createdAt: string;
  updatedAt: string;
  hash?: string;
  syncStatus?: 'synced' | 'pending' | 'conflict' | 'local-only';
}

interface ProjectMetadata {
  id: string;
  name: string;
  tags: string[];
  size: number;
  hash: string;
  createdAt: string;
  updatedAt: string;
  syncStatus?: 'synced' | 'pending' | 'conflict' | 'local-only';
}

export class EnhancedStorageAdapter {
  private db: IDBDatabase | null = null;
  private syncManager: SyncManager;
  private isInitialized = false;

  private readonly DB_NAME = 'GGEditorDB';
  private readonly DB_VERSION = 2; // Incremented for sync support
  private readonly STORE_NAME = 'projects';
  private readonly TAGS_STORE_NAME = 'tags';
  private readonly METADATA_STORE_NAME = 'project_metadata';

  constructor() {
    this.syncManager = new SyncManager();
  }

  async init(): Promise<void> {
    if (this.isInitialized) return;

    await Promise.all([this.initIndexedDB(), this.syncManager.init()]);

    this.isInitialized = true;
    console.log('Enhanced Storage Adapter initialized');
  }

  private async initIndexedDB(): Promise<void> {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.DB_NAME, this.DB_VERSION);

      request.onerror = () => reject(request.error);
      request.onsuccess = () => {
        this.db = request.result;
        resolve();
      };

      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result;

        // Create projects store
        if (!db.objectStoreNames.contains(this.STORE_NAME)) {
          const store = db.createObjectStore(this.STORE_NAME, { keyPath: 'id' });
          store.createIndex('name', 'name', { unique: false });
          store.createIndex('tags', 'tags', { unique: false, multiEntry: true });
          store.createIndex('createdAt', 'createdAt', { unique: false });
          store.createIndex('updatedAt', 'updatedAt', { unique: false });
          store.createIndex('syncStatus', 'syncStatus', { unique: false });
        }

        // Create tags store
        if (!db.objectStoreNames.contains(this.TAGS_STORE_NAME)) {
          const tagsStore = db.createObjectStore(this.TAGS_STORE_NAME, { keyPath: 'name' });
          tagsStore.createIndex('usageCount', 'usageCount', { unique: false });
          tagsStore.createIndex('createdAt', 'createdAt', { unique: false });
        }

        // Create metadata store for sync optimization
        if (!db.objectStoreNames.contains(this.METADATA_STORE_NAME)) {
          const metadataStore = db.createObjectStore(this.METADATA_STORE_NAME, { keyPath: 'id' });
          metadataStore.createIndex('hash', 'hash', { unique: false });
          metadataStore.createIndex('updatedAt', 'updatedAt', { unique: false });
        }
      };
    });
  }

  async save(id: string, name: string, data: string, tags: string[] = []): Promise<void> {
    if (!this.isInitialized) throw new Error('Storage adapter not initialized');

    const hash = await HashManager.generateHash(data);
    const now = new Date().toISOString();

    const projectData: ProjectData = {
      id,
      name,
      data,
      tags,
      size: this.estimateSize(data),
      hash,
      createdAt: now,
      updatedAt: now,
      syncStatus: 'pending',
    };

    // Check if project exists to preserve createdAt
    const existing = await this.load(id);
    if (existing) {
      projectData.createdAt = existing.createdAt;
    }

    // Save to IndexedDB
    await this.saveToIndexedDB(projectData);

    // Save tags
    if (tags.length > 0) {
      await this.saveTags(tags);
    }

    // Queue for sync
    await this.syncManager.queueProjectUpdate(projectData);

    console.log(`Saved project "${name}" (${id}) - Size: ${this.formatSize(projectData.size)}`);
  }

  private async saveToIndexedDB(projectData: ProjectData): Promise<void> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.STORE_NAME, this.METADATA_STORE_NAME], 'readwrite');

      // Save full project data
      const projectStore = transaction.objectStore(this.STORE_NAME);
      const projectRequest = projectStore.put(projectData);

      // Save metadata for sync optimization
      const metadataStore = transaction.objectStore(this.METADATA_STORE_NAME);
      const metadata: ProjectMetadata = {
        id: projectData.id,
        name: projectData.name,
        tags: projectData.tags,
        size: projectData.size,
        hash: projectData.hash!,
        createdAt: projectData.createdAt,
        updatedAt: projectData.updatedAt,
        syncStatus: projectData.syncStatus,
      };
      const metadataRequest = metadataStore.put(metadata);

      transaction.oncomplete = () => resolve();
      transaction.onerror = () => reject(transaction.error);
    });
  }

  async load(id: string): Promise<ProjectData | null> {
    if (!this.isInitialized) throw new Error('Storage adapter not initialized');

    // First try to load from local IndexedDB
    const localProject = await this.loadFromIndexedDB(id);

    if (!localProject) {
      // Project not found locally, try to download from server
      console.log(`Project ${id} not found locally, attempting download`);
      const serverProject = await this.syncManager.downloadProject(id);

      if (serverProject) {
        // Save downloaded project locally
        await this.saveToIndexedDB({
          ...serverProject,
          syncStatus: 'synced',
        });
        return serverProject;
      }

      return null;
    }

    // Check if local project needs sync with server
    const syncedProject = await this.syncManager.syncProjectIfNeeded(localProject);
    if (syncedProject) {
      // Update local project with server version
      await this.saveToIndexedDB({
        ...syncedProject,
        syncStatus: 'synced',
      });
      return syncedProject;
    }

    return localProject;
  }

  private async loadFromIndexedDB(id: string): Promise<ProjectData | null> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.STORE_NAME], 'readonly');
      const store = transaction.objectStore(this.STORE_NAME);
      const request = store.get(id);

      request.onsuccess = () => resolve(request.result || null);
      request.onerror = () => reject(request.error);
    });
  }

  async delete(id: string): Promise<void> {
    if (!this.isInitialized) throw new Error('Storage adapter not initialized');

    // Delete from IndexedDB
    await this.deleteFromIndexedDB(id);

    // Queue for sync
    await this.syncManager.queueProjectDelete(id);

    console.log(`Deleted project ${id}`);
  }

  private async deleteFromIndexedDB(id: string): Promise<void> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.STORE_NAME, this.METADATA_STORE_NAME], 'readwrite');

      const projectStore = transaction.objectStore(this.STORE_NAME);
      projectStore.delete(id);

      const metadataStore = transaction.objectStore(this.METADATA_STORE_NAME);
      metadataStore.delete(id);

      transaction.oncomplete = () => resolve();
      transaction.onerror = () => reject(transaction.error);
    });
  }

  async list(): Promise<ProjectData[]> {
    if (!this.isInitialized) throw new Error('Storage adapter not initialized');

    // Get local projects
    const localProjects = await this.listFromIndexedDB();

    // Fetch server metadata in parallel (non-blocking)
    this.syncServerMetadata().catch((error) => {
      console.error('Failed to sync server metadata:', error);
    });

    return localProjects;
  }

  private async listFromIndexedDB(): Promise<ProjectData[]> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.STORE_NAME], 'readonly');
      const store = transaction.objectStore(this.STORE_NAME);
      const request = store.getAll();

      request.onsuccess = () => {
        const projects = request.result as ProjectData[];
        projects.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime());
        resolve(projects);
      };
      request.onerror = () => reject(request.error);
    });
  }

  private async syncServerMetadata(): Promise<void> {
    try {
      const serverMetadata = await this.syncManager.fetchServerProjectsMetadata();

      // Compare with local metadata and identify projects that need sync
      for (const serverMeta of serverMetadata) {
        const localMeta = await this.getLocalMetadata(serverMeta.id);

        if (!localMeta) {
          // Project exists on server but not locally - mark for potential download
          console.log(`Server project ${serverMeta.id} not found locally`);
        } else if (localMeta.hash !== serverMeta.hash) {
          // Project has different hash - needs sync
          console.log(`Project ${serverMeta.id} has different hash, needs sync`);
        }
      }
    } catch (error) {
      console.error('Failed to sync server metadata:', error);
    }
  }

  private async getLocalMetadata(id: string): Promise<ProjectMetadata | null> {
    if (!this.db) return null;

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.METADATA_STORE_NAME], 'readonly');
      const store = transaction.objectStore(this.METADATA_STORE_NAME);
      const request = store.get(id);

      request.onsuccess = () => resolve(request.result || null);
      request.onerror = () => reject(request.error);
    });
  }

  async searchProjects(searchTerm: string, tags: string[], filterMode: 'all' | 'any' = 'any'): Promise<ProjectData[]> {
    const allProjects = await this.list();

    let filtered = allProjects;

    // Filter by search term
    if (searchTerm) {
      filtered = filtered.filter((project) => project.name.toLowerCase().includes(searchTerm.toLowerCase()));
    }

    // Filter by tags
    if (tags.length > 0) {
      filtered = filtered.filter((project) => {
        if (!project.tags || project.tags.length === 0) return false;

        if (filterMode === 'all') {
          return tags.every((tag) => project.tags.includes(tag));
        } else {
          return tags.some((tag) => project.tags.includes(tag));
        }
      });
    }

    return filtered;
  }

  // Tags management
  private async saveTags(tags: string[]): Promise<void> {
    if (!this.db) throw new Error('IndexedDB not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.TAGS_STORE_NAME], 'readwrite');
      const store = transaction.objectStore(this.TAGS_STORE_NAME);

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
      const transaction = this.db!.transaction([this.TAGS_STORE_NAME], 'readonly');
      const store = transaction.objectStore(this.TAGS_STORE_NAME);
      const request = store.getAll();

      request.onsuccess = () => {
        const tags = request.result.sort((a, b) => b.usageCount - a.usageCount);
        resolve(tags);
      };
      request.onerror = () => reject(request.error);
    });
  }

  // Storage info
  async getStorageInfo(): Promise<{ totalSize: number; projectCount: number }> {
    const projects = await this.listFromIndexedDB();
    let totalSize = 0;

    projects.forEach((project) => {
      totalSize += project.size || this.estimateSize(project.data);
    });

    return { totalSize, projectCount: projects.length };
  }

  async getProjectInfo(id: string): Promise<{ size: number; createdAt: string; updatedAt: string } | null> {
    const project = await this.loadFromIndexedDB(id);

    if (project) {
      return {
        size: project.size || this.estimateSize(project.data),
        createdAt: project.createdAt,
        updatedAt: project.updatedAt,
      };
    }

    return null;
  }

  // Sync management
  async getSyncStats() {
    return await this.syncManager.getSyncStats();
  }

  async retryFailedSync(): Promise<void> {
    await this.syncManager.retryFailedItems();
  }

  onSyncStart(callback: () => void): void {
    this.syncManager.onSyncStart(callback);
  }

  onSyncComplete(callback: (stats: any) => void): void {
    this.syncManager.onSyncComplete(callback);
  }

  onSyncError(callback: (error: Error) => void): void {
    this.syncManager.onSyncError(callback);
  }

  // Utility methods
  private estimateSize(data: string): number {
    return new Blob([data]).size / 1024;
  }

  private formatSize(sizeInKB: number): string {
    if (sizeInKB < 1024) {
      return `${sizeInKB.toFixed(1)}KB`;
    } else {
      return `${(sizeInKB / 1024).toFixed(1)}MB`;
    }
  }

  destroy(): void {
    this.syncManager.destroy();
  }
}
