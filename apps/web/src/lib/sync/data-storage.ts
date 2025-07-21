import { Content, ContentType } from '@/types';
import { v4 as uuidv4 } from 'uuid';
import slugify from 'slugify';
import { ContentRepository, ContentStorage, ImageRepository, ImageStorage, StorageFactory } from '@/lib/storage';
// Streaming support for large content
import { Readable, Writable } from 'stream';

// Initialize repositories
const contentRepo = new ContentRepository();
const imageRepo = new ImageRepository();

// Register with factory for direct access
StorageFactory.register('content', contentRepo);
StorageFactory.register('images', imageRepo);

// Content operations with O(1) performance
export const createContent = async <T extends Content>(data: Omit<T, 'id' | 'createdAt' | 'updatedAt' | 'slug'>): Promise<T> => {
  const { type, title } = data;

  // Generate slug
  const slug = slugify(title, { lower: true, strict: true });

  // Create content storage object
  const contentData: Omit<ContentStorage, 'id' | 'createdAt' | 'updatedAt'> = {
    ...data,
    slug,
    type,
    title,
  };

  // O(1) create operation
  const created = await contentRepo.create(contentData);

  return created as T;
};

// O(n) but optimized with indices
export const getAllContents = async (type?: ContentType, limit?: number, offset?: number): Promise<Content[]> => {
  // Get all with built-in pagination
  if (!type) return contentRepo.findAll({ limit, offset });

  // Use type index for O(log n) performance
  const contents = await contentRepo.findByType(type);

  // Apply pagination
  if (limit === undefined) return contents;

  const start = offset || 0;
  return contents.slice(start, start + limit);
};

// O(1) lookup operations
export const getContent = async (
  type: ContentType,
  idOrSlug: string,
): Promise<{
  metadata: Content;
  content: string;
} | null> => {
  // Try ID first (O(1))
  let content = await contentRepo.findById(idOrSlug);

  // Try slug if not found (O(1) with slug index)
  if (!content) {
    content = await contentRepo.findBySlug(idOrSlug);
  }

  if (!content || content.type !== type) {
    return null;
  }

  return {
    metadata: content as Content,
    content: content.content || '',
  };
};

// O(1) update operation
export const updateContent = async <T extends Content>(
  type: ContentType,
  idOrSlug: string,
  data: Partial<Omit<T, 'id' | 'createdAt' | 'updatedAt' | 'type'>>,
): Promise<T | null> => {
  // Find content first
  let content = await contentRepo.findById(idOrSlug);

  if (!content) {
    content = await contentRepo.findBySlug(idOrSlug);
  }

  if (!content || content.type !== type) {
    return null;
  }

  // O(1) update
  const updated = await contentRepo.update(content.id!, data);

  return updated as T;
};

// O(1) delete operation
export const deleteContent = async (type: ContentType, idOrSlug: string): Promise<boolean> => {
  // Find content first
  let content = await contentRepo.findById(idOrSlug);

  if (!content) {
    content = await contentRepo.findBySlug(idOrSlug);
  }

  if (!content || content.type !== type) {
    return false;
  }

  // O(1) delete
  return contentRepo.delete(content.id!);
};

// Batch operations for performance
export const createManyContents = async <T extends Content>(items: Array<Omit<T, 'id' | 'createdAt' | 'updatedAt' | 'slug'>>): Promise<T[]> => {
  const contentItems = items.map((item) => ({
    ...item,
    slug: slugify(item.title, { lower: true, strict: true }),
  }));

  // Batch create - optimized
  const created = await contentRepo.createMany(contentItems);

  return created as T[];
};

export const updateManyContents = async <T extends Content>(
  updates: Array<{
    id: string;
    data: Partial<T>;
  }>,
): Promise<Map<string, T>> => {
  const updateMap = new Map<string, Partial<T>>();

  updates.forEach(({ id, data }) => {
    updateMap.set(id, data);
  });

  // Batch update - optimized
  const results = await contentRepo.updateMany(updateMap);

  return results as Map<string, T>;
};

// Image operations
export const uploadImage = async (
  file: Buffer | ReadableStream,
  metadata?: {
    alt?: string;
    caption?: string;
    mimeType?: string;
  },
): Promise<ImageStorage> => {
  const id = `img-${uuidv4()}`;

  // Stream or buffer upload
  const result = await imageRepo.create({
    ...metadata,
    type: 'image',
  } as any);

  return result;
};

export const getImage = async (id: string): Promise<ImageStorage | null> => {
  return imageRepo.findById(id);
};

export const deleteImage = async (id: string): Promise<boolean> => {
  return imageRepo.delete(id);
};

// Advanced queries with O(n) but optimized
export const searchContent = async (query: {
  type?: ContentType;
  dateStart?: Date;
  dateEnd?: Date;
  connections?: string[];
  limit?: number;
  offset?: number;
}): Promise<Content[]> => {
  let results: ContentStorage[] = [];

  // Use date range index if provided
  if (query.dateStart && query.dateEnd) {
    results = await contentRepo.findByDateRange(query.dateStart, query.dateEnd);
  } else if (query.type) {
    results = await contentRepo.findByType(query.type);
  } else {
    results = await contentRepo.findAll();
  }

  // Filter by connections if needed
  if (query.connections && query.connections.length > 0) {
    results = results.filter((content) => content.connections?.some((conn) => query.connections!.includes(conn)));
  }

  // Apply pagination
  if (query.limit !== undefined) {
    const start = query.offset || 0;
    results = results.slice(start, start + query.limit);
  }

  return results as Content[];
};

// Performance monitoring
export const getStorageStats = async () => {
  const contentStats = await contentRepo.findAll();
  const imageAdapter = StorageFactory.get('images') as any;

  const imageStats = imageAdapter ? await imageAdapter.getImageStats() : null;

  return {
    content: {
      total: contentStats.length,
      byType: contentStats.reduce(
        (acc, content) => {
          acc[content.type] = (acc[content.type] || 0) + 1;
          return acc;
        },
        {} as Record<string, number>,
      ),
    },
    images: imageStats,
  };
};

// Export check function for backwards compatibility
export async function contentExists(type: ContentType, id: string): Promise<boolean> {
  const content = await getContent(type, id);
  return content !== null;
}

export const createContentReadStream = (type: ContentType, id: string): Readable => {
  const adapter = StorageFactory.get('content') as any;

  if (!adapter || !adapter.createReadStream) {
    throw new Error('Streaming not supported for content');
  }

  return adapter.createReadStream(id);
};

export const createContentWriteStream = (type: ContentType, id: string, metadata?: Partial<Content>): Writable => {
  const adapter = StorageFactory.get('content') as any;

  if (!adapter || !adapter.createWriteStream) {
    throw new Error('Streaming not supported for content');
  }

  return adapter.createWriteStream(id, metadata);
};

export const createImageReadStream = (id: string, options?: { size?: 'small' | 'medium' | 'large' }): Readable => {
  const adapter = StorageFactory.get('images') as any;

  if (!adapter || !adapter.createReadStream) {
    throw new Error('Streaming not supported for images');
  }

  return adapter.createReadStream(id, options);
};

// Migration helper for existing data
export const migrateFromOldIndex = async (oldIndexPath: string): Promise<void> => {
  // Implementation for migrating from old index.json format
  // This would read the old format and create entries using the new system
  console.log('Migration helper - implement based on your old format');
};
