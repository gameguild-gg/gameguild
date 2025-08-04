import { after, NextResponse } from 'next/server';

export async function GET(): Promise<NextResponse> {
  after(() => {
    // This is a placeholder for any cleanup or finalization logic
    // that you might want to run after the request is processed.
    // Currently, it does nothing.
  });

  try {
    return NextResponse.json(
      {
        status: 'healthy',
        timestamp: new Date().toISOString(),
        service: 'web',
        version: process.env.npm_package_version || 'unknown',
      },
      { status: 200 },
    );
  } catch (error) {
    return NextResponse.json(
      {
        status: 'unhealthy',
        timestamp: new Date().toISOString(),
        service: 'web',
        error: error instanceof Error ? error.message : 'Unknown error',
      },
      { status: 503 },
    );
  }
}
