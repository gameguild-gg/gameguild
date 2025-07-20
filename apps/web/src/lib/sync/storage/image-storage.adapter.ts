import { createReadStream, createWriteStream } from 'fs';
import fs from 'fs/promises';
import { basename, join } from 'path';
import { pipeline, Readable, Transform, Writable } from 'stream';
import { promisify } from 'util';
import crypto from 'crypto';
import sharp from 'sharp'; // For image processing
import { BaseStorageAdapter } from './base-storage.adapter';
import { ImageStorage, StorageMetadata, StorageResult } from './storage.types';

const pipelineAsync = promisify(pipeline);

interface ImageBucketConfig {
  basePath: string;
  publicUrl?: string;
  thumbnails?: {
    small: { width: 150; height: 150 };
    medium: { width: 300; height: 300 };
    large: { width: 600; height: 600 };
  };
  formats?: string[];
  maxSize?: number; // Max file size in bytes
}

export class ImageStorageAdapter extends BaseStorageAdapter<ImageStorage> {
  private config: Required<ImageBucketConfig>;

  constructor(config: Partial<ImageBucketConfig> = {}) {
    super({ bucket: 'images' });

    this.config = {
      basePath: config.basePath || './data/images',
      publicUrl: config.publicUrl || '/images',
      thumbnails: config.thumbnails || {
        small: { width: 150, height: 150 },
        medium: { width: 300, height: 300 },
        large: { width: 600, height: 600 },
      },
      formats: config.formats || ['jpg', 'jpeg', 'png', 'webp', 'gif', 'svg'],
      maxSize: config.maxSize || 10 * 1024 * 1024, // 10MB default
    };

    void this.initializeStorage();
  }

  // O(1) - Override to handle image-specific data
  public async write(id: string, data: ImageStorage | Buffer | Readable, metadata?: Partial<StorageMetadata>): Promise<StorageResult<ImageStorage>> {
    // Handle different input types
    if (Buffer.isBuffer(data) || data instanceof Readable) {
      // Process raw image data
      return this.writeImage(id, data, metadata) as Promise<StorageResult<ImageStorage>>;
    }

    // Handle ImageStorage object
    return super.write(id, data as ImageStorage, metadata);
  }

  // Streaming support with transforms
  public createReadStream(
    id: string,
    options?: {
      size?: 'small' | 'medium' | 'large';
      highWaterMark?: number;
      start?: number;
      end?: number;
    },
  ): Readable {
    const image = this.index.byId.get(id) as ImageStorage;
    if (!image) {
      throw new Error(`Image ${id} not found`);
    }

    // Determine which version to stream
    let filePath: string;

    if (options?.size && image.thumbnails?.[options.size]) {
      // Stream thumbnail
      const thumbnailUrl = image.thumbnails[options.size];
      const filename = basename(thumbnailUrl!);
      filePath = join(this.config.basePath, 'thumbnails', filename);
    } else {
      // Stream original
      const filename = basename(image.url);
      filePath = join(this.config.basePath, 'original', filename);
    }

    return createReadStream(filePath, {
      highWaterMark: options?.highWaterMark || 64 * 1024, // 64KB chunks
      start: options?.start,
      end: options?.end,
    });
  }

  public createWriteStream(id: string, metadata?: Partial<StorageMetadata>): Writable {
    const ext = metadata?.mimeType ? this.mimeToExt(metadata.mimeType) : 'jpg';
    const filename = `${id}.${ext}`;
    const filePath = join(this.config.basePath, 'original', filename);

    // Create a pass-through stream that processes the image
    const processStream = new Transform({
      transform(chunk, encoding, callback) {
        this.push(chunk);
        callback();
      },
    });

    const writeStream = createWriteStream(filePath);

    // Set up a pipeline
    pipeline(processStream, writeStream, async (err: unknown) => {
      if (err) {
        console.error('Stream pipeline error:', err);
        return;
      }

      // Process the saved image
      const buffer = await fs.readFile(filePath);
      await this.writeImage(id, buffer, metadata);
    });

    return processStream;
  }

