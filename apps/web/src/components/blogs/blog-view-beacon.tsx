'use client';

import { useEffect } from 'react';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080';

function sessionStorageKey(postId: string): string {
  return `blog-view-${postId}`;
}

export function BlogViewBeacon({ postId }: { postId: string }) {
  useEffect(() => {
    const key = sessionStorageKey(postId);
    if (typeof window === 'undefined' || sessionStorage.getItem(key)) return;

    sessionStorage.setItem(key, '1');
    void fetch(`${API_BASE_URL.replace(/\/$/, '')}/api/social/blog/public/posts/${postId}/views`, {
      method: 'POST',
      credentials: 'include',
    }).catch(() => {
      /* view counting is best-effort */
    });
  }, [postId]);

  return null;
}
