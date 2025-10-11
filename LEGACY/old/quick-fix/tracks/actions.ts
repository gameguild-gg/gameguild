'use server';

import path from 'path';
import { promises as fs } from 'fs';

export async function getTracks() {
  try {
    const jsonDirectory = path.join(process.cwd(), 'data');
    const fileContents = await fs.readFile(jsonDirectory + '/tracks.json', 'utf8');
    const tracks = JSON.parse(fileContents);
    return tracks;
  } catch (error) {
    console.error('Error reading tracks data:', error);
    throw new Error('Failed to fetch tracks');
  }
}