  // Image-specific methods
  public async getImageStats(): Promise<{
    totalImages: number;
    totalSize: number;
    byFormat: Record<string, number>;
    averageSize: number;
  }> {
    const images = Array.from(this.index.byId.values()) as ImageStorage[];

    const stats = {
      totalImages: images.length,
      totalSize: 0,
      byFormat: {} as Record<string, number>,
      averageSize: 0,
    };

    for (const image of images) {
      stats.totalSize += image.size || 0;

      const format = image.format || 'unknown';
      stats.byFormat[format] = (stats.byFormat[format] || 0) + 1;
    }

    stats.averageSize = stats.totalImages > 0 ? stats.totalSize / stats.totalImages : 0;

    return stats;
  }

  // O(log n) - Find similar images by hash
  public async findSimilar(checksum: string): Promise<ImageStorage[]> {
    const images = Array.from(this.index.byId.values()) as ImageStorage[];
    return images.filter((img) => img.checksum === checksum);
  }

  // Cleanup orphaned thumbnails - O(n) but runs async
  public async cleanupOrphans(): Promise<number> {
    let cleaned = 0;

    try {
      const thumbnailDir = join(this.config.basePath, 'thumbnails');
      const files = await fs.readdir(thumbnailDir);

      // Check each file
      for (const file of files) {
        const id = file.split('_')[0];

        if (!this.index.byId.has(id)) {
          // Orphaned thumbnail
          await fs.unlink(join(thumbnailDir, file));
          cleaned++;
        }
      }
    } catch (error) {
      console.error('Cleanup error:', error);
    }

    return cleaned;
  }

  // Implement required abstract methods
  protected async persistWrite(id: string, data: ImageStorage, metadata: StorageMetadata): Promise<void> {
    // Save metadata
    const metadataPath = join(this.config.basePath, 'metadata', `${id}.json`);
    await fs.writeFile(metadataPath, JSON.stringify(data, null, 2));

    await this.saveIndex();
  }

  protected async persistDelete(id: string): Promise<void> {
    const image = this.index.byId.get(id) as ImageStorage;
    if (!image) return;

    // Delete all files
    const filesToDelete = [join(this.config.basePath, 'original', basename(image.url)), join(this.config.basePath, 'metadata', `${id}.json`)];

    // Add thumbnails
    if (image.thumbnails) {
      for (const url of Object.values(image.thumbnails)) {
        filesToDelete.push(join(this.config.basePath, 'thumbnails', basename(url)));
      }
    }

    // Delete in parallel
    await Promise.all(
      filesToDelete.map(
        (file) => fs.unlink(file).catch(() => {}), // Ignore errors
      ),
    );

    await this.saveIndex();
  }

  protected async loadIndex(): Promise<void> {
    const indexPath = join(this.config.basePath, 'index.json');

    try {
      const data = await fs.readFile(indexPath, 'utf-8');
      const parsed = JSON.parse(data);

      // Rebuild index
      this.index = this.createIndex();

      if (parsed.images) {
        for (const image of parsed.images) {
          this.index.byId.set(image.id, image);
          this.index.metadata.set(image.id, {
            id: image.id,
            type: 'image',
            createdAt: image.createdAt,
            updatedAt: image.updatedAt,
            size: image.size,
            mimeType: image.mimeType,
            checksum: image.checksum,
          });
        }
      }
    } catch {
      // No index yet
    }
  }

  private async initializeStorage(): Promise<void> {
    // Create bucket directories
    const dirs = [
      this.config.basePath,
      join(this.config.basePath, 'original'),
      join(this.config.basePath, 'thumbnails'),
      join(this.config.basePath, 'metadata'),
    ];

    for (const dir of dirs) {
      try {
        await fs.access(dir);
      } catch {
        await fs.mkdir(dir, { recursive: true });
      }
    }

    await this.loadIndex();
  }

