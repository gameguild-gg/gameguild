import {
  blocksToStorage,
  blockToView,
  storageToBlocks,
} from "@game-guild/block-list";
import { QUIZ_BLOCK_TYPE, QUIZ_CONTENT_SCHEMA_VERSION } from "./constants";
import type {
  QuizBlock,
  QuizBlockList,
  QuizBlockStorage,
  QuizBlockView,
  QuizContentDocument,
  QuizContentItem,
} from "./types";

export function createEmptyQuizContentDocument(): QuizContentDocument {
  return {
    schemaVersion: QUIZ_CONTENT_SCHEMA_VERSION,
    order: [],
    blocks: {},
  };
}

export function quizContentItemsToBlocks(
  items: readonly QuizContentItem[],
): QuizBlockList {
  return items.map((item) => ({
    id: item.id,
    type: QUIZ_BLOCK_TYPE,
    data: item.entry,
  }));
}

export function quizBlocksToContentItems(
  blocks: readonly QuizBlock[],
): QuizContentItem[] {
  return blocks.map((block) => ({ id: block.id, entry: block.data }));
}

export function quizBlocksToStorage(
  blocks: readonly QuizBlock[],
): QuizBlockStorage {
  return blocksToStorage(blocks) as QuizBlockStorage;
}

export function quizStorageToBlocks(
  storage: QuizBlockStorage | null | undefined,
): QuizBlockList {
  return storageToBlocks<"quiz", QuizBlock["data"], QuizBlock>(storage);
}

export function quizContentItemsToStorage(
  items: readonly QuizContentItem[],
): QuizBlockStorage {
  return quizBlocksToStorage(quizContentItemsToBlocks(items));
}

export function quizStorageToContentItems(
  storage: QuizBlockStorage | null | undefined,
): QuizContentItem[] {
  return quizBlocksToContentItems(quizStorageToBlocks(storage));
}

export function quizDocumentToBlocks(
  document: QuizContentDocument,
): QuizBlockList {
  return quizStorageToBlocks(document);
}

export function quizDocumentToContentItems(
  document: QuizContentDocument,
): QuizContentItem[] {
  return quizStorageToContentItems(document);
}

export function quizBlockToView(block: QuizBlock): QuizBlockView {
  return blockToView(block) as QuizBlockView;
}
