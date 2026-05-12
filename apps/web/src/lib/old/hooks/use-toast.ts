'use client';

import { useCallback } from 'react';
import { toast as sonnerToast } from 'sonner';

export interface Toast {
  id: string;
  title?: string;
  description?: string;
  variant?: 'default' | 'success' | 'error' | 'warning';
  duration?: number;
}

export function useToast() {
  const toast = useCallback((options: Omit<Toast, 'id'>) => {
    const message = options.title ?? options.description ?? '';
    const toastOptions = {
      description: options.title ? options.description : undefined,
      duration: options.duration,
    };

    const id =
      options.variant === 'success'
        ? sonnerToast.success(message, toastOptions)
        : options.variant === 'error'
          ? sonnerToast.error(message, toastOptions)
          : options.variant === 'warning'
            ? sonnerToast.warning(message, toastOptions)
            : sonnerToast(message, toastOptions);

    return String(id);
  }, []);

  const dismiss = useCallback((id?: string) => {
    sonnerToast.dismiss(id);
  }, []);

  return {
    toasts: [] as Toast[],
    toast,
    dismiss,
  };
}

export default useToast;
