export type QuizEditorModalSize =
  | "compact"
  | "widescreen"
  | "ultrawide"
  | "fullscreen";

const DB_NAME = "quiz-surface-editor-preferences";
const DB_VERSION = 1;
const STORE_NAME = "preferences";
const MODAL_SIZE_KEY = "modal-size";
const MODAL_SIZES: QuizEditorModalSize[] = [
  "compact",
  "widescreen",
  "ultrawide",
  "fullscreen",
];

export const DEFAULT_QUIZ_EDITOR_MODAL_SIZE: QuizEditorModalSize = "widescreen";

const listeners = new Set<() => void>();

export function normalizeQuizEditorModalSize(
  value: unknown,
): QuizEditorModalSize {
  return MODAL_SIZES.includes(value as QuizEditorModalSize)
    ? (value as QuizEditorModalSize)
    : DEFAULT_QUIZ_EDITOR_MODAL_SIZE;
}

function openDatabase(): Promise<IDBDatabase | null> {
  if (typeof indexedDB === "undefined") return Promise.resolve(null);

  return new Promise((resolve) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    request.onerror = () => resolve(null);
    request.onsuccess = () => resolve(request.result);
    request.onupgradeneeded = () => {
      const database = request.result;
      if (!database.objectStoreNames.contains(STORE_NAME)) {
        database.createObjectStore(STORE_NAME);
      }
    };
  });
}

export async function getQuizEditorModalSize(): Promise<QuizEditorModalSize> {
  const database = await openDatabase();
  if (!database) return DEFAULT_QUIZ_EDITOR_MODAL_SIZE;

  return new Promise((resolve) => {
    const request = database
      .transaction(STORE_NAME, "readonly")
      .objectStore(STORE_NAME)
      .get(MODAL_SIZE_KEY);
    request.onerror = () => resolve(DEFAULT_QUIZ_EDITOR_MODAL_SIZE);
    request.onsuccess = () =>
      resolve(normalizeQuizEditorModalSize(request.result));
  });
}

export async function setQuizEditorModalSize(
  modalSize: QuizEditorModalSize,
): Promise<void> {
  const database = await openDatabase();
  if (database) {
    await new Promise<void>((resolve) => {
      const request = database
        .transaction(STORE_NAME, "readwrite")
        .objectStore(STORE_NAME)
        .put(modalSize, MODAL_SIZE_KEY);
      request.onerror = () => resolve();
      request.onsuccess = () => resolve();
    });
  }
  listeners.forEach((listener) => listener());
}

export function subscribeToQuizEditorPreferences(
  listener: () => void,
): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}
