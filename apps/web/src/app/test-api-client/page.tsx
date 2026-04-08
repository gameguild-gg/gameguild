/**
 * API Client Integration Test Page
 * 
 * Tests the @game-guild/client package integration in Next.js
 */

'use client';

import { useState } from 'react';

export default function ApiClientTestPage() {
  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);

  const testBasicImport = async () => {
    setStatus('loading');
    setError(null);
    
    try {
      // Test basic import
      const { createClient } = await import('@game-guild/client');
      
      setResult({
        test: 'Basic Import',
        createClient: typeof createClient === 'function' ? 'Available ✅' : 'Not available ❌',
      });
      setStatus('success');
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setStatus('error');
    }
  };

  const testNextIntegration = async () => {
    setStatus('loading');
    setError(null);
    
    try {
      // Test Next.js integration
      const nextIntegration = await import('@game-guild/client/next');
      
      setResult({
        test: 'Next.js Integration',
        createNextClient: typeof nextIntegration.createNextClient === 'function' ? 'Available ✅' : 'Not available ❌',
        createClientFromCookies: typeof nextIntegration.createClientFromCookies === 'function' ? 'Available ✅' : 'Not available ❌',
      });
      setStatus('success');
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setStatus('error');
    }
  };

  const testClientCreation = async () => {
    setStatus('loading');
    setError(null);
    
    try {
      const { createClient } = await import('@game-guild/client');
      
      // Create a test client
      const client = createClient({
        baseUrl: 'http://localhost:5295',
        tenant: { getTenantId: async () => 'test-tenant' },
      });
      
      setResult({
        test: 'Client Creation',
        clientCreated: client ? 'Success ✅' : 'Failed ❌',
        baseUrl: 'http://localhost:5295',
        hasRequestMethod: typeof client.request === 'function' ? 'Available ✅' : 'Not available ❌',
        hasGetBaseUrl: typeof client.getBaseUrl === 'function' ? 'Available ✅' : 'Not available ❌',
      });
      setStatus('success');
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setStatus('error');
    }
  };

  const testTypeGeneration = async () => {
    setStatus('loading');
    setError(null);
    
    try {
      // Test that types are properly exported
      const clientModule = await import('@game-guild/client');
      
      setResult({
        test: 'Type Generation',
        hasCreateClient: 'createClient' in clientModule ? 'Available ✅' : 'Not available ❌',
        hasTypes: 'ApiError' in clientModule ? 'Available ✅' : 'Not available ❌',
        exports: Object.keys(clientModule).slice(0, 10).join(', '),
      });
      setStatus('success');
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setStatus('error');
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        <div className="bg-white shadow rounded-lg p-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            🧪 API Client Integration Tests
          </h1>
          <p className="text-gray-600 mb-8">
            Testing @game-guild/client package integration in Next.js
          </p>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-8">
            <button
              onClick={testBasicImport}
              disabled={status === 'loading'}
              className="px-4 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
            >
              Test Basic Import
            </button>

            <button
              onClick={testNextIntegration}
              disabled={status === 'loading'}
              className="px-4 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
            >
              Test Next.js Integration
            </button>

            <button
              onClick={testClientCreation}
              disabled={status === 'loading'}
              className="px-4 py-3 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
            >
              Test Client Creation
            </button>

            <button
              onClick={testTypeGeneration}
              disabled={status === 'loading'}
              className="px-4 py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
            >
              Test Type Generation
            </button>
          </div>

          {status === 'loading' && (
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-4">
              <div className="flex items-center">
                <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600 mr-3"></div>
                <p className="text-blue-900 font-medium">Running test...</p>
              </div>
            </div>
          )}

          {status === 'success' && result && (
            <div className="bg-green-50 border border-green-200 rounded-lg p-6 mb-4">
              <div className="flex items-start mb-3">
                <svg className="h-6 w-6 text-green-600 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <h3 className="text-lg font-bold text-green-900">Test Passed</h3>
              </div>
              <pre className="bg-white p-4 rounded border border-green-300 overflow-x-auto text-sm">
                {JSON.stringify(result, null, 2)}
              </pre>
            </div>
          )}

          {status === 'error' && error && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-6 mb-4">
              <div className="flex items-start mb-3">
                <svg className="h-6 w-6 text-red-600 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <h3 className="text-lg font-bold text-red-900">Test Failed</h3>
              </div>
              <pre className="bg-white p-4 rounded border border-red-300 overflow-x-auto text-sm text-red-800">
                {error}
              </pre>
            </div>
          )}

          <div className="mt-8 border-t pt-6">
            <h2 className="text-xl font-bold text-gray-900 mb-4">Package Information</h2>
            <dl className="grid grid-cols-1 gap-3 text-sm">
              <div className="bg-gray-50 px-4 py-3 rounded">
                <dt className="font-medium text-gray-600">Package Name</dt>
                <dd className="mt-1 text-gray-900">@game-guild/client</dd>
              </div>
              <div className="bg-gray-50 px-4 py-3 rounded">
                <dt className="font-medium text-gray-600">Integration Type</dt>
                <dd className="mt-1 text-gray-900">Next.js 15 App Router</dd>
              </div>
              <div className="bg-gray-50 px-4 py-3 rounded">
                <dt className="font-medium text-gray-600">Features Tested</dt>
                <dd className="mt-1 text-gray-900">
                  Basic import, Next.js integration, Client creation, Type generation
                </dd>
              </div>
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
}
