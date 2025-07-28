'use client';

import { useRouter } from '@/i18n/navigation';
import { useEffect, useState, use, useCallback } from 'react';
import { getTestSessionBySlug, TestSession } from '@/lib/admin';
import { JoinProcessModal } from '@/components/testing-lab/join/join-process-modal';

interface JoinModalProps {
  params: Promise<{
    slug: string;
  }>;
}

export default function JoinModal({ params }: JoinModalProps) {
  const router = useRouter();
  const resolvedParams = use(params);
  const [session, setSession] = useState<TestSession | null>(null);
  const [loading, setLoading] = useState(true);

  const handleClose = useCallback(() => {
    // Proper way to close intercept modal - go back to the testing-lab page
    router.back();
  }, [router]);

  useEffect(() => {
    console.log('JoinModal (intercept from testing-lab) mounted for slug:', resolvedParams.slug);

    const fetchSession = async () => {
      try {
        const sessionData = await getTestSessionBySlug(resolvedParams.slug);
        setSession(sessionData);
      } catch (error) {
        console.error('Failed to fetch session:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchSession();
  }, [resolvedParams.slug]);

  useEffect(() => {
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

  if (loading) {
    return (
      <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4" onClick={handleBackdropClick}>
        <div className="bg-slate-900 rounded-lg border border-slate-700 p-6 max-w-2xl w-full">
          <div className="text-white text-center">Loading...</div>
        </div>
      </div>
    );
  }

  if (!session) {
    return (
      <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4" onClick={handleBackdropClick}>
        <div className="bg-slate-900 rounded-lg border border-slate-700 p-6 max-w-2xl w-full">
          <div className="text-white text-center">Session not found</div>
          <button onClick={handleClose} className="mt-4 bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded w-full">
            Close
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4" onClick={handleBackdropClick}>
      <div className="bg-slate-900 rounded-lg border border-slate-700 p-6 max-w-4xl w-full max-h-[90vh] overflow-hidden">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-bold text-white">Join Testing Session: {session.sessionName}</h2>
          <button onClick={handleClose} className="text-slate-400 hover:text-white text-2xl">
            ×
          </button>
        </div>
        <div className="overflow-y-auto max-h-[calc(90vh-8rem)]">
          <JoinProcessModal />
        </div>
      </div>
    </div>
  );
}
