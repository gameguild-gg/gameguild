'use client';

import {
  getEditorGlobalPreferencesAction,
  updateEditorGlobalPreferencesAction,
} from '@/lib/user-settings/actions';
import {
  applyStoredGlobalPreferences,
  getAllPreferences,
  subscribeToPreferences,
} from '@/components/block-content-editor/lib/storage/editor/editor-preferences';
import * as React from 'react';

export function EditorPreferencesSyncInitializer(): null {
  React.useEffect(() => {
    let cancelled = false;
    let hydrating = true;

    const syncStoredPreferences = async (): Promise<void> => {
      const preferences = await getAllPreferences();
      const global = preferences.global;
      await updateEditorGlobalPreferencesAction({
        modalSize: global.modalSize,
        editor: { ...global.editor },
        preview: { ...global.preview },
      });
    };

    const initialize = async (): Promise<void> => {
      const result = await getEditorGlobalPreferencesAction();
      if (cancelled) return;

      if (result.success && result.data) {
        await applyStoredGlobalPreferences(result.data);
      } else {
        await syncStoredPreferences();
      }

      hydrating = false;
    };

    const unsubscribe = subscribeToPreferences(() => {
      if (!hydrating) void syncStoredPreferences();
    });

    void initialize();

    return () => {
      cancelled = true;
      unsubscribe();
    };
  }, []);

  return null;
}