  // Specialized method for writing images with processing
  private async writeImage(id: string, input: Buffer | Readable, metadata?: Partial<StorageMetadata>): Promise<StorageResult<ImageStorage>> {
    try {
      // Generate file paths
      const ext = metadata?.mimeType ? this.mimeToExt(metadata.mimeType) : 'jpg';
      const filename = `${id}.${ext}`;
      const originalPath = join(this.config.basePath, 'original', filename);

      // Process and save image
      let imageBuffer: Buffer;

      if (Buffer.isBuffer(input)) {
        imageBuffer = input;
      } else {
        // Stream to buffer
        imageBuffer = await this.streamToBuffer(input);
      }

      // Validate image
      const imageMetadata = await this.validateAndGetMetadata(imageBuffer);

      // Save original
      await fs.writeFile(originalPath, imageBuffer);

      // Generate thumbnails in parallel
      const thumbnailUrls = await this.generateThumbnails(id, imageBuffer, ext);

      // Calculate checksum
      const checksum = crypto.createHash('sha256').update(imageBuffer).digest('hex');

      // Create the storage object
      const imageStorage: ImageStorage = {
        id,
        url: `${this.config.publicUrl}/original/${filename}`,
        width: imageMetadata.width,
        height: imageMetadata.height,
        format: imageMetadata.format,
        size: imageBuffer.length,
        mimeType: `image/${imageMetadata.format}`,
        thumbnails: thumbnailUrls,
        checksum,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        type: 'image',
        ...metadata,
      };

      // Save to index
      return super.write(id, imageStorage);
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error : new Error('Image write failed'),
      };
    }
  }

  // O(1) thumbnail generation with streaming
  private async generateThumbnails(id: string, buffer: Buffer, ext: string): Promise<Record<string, string>> {
    const thumbnails: Record<string, string> = {};

    // Process thumbnails in parallel
    const promises = Object.entries(this.config.thumbnails).map(async ([size, dimensions]) => {
      const filename = `${id}_${size}.${ext}`;
      const path = join(this.config.basePath, 'thumbnails', filename);

      try {
        // Use sharp for efficient image processing
        await sharp(buffer)
          .resize(dimensions.width, dimensions.height, {
            fit: 'cover',
            position: 'center',
          })
          .toFile(path);

        thumbnails[size] = `${this.config.publicUrl}/thumbnails/${filename}`;
      } catch (error) {
        console.warn(`Failed to generate ${size} thumbnail for ${id}:`, error);
      }
    });

    await Promise.all(promises);
    return thumbnails;
  }

  // Helper methods
  private async validateAndGetMetadata(buffer: Buffer): Promise<{
    width: number;
    height: number;
    format: string;
  }> {
    // Check size
    if (buffer.length > this.config.maxSize) throw new Error(`Image size exceeds maximum of ${this.config.maxSize} bytes`);

    // Get metadata using sharp
    const metadata = await sharp(buffer).metadata();

    if (!metadata.width || !metadata.height) throw new Error('Invalid image: could not determine dimensions');

    const format = metadata.format || 'unknown';

    // Check format
    if (!this.config.formats.includes(format)) throw new Error(`Unsupported image format: ${format}`);

    return {
      width: metadata.width,
      height: metadata.height,
      format,
    };
  }

  private mimeToExt(mimeType: string): string {
    const map: Record<string, string> = {
      'image/jpeg': 'jpg',
      'image/png': 'png',
      'image/webp': 'webp',
      'image/gif': 'gif',
      'image/svg+xml': 'svg',
    };

    return map[mimeType] || 'jpg';
  }

  private async streamToBuffer(stream: Readable): Promise<Buffer> {
    const chunks: Buffer[] = [];

    return new Promise((resolve, reject) => {
      stream.on('data', (chunk) => chunks.push(Buffer.from(chunk)));
      stream.on('end', () => resolve(Buffer.concat(chunks)));
      stream.on('error', reject);
    });
  }

  private async saveIndex(): Promise<void> {
    const indexPath = join(this.config.basePath, 'index.json');
    const images = Array.from(this.index.byId.values());

    const data = JSON.stringify(
      {
        version: '2.0',
        updatedAt: new Date().toISOString(),
        count: images.length,
        images,
      },
      null,
      2,
    );

    await fs.writeFile(indexPath, data);
  }
}
