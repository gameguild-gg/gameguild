'use client';

/**
 * UrlPayload viewer: prominent anchor (new tab, noopener) + sandboxed iframe
 * embed attempt. Sites that refuse framing still leave the anchor usable.
 */
export function UrlViewer({ url }: { url: string }): React.JSX.Element {
  return (
    <div data-testid="url-viewer" className="space-y-2">
      <a
        data-testid="url-anchor"
        href={url}
        target="_blank"
        rel="noopener noreferrer"
        className="break-all text-sm font-medium text-primary underline-offset-4 hover:underline"
      >
        {url}
      </a>
      <iframe
        data-testid="url-embed"
        src={url}
        title="Submitted URL preview"
        sandbox=""
        referrerPolicy="no-referrer"
        className="h-[60vh] w-full rounded-md border bg-card"
      />
    </div>
  );
}
