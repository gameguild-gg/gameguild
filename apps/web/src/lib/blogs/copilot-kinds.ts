/**
 * Copilot proposal-kind auto-selection for blog authoring.
 *
 * Server-side guardrail (BlogAiProposalKindNotAllowedException) allows:
 *   Markdown → {ReplaceDocument, InsertAtCursor, MetadataPatch}
 *   Lexical  → {LexicalPatch, MetadataPatch}
 * This matrix mirrors that contract for UX; the server remains the authority.
 */

export type BlogProposalKind =
  | "ReplaceDocument"
  | "InsertAtCursor"
  | "LexicalPatch"
  | "MetadataPatch";

export type CopilotMode = "content" | "metadata";

export type CopilotFormat = "Markdown" | "Lexical";

export function selectProposalKind(
  format: CopilotFormat,
  hasSelection: boolean,
  mode: CopilotMode,
): BlogProposalKind {
  if (mode === "metadata") return "MetadataPatch";
  if (format === "Lexical") return "LexicalPatch";
  if (hasSelection) return "InsertAtCursor";
  return "ReplaceDocument";
}
