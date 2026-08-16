'use client';

const IMAGE_EXTENSIONS = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.svg', '.bmp', '.avif'];

function fileNameOf(payload: string): string {
  const withoutQuery = payload.split('?')[0] ?? payload;
  const segments = withoutQuery.split('/');
  return segments[segments.length - 1] || withoutQuery;
}

/**
 * FilePayload viewer. The payload is a file reference (URL or filename):
 * images render inline, PDFs embed via <object>, everything else gets a
 * filename + download link.
 */
export function FileViewer({ payload }: { payload: string }): React.JSX.Element {
  const name = fileNameOf(payload);
  const lower = name.toLowerCase();
  const isImage = IMAGE_EXTENSIONS.some((ext) => lower.endsWith(ext));
  const isPdf = lower.endsWith('.pdf');

  if (isImage) {
    return (
      <div data-testid="file-viewer">
        <img data-testid="file-image" src={payload} alt={name} className="max-h-[70vh] w-full rounded-md border object-contain" />
      </div>
    );
  }

  if (isPdf) {
    return (
      <div data-testid="file-viewer">
        <object data-testid="file-pdf" data={payload} type="application/pdf" className="h-[70vh] w-full rounded-md border">
          <a href={payload} className="text-sm text-primary underline">
            {name}
          </a>
        </object>
      </div>
    );
  }

  return (
    <div data-testid="file-viewer" className="space-y-2 rounded-md border bg-card p-4">
      <div data-testid="file-fallback" className="flex items-center gap-2 text-sm">
        <span className="font-medium">{name}</span>
      </div>
      <a
        data-testid="file-download"
        href={payload}
        download={name}
        target="_blank"
        rel="noopener noreferrer"
        className="text-sm text-primary underline-offset-4 hover:underline"
      >
        Download file
      </a>
    </div>
  );
}
