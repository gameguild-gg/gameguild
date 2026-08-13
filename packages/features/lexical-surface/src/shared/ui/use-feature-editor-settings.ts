"use client";

import { useCallback, useEffect, useState } from "react";
import {
  DEFAULT_FEATURE_EDITOR_PREFERENCES,
  getFeatureEditorPreferences,
  setFeatureModalSize,
  setGlobalMonacoPreference,
  subscribeToFeatureEditorPreferences,
  type EditorModalSize,
  type FeatureEditorPreferences,
  type MonacoSurfacePreferences,
} from "./editor-preferences";

export interface FeatureEditorSettings extends FeatureEditorPreferences {
  setModalSize: (size: EditorModalSize) => Promise<void>;
  setEditorOption: <Key extends keyof MonacoSurfacePreferences>(
    key: Key,
    value: MonacoSurfacePreferences[Key],
  ) => Promise<void>;
}

export function useFeatureEditorSettings(
  feature: string,
): FeatureEditorSettings {
  const [preferences, setPreferences] = useState(
    DEFAULT_FEATURE_EDITOR_PREFERENCES,
  );

  useEffect(() => {
    let active = true;
    const load = async () => {
      const next = await getFeatureEditorPreferences(feature);
      if (active) setPreferences(next);
    };
    void load();
    const unsubscribe = subscribeToFeatureEditorPreferences(() => void load());
    return () => {
      active = false;
      unsubscribe();
    };
  }, [feature]);

  const setModalSize = useCallback(
    async (modalSize: EditorModalSize) => {
      setPreferences((current) => ({ ...current, modalSize }));
      await setFeatureModalSize(feature, modalSize);
    },
    [feature],
  );

  const setEditorOption = useCallback(
    async <Key extends keyof MonacoSurfacePreferences>(
      key: Key,
      value: MonacoSurfacePreferences[Key],
    ) => {
      setPreferences((current) => ({
        ...current,
        editor: { ...current.editor, [key]: value },
      }));
      await setGlobalMonacoPreference(key, value);
    },
    [],
  );

  return { ...preferences, setModalSize, setEditorOption };
}
