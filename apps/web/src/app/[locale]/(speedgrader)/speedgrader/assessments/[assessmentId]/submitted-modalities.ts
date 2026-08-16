import type { LearningAssessmentsSubmissionModality } from '@game-guild/client';

const MODALITY_NAMES = ['Text', 'File', 'Url', 'Code', 'Media', 'Project', 'StructuredAnswer'] as const;

export type ModalityName = (typeof MODALITY_NAMES)[number];

const MODALITY_FLAG_BITS: Record<ModalityName, number> = {
  Text: 1,
  File: 2,
  Url: 4,
  Code: 8,
  Media: 16,
  Project: 32,
  StructuredAnswer: 64,
};

/**
 * Parse SubmittedModalities from the wire. The API serializes the backend
 * [Flags] enum as comma-separated names ("Text, Code"); tolerate a raw
 * numeric string ("3" → Text|File) and "None"/empty as no modalities.
 * Pure + client/server safe.
 */
export function parseSubmittedModalities(raw: LearningAssessmentsSubmissionModality | null | undefined): Set<ModalityName> {
  const result = new Set<ModalityName>();
  const trimmed = (raw ?? '').trim();
  if (!trimmed || trimmed === 'None') return result;

  const asNumber = Number.parseInt(trimmed, 10);
  if (Number.isFinite(asNumber) && String(asNumber) === trimmed) {
    for (const name of MODALITY_NAMES) {
      if ((asNumber & MODALITY_FLAG_BITS[name]) !== 0) result.add(name);
    }
    return result;
  }

  for (const part of trimmed.split(',')) {
    const name = part.trim();
    if ((MODALITY_NAMES as readonly string[]).includes(name)) {
      result.add(name as ModalityName);
    }
  }
  return result;
}
