import fs from 'fs/promises';
import { createReadStream, createWriteStream } from 'fs';
import { join } from 'path';
import { pipeline, Readable, Writable } from 'stream';
import { promisify } from 'util';
import matter from 'gray-matter';
import slugify from 'slugify';
import { BaseStorageAdapter } from './base-storage.adapter';
import { ContentStorage, StorageMetadata, StreamOptions } from './storage.types';

const pipelineAsync = promisify(pipeline);

export class MarkdownStorageAdapter extends BaseStorageAdapter<ContentStorage> {
  private readonly basePath: string;

  constructor(basePath: string = './data/content') {
    super({ path: basePath });

    this.basePath = basePath;

    void this.initializeStorage();
  }

  // O(1) - Override to also handle slug lookups
  async read(id: string): Promise<ContentStorage | null> {
    // Try ID first
    let content = await super.read(id);

    // Try slug if not found by ID
    if (!content && this.index.bySlug) {
      content = this.index.bySlug.get(id) || null;
    }

    return content;
  }

  // O(1) - Enhanced write with slug indexing
  async write(id: string, data: ContentStorage, metadata?: Partial<StorageMetadata>): Promise<ContentStorage> {
    // Ensure slug
    if (!data.slug && data.title) {
      data.slug = slugify(data.title, { lower: true, strict: true });
    }

    // Update slug index
    if (data.slug && this.index.bySlug) {
      this.index.bySlug.set(data.slug, data);
    }

    return super.write(id, data, metadata);
  }

  // Streaming support
  createReadStream(id: string, options?: StreamOptions): Readable {
    const filePath = this.getFilePath(id);

    return createReadStream(filePath, {
      encoding: 'utf-8',
      highWaterMark: options?.highWaterMark || 16 * 1024, // 16KB chunks
      start: options?.start,
      end: options?.end,
    });
  }

  createWriteStream(id: string, metadata?: Partial<StorageMetadata>): Writable {
    const filePath = this.getFilePath(id);

    // Create a transform stream that adds front matter
    const writeStream = createWriteStream(filePath, {
      encoding: 'utf-8',
    });

    // Add to index when stream closes
    writeStream.on('close', async () => {
      const content = await this.readFile(id);
      if (content) {
        await this.write(id, content, metadata);
      }
    });

    return writeStream;
  }

  // Additional markdown-specific methods
  async findBySlug(slug: string): Promise<ContentStorage | null> {
    return this.index.bySlug?.get(slug) || null;
  }

  async updateConnections(id: string, connections: string[]): Promise<void> {
    const content = await this.read(id);
    if (content) {
      content.connections = connections;
      await this.write(id, content);
    }
  }

  async updatePosition(id: string, position: { x: number; y: number }): Promise<void> {
    const content = await this.read(id);
    if (content) {
      content.position = position;
      await this.write(id, content);
    }
  }

  // Implement persistence methods
  protected async persistWrite(id: string, data: ContentStorage): Promise<void> {
    const filePath = this.getFilePath(id);

    // Separate content from metadata
    const { content, ...frontMatter } = data;

    // Create a Markdown file with front matter
    const fileContent = matter.stringify(content || '', frontMatter);

    // Use streaming for large files
    if (fileContent.length > 1024 * 1024) {
      // 1MB threshold
      await this.writeStream(filePath, fileContent);
    } else {
      await fs.writeFile(filePath, fileContent, 'utf-8');
    }

    // Update index file
    await this.saveIndex();
  }

  protected async persistDelete(id: string): Promise<void> {
    const filePath = this.getFilePath(id);

    try {
      await fs.unlink(filePath);
    } catch (error) {
      // File might not exist
      console.warn(`Failed to delete file: ${filePath}`, error);
    }

    // Update index file
    await this.saveIndex();
  }

