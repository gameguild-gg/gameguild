'use client';

import { useSession } from 'next-auth/react';
import { useEffect, useState } from 'react';

export function AuthDebug() {
  const { data: session, status, update } = useSession();
  const [refreshCount, setRefreshCount] = useState(0);

  const handleRefreshSession = async () => {
    console.log('🔄 [AUTH DEBUG] Manually refreshing session...');
    setRefreshCount(prev => prev + 1);
    await update();
  };

  const handleTestApiCall = async () => {
    console.log('🧪 [AUTH DEBUG] Testing API call...');
    try {
      const response = await fetch('/api/auth/session', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });
      
      if (response.ok) {
        const sessionData = await response.json();
        console.log('✅ [AUTH DEBUG] Session API call successful:', sessionData);
      } else {
        console.error('❌ [AUTH DEBUG] Session API call failed:', response.status);
      }
    } catch (error) {
      console.error('❌ [AUTH DEBUG] Session API call error:', error);
    }
  };

  useEffect(() => {
    console.log('🔍 [AUTH DEBUG] Session state changed:', {
      status,
      hasSession: !!session,
      hasAccessToken: !!(session as any)?.accessToken,
      hasError: !!(session as any)?.error,
      error: (session as any)?.error,
      userEmail: session?.user?.email,
      userId: session?.user?.id,
      refreshCount,
    });
  }, [session, status, refreshCount]);

  if (status === 'loading') {
    return (
      <div className="fixed bottom-4 right-4 bg-yellow-100 border border-yellow-400 rounded p-4 max-w-sm">
        <h3 className="font-bold text-yellow-800">Auth Debug (Loading...)</h3>
        <p className="text-sm text-yellow-700">Checking authentication status...</p>
      </div>
    );
  }

  const sessionError = (session as any)?.error;
  const hasError = !!sessionError;

  return (
    <div className={`fixed bottom-4 right-4 border rounded p-4 max-w-sm ${
      hasError ? 'bg-red-100 border-red-400' : 
      session ? 'bg-green-100 border-green-400' : 
      'bg-gray-100 border-gray-400'
    }`}>
      <h3 className={`font-bold ${
        hasError ? 'text-red-800' : 
        session ? 'text-green-800' : 
        'text-gray-800'
      }`}>
        Auth Debug ({status})
      </h3>
      
      <div className="text-sm mt-2 space-y-1">
        <div>
          <strong>Status:</strong> {status}
        </div>
        <div>
          <strong>Has Session:</strong> {session ? 'Yes' : 'No'}
        </div>
        {session && (
          <>
            <div>
              <strong>User:</strong> {session.user?.email || 'Unknown'}
            </div>
            <div>
              <strong>Access Token:</strong> {(session as any)?.accessToken ? 'Present' : 'Missing'}
            </div>
            <div>
              <strong>Error:</strong> {sessionError || 'None'}
            </div>
          </>
        )}
        <div>
          <strong>Refresh Count:</strong> {refreshCount}
        </div>
      </div>

      <div className="mt-3 space-y-2">
        <button
          onClick={handleRefreshSession}
          className="block w-full px-3 py-1 text-xs bg-blue-500 text-white rounded hover:bg-blue-600"
        >
          Refresh Session
        </button>
        <button
          onClick={handleTestApiCall}
          className="block w-full px-3 py-1 text-xs bg-purple-500 text-white rounded hover:bg-purple-600"
        >
          Test API Call
        </button>
      </div>

      {hasError && (
        <div className="mt-2 p-2 bg-red-50 border border-red-200 rounded">
          <p className="text-xs text-red-700">
            Authentication error detected. Please sign in again.
          </p>
          <button
            onClick={() => window.location.href = '/sign-in'}
            className="mt-1 px-2 py-1 text-xs bg-red-500 text-white rounded hover:bg-red-600"
          >
            Sign In
          </button>
        </div>
      )}
    </div>
  );
}
