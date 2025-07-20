// import fs from 'fs/promises';
// import { createReadStream, createWriteStream } from 'fs';
// import { join } from 'path';
// import { Readable, Transform } from 'stream';
// import { pipeline } from 'stream/promises';
// import { StorageMetadata, StreamOptions } from './storage.types';
// import { BaseStorageAdapter } from './base-storage.adapter';
//
// // JSON Storage Adapter for structured data
// export class JsonStorageAdapter<T = any> extends BaseStorageAdapter<T> {
//   private basePath: string;
//   private prettyPrint: boolean;
//
//   constructor(basePath: string = './data/json', prettyPrint: boolean = true) {
//     super({ path: basePath });
//     this.basePath = basePath;
//     this.prettyPrint = prettyPrint;
//
//     void this.initializeStorage();
//   }
//
//   // Streaming support for large JSON files
//   createReadStream(id: string, options?: StreamOptions): Readable {
//     const filePath = this.getFilePath(id);
//
//     // Create a transform stream that parses JSON chunks
//     const jsonStream = new Transform({
//       transform(chunk, encoding, callback) {
//         callback(null, chunk);
//       }
//     });
//
//     const fileStream = createReadStream(filePath, {
//       encoding: 'utf-8',
//       highWaterMark: options?.highWaterMark || 16 * 1024,
//     });
//
//     return fileStream.pipe(jsonStream);
//   }
//
//   // Schema validation support
//   async validateSchema(id: string, schema: any): Promise<boolean> {
//     const data = await this.read(id);
//     if (!data) return false;
//
//     // Simple schema validation - could use a library like Joi or Zod
//     try {
//       // Basic type checking
//       for (const [ key, expectedType ] of Object.entries(schema)) {
//         const actualType = typeof (data as any)[key];
//         if (actualType !== expectedType) {
//           return false;
//         }
//       }
//       return true;
//     } catch {
//       return false;
//     }
//   }
//
//   protected async persistWrite(id: string, data: T, metadata: StorageMetadata): Promise<void> {
//     const filePath = this.getFilePath(id);
//
//     // Use streaming for large objects
//     const jsonString = this.prettyPrint
//       ? JSON.stringify(data, null, 2)
//       : JSON.stringify(data);
//
//     if (jsonString.length > 1024 * 1024) { // 1MB threshold
//       await this.writeStream(filePath, jsonString);
//     } else {
//       await fs.writeFile(filePath, jsonString, 'utf-8');
//     }
//
//     await this.saveIndex();
//   }
//
//   protected async persistDelete(id: string): Promise<void> {
//     const filePath = this.getFilePath(id);
//
//     try {
//       await fs.unlink(filePath);
//     } catch (error) {
//       console.warn(`Failed to delete file: ${ filePath }`, error);
//     }
//
//     await this.saveIndex();
//   }
//
//   protected async loadIndex(): Promise<void> {
//     const indexPath = join(this.basePath, '_index.json');
//
//     try {
//       const indexData = await fs.readFile(indexPath, 'utf-8');
//       const parsed = JSON.parse(indexData);
//
//       // Rebuild indices
//       this.index = this.createIndex();
//
//       for (const metadata of parsed.items || []) {
//         // Load actual data
//         try {
//           const data = await this.loadFile(metadata.id);
//           if (data) {
//             this.index.byId.set(metadata.id, data);
//             this.index.metadata.set(metadata.id, metadata);
//
//             if (metadata.type && this.index.byType) {
//               if (!this.index.byType.has(metadata.type)) {
//                 this.index.byType.set(metadata.type, new Set());
//               }
//               this.index.byType.get(metadata.type)!.add(metadata.id);
//             }
//           }
//         } catch (error) {
//           console.warn(`Failed to load JSON file ${ metadata.id }:`, error);
//         }
//       }
//     } catch {
//       // No index yet
//       console.log('No JSON index found, starting fresh');
//     }
//   }
//
//   private async initializeStorage(): Promise<void> {
//     try {
//       await fs.access(this.basePath);
//     } catch {
//       await fs.mkdir(this.basePath, { recursive: true });
//     }
//
//     await this.loadIndex();
//   }
//
//   private async saveIndex(): Promise<void> {
//     const indexPath = join(this.basePath, '_index.json');
//
//     const items = Array.from(this.index.metadata.values());
//
//     const indexData = {
//       version: '1.0',
//       updatedAt: new Date().toISOString(),
//       count: items.length,
//       items,
//     };
//
//     await fs.writeFile(indexPath, JSON.stringify(indexData, null, 2));
//   }
//
//   private getFilePath(id: string): string {
//     return join(this.basePath, `${ id }.json`);
//   }
//
//   private async loadFile(id: string): Promise<T | null> {
//     try {
//       const filePath = this.getFilePath(id);
//       const content = await fs.readFile(filePath, 'utf-8');
//       return JSON.parse(content);
//     } catch {
//       return null;
//     }
//   }
//
//   private async writeStream(filePath: string, data: string): Promise<void> {
//     const readable = Readable.from([ data ]);
//     const writable = createWriteStream(filePath);
//     await pipeline(readable, writable);
//   }
// }
//
// // CSV Storage Adapter for tabular data
// export class CsvStorageAdapter<T extends Record<string, any> = any> extends BaseStorageAdapter<T[]> {
//   private basePath: string;
//   private delimiter: string;
//   private headers: string[];
//
//   constructor(
//     basePath: string = './data/csv',
//     options: {
//       delimiter?: string;
//       headers?: string[];
//     } = {}
//   ) {
//     super({ path: basePath });
//     this.basePath = basePath;
//     this.delimiter = options.delimiter || ',';
//     this.headers = options.headers || [];
//     this.initializeStorage();
//   }
//
//   // O(1) append operation for CSV
//   async appendRow(fileId: string, row: T): Promise<void> {
//     const filePath = this.getFilePath(fileId);
//
//     // Convert row to CSV format
//     const csvRow = Papa.unparse([ row ], {
//       header: false,
//       delimiter: this.delimiter,
//     });
//
//     try {
//       // Append to existing file
//       await fs.appendFile(filePath, '\n' + csvRow);
//
//       // Update in-memory data
//       let data = this.index.byId.get(fileId) || [];
//       data.push(row);
//       this.index.byId.set(fileId, data);
//     } catch (error) {
//       console.error('Failed to append CSV row:', error);
//       throw error;
//     }
//   }
//
//   // O(n) but optimized batch append
//   async appendRows(fileId: string, rows: T[]): Promise<void> {
//     const filePath = this.getFilePath(fileId);
//
//     // Convert rows to CSV format
//     const csvData = Papa.unparse(rows, {
//       header: false,
//       delimiter: this.delimiter,
//     });
//
//     try {
//       // Append to existing file
//       await fs.appendFile(filePath, '\n' + csvData);
//
//       // Update in-memory data
//       let data = this.index.byId.get(fileId) || [];
//       data.push(...rows);
//       this.index.byId.set(fileId, data);
//     } catch (error) {
//       console.error('Failed to append CSV rows:', error);
//       throw error;
//     }
//   }
//
//   // Create streaming CSV reader
//   createReadStream(id: string, options?: StreamOptions & { parse?: boolean }): Readable {
//     const filePath = this.getFilePath(id);
//
//     const fileStream = createReadStream(filePath, {
//       encoding: 'utf-8',
//       highWaterMark: options?.highWaterMark || 64 * 1024,
//     });
//
//     if (options?.parse) {
//       // Return parsed stream
//       const parseStream = Papa.parse(Papa.NODE_STREAM_INPUT, {
//         header: true,
//         delimiter: this.delimiter,
//         dynamicTyping: true,
//       });
//
//       return fileStream.pipe(parseStream);
//     }
//
//     return fileStream;
//   }
//
//   // Query operations optimized for tabular data
//   async query(id: string, filter: (row: T) => boolean): Promise<T[]> {
//     const data = await this.read(id);
//     if (!data) return [];
//
//     return data.filter(filter);
//   }
//
//   // Aggregate operations
//   async aggregate(
//     id: string,
//     groupBy: keyof T,
//     aggregations: Record<string, (group: T[]) => any>
//   ): Promise<Record<string, any>> {
//     const data = await this.read(id);
//     if (!data) return {};
//
//     // Group data - O(n)
//     const groups = new Map<any, T[]>();
//     for (const row of data) {
//       const key = row[groupBy];
//       if (!groups.has(key)) {
//         groups.set(key, []);
//       }
//       groups.get(key)!.push(row);
//     }
//
//     // Apply aggregations - O(g) where g is number of groups
//     const results: Record<string, any> = {};
//
//     for (const [ key, group ] of groups) {
//       results[key] = {};
//       for (const [ aggName, aggFunc ] of Object.entries(aggregations)) {
//         results[key][aggName] = aggFunc(group);
//       }
//     }
//
//     return results;
//   }
//
//   protected async persistWrite(id: string, data: T[], metadata: StorageMetadata): Promise<void> {
//     const filePath = this.getFilePath(id);
//
//     // Use Papa Parse for CSV generation
//     const csv = Papa.unparse(data, {
//       header: true,
//       delimiter: this.delimiter,
//     });
//
//     // Use streaming for large CSV files
//     if (csv.length > 1024 * 1024 || data.length > 10000) {
//       await this.writeStreamCsv(filePath, data);
//     } else {
//       await fs.writeFile(filePath, csv, 'utf-8');
//     }
//
//     await this.saveIndex();
//   }
//
//   protected async persistDelete(id: string): Promise<void> {
//     const filePath = this.getFilePath(id);
//
//     try {
//       await fs.unlink(filePath);
//     } catch (error) {
//       console.warn(`Failed to delete CSV file: ${ filePath }`, error);
//     }
//
//     await this.saveIndex();
//   }
//
//   protected async loadIndex(): Promise<void> {
//     const indexPath = join(this.basePath, '_index.json');
//
//     try {
//       const indexData = await fs.readFile(indexPath, 'utf-8');
//       const parsed = JSON.parse(indexData);
//
//       // Rebuild indices
//       this.index = this.createIndex();
//
//       // Load CSV files
//       for (const metadata of parsed.items || []) {
//         try {
//           const data = await this.loadCsvFile(metadata.id);
//           if (data) {
//             this.index.byId.set(metadata.id, data);
//             this.index.metadata.set(metadata.id, metadata);
//           }
//         } catch (error) {
//           console.warn(`Failed to load CSV file ${ metadata.id }:`, error);
//         }
//       }
//     } catch {
//       console.log('No CSV index found, starting fresh');
//     }
//   }
//
//   private async initializeStorage(): Promise<void> {
//     try {
//       await fs.access(this.basePath);
//     } catch {
//       await fs.mkdir(this.basePath, { recursive: true });
//     }
//
//     await this.loadIndex();
//   }
//
//   private async writeStreamCsv(filePath: string, data: T[]): Promise<void> {
//     const writeStream = createWriteStream(filePath);
//
//     return new Promise((resolve, reject) => {
//       // Write header
//       if (data.length > 0) {
//         const headers = Object.keys(data[0]).join(this.delimiter) + '\n';
//         writeStream.write(headers);
//       }
//
//       // Write data in chunks
//       const CHUNK_SIZE = 1000;
//       let index = 0;
//
//       const writeChunk = () => {
//         let ok = true;
//
//         while (index < data.length && ok) {
//           const chunk = data.slice(index, index + CHUNK_SIZE);
//           const csv = Papa.unparse(chunk, {
//             header: false,
//             delimiter: this.delimiter,
//           });
//
//           if (index + CHUNK_SIZE >= data.length) {
//             // Last chunk
//             writeStream.end(csv);
//           } else {
//             ok = writeStream.write(csv + '\n');
//           }
//
//           index += CHUNK_SIZE;
//         }
//
//         if (index < data.length) {
//           // Wait for drain event
//           writeStream.once('drain', writeChunk);
//         }
//       };
//
//       writeStream.on('finish', resolve);
//       writeStream.on('error', reject);
//
//       writeChunk();
//     });
//   }
//
//   private async loadCsvFile(id: string): Promise<T[] | null> {
//     try {
//       const filePath = this.getFilePath(id);
//       const content = await fs.readFile(filePath, 'utf-8');
//
//       const parsed = Papa.parse<T>(content, {
//         header: true,
//         delimiter: this.delimiter,
//         dynamicTyping: true,
//         skipEmptyLines: true,
//       });
//
//       if (parsed.errors.length > 0) {
//         console.warn(`CSV parsing errors for ${ id }:`, parsed.errors);
//       }
//
//       return parsed.data;
//     } catch {
//       return null;
//     }
//   }
//
//   private async saveIndex(): Promise<void> {
//     const indexPath = join(this.basePath, '_index.json');
//
//     const items = Array.from(this.index.metadata.values());
//
//     const indexData = {
//       version: '1.0',
//       updatedAt: new Date().toISOString(),
//       count: items.length,
//       items,
//     };
//
//     await fs.writeFile(indexPath, JSON.stringify(indexData, null, 2));
//   }
//
//   private getFilePath(id: string): string {
//     return join(this.basePath, `${ id }.csv`);
//   }
// }