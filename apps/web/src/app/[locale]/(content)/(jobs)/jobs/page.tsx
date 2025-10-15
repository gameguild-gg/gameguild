'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { GitHubIssueModal } from '@/components/ui/github-issue-modal';

export default function JobsPage() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const router = useRouter();

  useEffect(() => {
    // Show modal immediately when page loads
    setIsModalOpen(true);
  }, []);

  const handleClose = () => {
    setIsModalOpen(false);
    // Navigate back to home page when modal is closed
    router.push('/');
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 flex items-center justify-center p-4">
      <GitHubIssueModal 
        isOpen={isModalOpen} 
        onClose={handleClose}
        route="/jobs"
      />
    </div>
  );
}