import { NextResponse } from 'next/server';


export const GET = async (): Promise<NextResponse> => {
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
};
