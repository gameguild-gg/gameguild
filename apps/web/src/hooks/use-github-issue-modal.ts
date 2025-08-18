'use client';

import { useState, useCallback } from 'react';

interface UseGitHubIssueModalOptions {
  route?: string;
  githubIssue?: string;
}

export function useGitHubIssueModal(options: UseGitHubIssueModalOptions = {}) {
  const [isOpen, setIsOpen] = useState(false);

  const openModal = useCallback(() => {
    setIsOpen(true);
  }, []);

  const closeModal = useCallback(() => {
    setIsOpen(false);
  }, []);

  const modalProps = {
    isOpen,
    onClose: closeModal,
    route: options.route,
    githubIssue: options.githubIssue,
  };

  return {
    isOpen,
    openModal,
    closeModal,
    modalProps,
  };
}