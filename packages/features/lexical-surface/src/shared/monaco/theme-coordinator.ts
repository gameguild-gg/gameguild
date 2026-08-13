import type { Monaco } from "@monaco-editor/react";

interface Registration {
  monaco: Monaco;
  getTheme: () => string;
}

const registrations: Registration[] = [];

function applyDominantTheme(): void {
  const registration = registrations.at(-1);
  if (!registration) return;
  registration.monaco.editor.setTheme(registration.getTheme());
}

export interface MonacoThemeHandle {
  refresh: () => void;
  unregister: () => void;
}

export function registerMonacoSurface(
  monaco: Monaco,
  getTheme: () => string,
): MonacoThemeHandle {
  const registration = { monaco, getTheme };
  registrations.push(registration);
  applyDominantTheme();
  let registered = true;

  return {
    refresh() {
      if (registered) applyDominantTheme();
    },
    unregister() {
      if (!registered) return;
      registered = false;
      const index = registrations.lastIndexOf(registration);
      if (index >= 0) registrations.splice(index, 1);
      applyDominantTheme();
    },
  };
}
