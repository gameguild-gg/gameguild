/**
 * API Client Example - Real Integration Demo
 * 
 * Demonstrates actual usage of the @game-guild/client package
 */

'use client';

import { useState, useEffect } from 'react';
import { createClient } from '@game-guild/client';
import type { ApiError } from '@game-guild/client';

export default function ApiExamplePage() {
  const [client] = useState(() =>
    createClient({
      baseUrl: 'http://localhost:5295',
      headers: {
        'X-Tenant-Id': 'default',
      },
    })
  );

  const [healthStatus, setHealthStatus] = useState<'checking' | 'healthy' | 'unhealthy'>('checking');
  const [healthData, setHealthData] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    checkHealth();
  }, []);

  const checkHealth = async () => {
    setHealthStatus('checking');
    setError(null);

    try {
      // Call the health check endpoint using the generic request method
      const result = await client.request<{ status: string; timestamp: string }>({
        method: 'GET',
        path: '/health',
      });

      if (result.ok) {
        setHealthData(result.value);
        setHealthStatus('healthy');
      } else {
        const apiError = result.error as ApiError;
        setError(`API Error: ${apiError.message} (${apiError.code})`);
        setHealthStatus('unhealthy');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
      setHealthStatus('unhealthy');
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 py-12 px-4">
      <div className="max-w-3xl mx-auto">
        <div className="bg-white rounded-2xl shadow-xl p-8 mb-6">
          <div className="flex items-center justify-between mb-6">
            <h1 className="text-3xl font-bold text-gray-900">
              🚀 API Client Demo
            </h1>
            <button
              onClick={checkHealth}
              disabled={healthStatus === 'checking'}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium text-sm"
            >
              Refresh
            </button>
          </div>

          <div className="mb-8">
            <h2 className="text-lg font-semibold text-gray-700 mb-4">Health Check Status</h2>
            
            {healthStatus === 'checking' && (
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
                <div className="flex items-center">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mr-4"></div>
                  <div>
                    <p className="text-blue-900 font-semibold">Checking API...</p>
                    <p className="text-blue-700 text-sm">Connecting to http://localhost:5295</p>
                  </div>
                </div>
              </div>
            )}

            {healthStatus === 'healthy' && healthData && (
              <div className="bg-green-50 border border-green-200 rounded-lg p-6">
                <div className="flex items-start mb-4">
                  <svg className="h-8 w-8 text-green-600 mr-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <div>
                    <h3 className="text-lg font-bold text-green-900">API is Healthy ✅</h3>
                    <p className="text-green-700 text-sm">All systems operational</p>
                  </div>
                </div>
                <div className="bg-white rounded-lg p-4 border border-green-300">
                  <pre className="text-sm overflow-x-auto">{JSON.stringify(healthData, null, 2)}</pre>
                </div>
              </div>
            )}

            {healthStatus === 'unhealthy' && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-6">
                <div className="flex items-start mb-4">
                  <svg className="h-8 w-8 text-red-600 mr-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <div>
                    <h3 className="text-lg font-bold text-red-900">API Error ❌</h3>
                    <p className="text-red-700 text-sm">Failed to connect to API</p>
                  </div>
                </div>
                {error && (
                  <div className="bg-white rounded-lg p-4 border border-red-300">
                    <p className="text-sm text-red-800 font-mono">{error}</p>
                  </div>
                )}
                <div className="mt-4 text-sm text-red-700">
                  <p className="font-semibold mb-1">Troubleshooting:</p>
                  <ul className="list-disc list-inside space-y-1">
                    <li>Make sure the API server is running on port 5295</li>
                    <li>Check CORS settings allow localhost:3000</li>
                    <li>Verify the API is accessible at http://localhost:5295/health</li>
                  </ul>
                </div>
              </div>
            )}
          </div>

          <div className="border-t pt-6">
            <h2 className="text-lg font-semibold text-gray-700 mb-4">Integration Details</h2>
            <div className="grid grid-cols-2 gap-4">
              <div className="bg-gray-50 rounded-lg p-4">
                <p className="text-xs font-medium text-gray-600 mb-1">Package</p>
                <p className="text-sm font-mono text-gray-900">@game-guild/client</p>
              </div>
              <div className="bg-gray-50 rounded-lg p-4">
                <p className="text-xs font-medium text-gray-600 mb-1">Framework</p>
                <p className="text-sm font-mono text-gray-900">Next.js 16</p>
              </div>
              <div className="bg-gray-50 rounded-lg p-4">
                <p className="text-xs font-medium text-gray-600 mb-1">API URL</p>
                <p className="text-sm font-mono text-gray-900">localhost:5295</p>
              </div>
              <div className="bg-gray-50 rounded-lg p-4">
                <p className="text-xs font-medium text-gray-600 mb-1">Tenant</p>
                <p className="text-sm font-mono text-gray-900">default</p>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-lg p-6">
          <h2 className="text-xl font-bold text-gray-900 mb-4">💡 Code Example</h2>
          <pre className="bg-gray-900 text-gray-100 rounded-lg p-4 overflow-x-auto text-sm">
{`import { createClient } from '@game-guild/client';

const client = createClient({
  baseUrl: 'http://localhost:5295',
  headers: {
    'X-Tenant-Id': 'default',
  },
});

// Type-safe API calls using the generic request method
const result = await client.request({
  method: 'GET',
  path: '/health',
});

if (result.ok) {
  console.log('Health:', result.value);
} else {
  console.error('Error:', result.error);
}`}
          </pre>
        </div>
      </div>
    </div>
  );
}
