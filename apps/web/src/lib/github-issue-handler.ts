'use client';

import { GitHubIssueModal } from '@/components/ui/github-issue-modal';
import { createRoot } from 'react-dom/client';
import React from 'react';

interface GitHubIssueConfig {
  route?: string;
  githubIssue?: string;
}

class GitHubIssueHandler {
  private modalContainer: HTMLDivElement | null = null;
  private modalRoot: any = null;
  private isModalOpen = false;

  constructor() {
    if (typeof window !== 'undefined') {
      this.init();
    }
  }

  private init() {
    // Add global click listener
    document.addEventListener('click', this.handleClick.bind(this));
  }

  private handleClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    const anchor = target.closest('a[data-github-issue]');
    
    if (anchor && anchor.hasAttribute('data-github-issue')) {
      event.preventDefault();
      event.stopPropagation();
      
      const route = anchor.getAttribute('data-route') || window.location.pathname;
      const githubIssueValue = anchor.getAttribute('data-github-issue');
      // Only pass githubIssue if it's not just "true"
      const githubIssue = githubIssueValue && githubIssueValue !== 'true' ? githubIssueValue : undefined;
      
      this.showModal({ route, githubIssue });
    }
  }

  private showModal(config: GitHubIssueConfig) {
    if (this.isModalOpen) return;
    
    this.isModalOpen = true;
    
    // Create modal container if it doesn't exist
    if (!this.modalContainer) {
      this.modalContainer = document.createElement('div');
      this.modalContainer.id = 'github-issue-modal-container';
      document.body.appendChild(this.modalContainer);
      this.modalRoot = createRoot(this.modalContainer);
    }

    // Render the modal
    const modalElement = React.createElement(GitHubIssueModal, {
      isOpen: true,
      onClose: this.closeModal.bind(this),
      route: config.route,
      githubIssue: config.githubIssue
    });

    this.modalRoot.render(modalElement);
  }

  private closeModal() {
    this.isModalOpen = false;
    
    if (this.modalRoot && this.modalContainer) {
      this.modalRoot.render(null);
    }
  }

  public destroy() {
    document.removeEventListener('click', this.handleClick.bind(this));
    
    if (this.modalContainer) {
      document.body.removeChild(this.modalContainer);
      this.modalContainer = null;
      this.modalRoot = null;
    }
  }
}

// Create global instance
let globalHandler: GitHubIssueHandler | null = null;

export function initGitHubIssueHandler() {
  if (typeof window !== 'undefined' && !globalHandler) {
    globalHandler = new GitHubIssueHandler();
  }
  return globalHandler;
}

export function destroyGitHubIssueHandler() {
  if (globalHandler) {
    globalHandler.destroy();
    globalHandler = null;
  }
}