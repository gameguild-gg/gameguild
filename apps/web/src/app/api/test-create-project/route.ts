import { NextRequest, NextResponse } from 'next/server';
import { createProject } from '@/components/projects/actions';
import { auth } from '@/auth';

export async function POST(request: NextRequest) {
  try {
    // Check if user is authenticated
    const session = await auth();
    if (!session || !session.accessToken) {
      return NextResponse.json({ error: 'Authentication required' }, { status: 401 });
    }

    // Get request body
    const body = await request.json();

    // Create the project using our action
    const project = await createProject(body);

    return NextResponse.json(project);
  } catch (error) {
    console.error('API route error:', error);
    return NextResponse.json(
      {
        error: 'Failed to create project',
        details: error instanceof Error ? error.message : 'Unknown error',
      },
      { status: 500 },
    );
  }
}