  protected async loadIndex(): Promise<void> {
    const indexPath = join(this.basePath, 'index.json');

    try {
      const indexData = await fs.readFile(indexPath, 'utf-8');
      const parsed = JSON.parse(indexData);

      // Rebuild indices from saved data - O(n) on startup only
      this.index = this.createIndex();

      if (parsed.items) {
        for (const item of parsed.items) {
          // All O(1) operations
          this.index.byId.set(item.id, item);

          if (item.slug && this.index.bySlug) {
            this.index.bySlug.set(item.slug, item);
          }

          if (item.type && this.index.byType) {
            if (!this.index.byType.has(item.type)) {
              this.index.byType.set(item.type, new Set());
            }
            this.index.byType.get(item.type)!.add(item.id);
          }

          const metadata: StorageMetadata = {
            id: item.id,
            type: item.type,
            createdAt: item.createdAt,
            updatedAt: item.updatedAt,
          };
          this.index.metadata.set(item.id, metadata);
        }
      }
    } catch {
      // Index doesn't exist yet
      console.log('No existing index found, starting fresh');
    }

    // Also scan the directory for orphaned files
    await this.scanDirectory();
  }

  private async initializeStorage(): Promise<void> {
    try {
      await fs.access(this.basePath);
    } catch {
      await fs.mkdir(this.basePath, { recursive: true });
    }

    // Load the existing index
    await this.loadIndex();
  }

  private async scanDirectory(): Promise<void> {
    try {
      const files = await fs.readdir(this.basePath);
      const mdFiles = files.filter((f) => f.endsWith('.md'));

      // Process files in parallel but in chunks
      const CHUNK_SIZE = 10;

      for (let i = 0; i < mdFiles.length; i += CHUNK_SIZE) {
        const chunk = mdFiles.slice(i, i + CHUNK_SIZE);

        await Promise.all(
          chunk.map(async (file) => {
            const id = file.replace('.md', '');

            // Skip if already in index
            if (this.index.byId.has(id)) return;

            try {
              const filePath = join(this.basePath, file);
              const content = await fs.readFile(filePath, 'utf-8');
              const { data, content: body } = matter(content);

              const contentData: ContentStorage = {
                id,
                slug: data.slug || id,
                title: data.title || 'Untitled',
                type: data.type || 'note',
                content: body,
                createdAt: data.createdAt || new Date().toISOString(),
                updatedAt: data.updatedAt || new Date().toISOString(),
                ...data,
              };

              // Add to indices
              this.index.byId.set(id, contentData);
              if (contentData.slug && this.index.bySlug) {
                this.index.bySlug.set(contentData.slug, contentData);
              }
            } catch (error) {
              console.warn(`Failed to load file ${file}:`, error);
            }
          }),
        );
      }
    } catch (error) {
      console.warn('Failed to scan directory:', error);
    }
  }

  private async saveIndex(): Promise<void> {
    const indexPath = join(this.basePath, 'index.json');

    // Convert index to serializable format
    const items = Array.from(this.index.byId.values());

    const indexData = {
      version: '2.0',
      updatedAt: new Date().toISOString(),
      count: items.length,
      items: items.map((item) => ({
        id: item.id,
        slug: item.slug,
        title: item.title,
        type: item.type,
        createdAt: item.createdAt,
        updatedAt: item.updatedAt,
        connections: item.connections,
        position: item.position,
      })),
    };

    // Use streaming for large indices
    const data = JSON.stringify(indexData, null, 2);

    if (data.length > 1024 * 1024) {
      // 1MB
      await this.writeStream(indexPath, data);
    } else {
      await fs.writeFile(indexPath, data, 'utf-8');
    }
  }

  // Helper methods
  private getFilePath(id: string): string {
    return join(this.basePath, `${id}.md`);
  }

  private async readFile(id: string): Promise<ContentStorage | null> {
    try {
      const filePath = this.getFilePath(id);
      const content = await fs.readFile(filePath, 'utf-8');
      const { data, content: body } = matter(content);

      return {
        id,
        slug: data.slug || id,
        title: data.title || 'Untitled',
        type: data.type || 'note',
        content: body,
        createdAt: data.createdAt || new Date().toISOString(),
        updatedAt: data.updatedAt || new Date().toISOString(),
        ...data,
      };
    } catch {
      return null;
    }
  }

  private async writeStream(filePath: string, data: string): Promise<void> {
    return new Promise((resolve, reject) => {
      const readable = Readable.from([data]);
      const writable = createWriteStream(filePath);

      pipelineAsync(readable, writable)
        .then(() => resolve())
        .catch(reject);
    });
  }
}
