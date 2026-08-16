'use client';

/** Prose render of a TextPayload submission. */
export function TextViewer({ text }: { text: string }): React.JSX.Element {
  return (
    <div data-testid="text-viewer" className="whitespace-pre-wrap rounded-md border bg-card p-4 text-sm text-card-foreground">
      {text}
    </div>
  );
}
