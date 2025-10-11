'use client';

import { useRouter } from '@/i18n/navigation';
import { useEffect, useCallback } from 'react';

export default function JoinModal() {
  const router = useRouter();

  const handleClose = useCallback(() => {
    // Proper way to close intercept modal - go back to the session page
    router.back();
  }, [router]);

  useEffect(() => {
    console.log('JoinModal intercepting route component mounted!');

    const handleEscapeKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        handleClose();
      }
    };

    document.addEventListener('keydown', handleEscapeKey);
    return () => document.removeEventListener('keydown', handleEscapeKey);
  }, [handleClose]);

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      handleClose();
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4" onClick={handleBackdropClick}>
      <div className="bg-slate-900 rounded-lg border border-slate-700 p-6 max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-bold text-white">Join Testing Session (Modal)</h2>
          <button onClick={handleClose} className="text-slate-400 hover:text-white text-2xl">
            ×
          </button>
        </div>
        <div className="space-y-4 text-white">
          <p>This is the intercepting route modal!</p>
          <p>If you see this, the intercepting route is working correctly.</p>
          <button onClick={handleClose} className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded">
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
