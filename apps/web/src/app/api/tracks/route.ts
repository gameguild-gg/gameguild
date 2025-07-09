import { NextResponse } from 'next/server';
import path from 'path';
import { promises as fs } from 'fs';

export async function GET() {
  try {
    const jsonDirectory = path.join(process.cwd(), 'data');
    const fileContents = await fs.readFile(jsonDirectory + '/tracks.json', 'utf8');
    const tracks = JSON.parse(fileContents);
    return NextResponse.json(tracks);
  } catch (error) {
    console.error('Error reading tracks data:', error);
    return NextResponse.json({ error: 'Failed to fetch tracks' }, { status: 500 });
  }
}
