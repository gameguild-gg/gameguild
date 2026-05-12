'use client';

import { GitHubIssueModal } from '@/components/ui/github-issue-modal';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

export default function JobsPage() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const router = useRouter();
  const params = useParams<{ locale: string }>();

  useEffect(() => {
    // Show modal immediately when page loads
    setIsModalOpen(true);
  }, []);

  const handleClose = () => {
    setIsModalOpen(false);
    // Navigate back to home page when modal is closed
    router.push(`/${params.locale}`);
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