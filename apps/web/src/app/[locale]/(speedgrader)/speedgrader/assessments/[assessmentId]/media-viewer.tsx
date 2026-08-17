'use client';

const AUDIO_EXTENSIONS = ['.mp3', '.wav', '.ogg', '.m4a', '.flac', '.aac'];

function isAudioUrl(url: string): boolean {
  const lower = (url.split('?')[0] ?? url).toLowerCase();
  return AUDIO_EXTENSIONS.some((ext) => lower.endsWith(ext));
}

/**
 * MediaPayload viewer: audio extensions get <audio controls>, everything
 * else (video URLs, embeds) gets <video controls>.
 */
export function MediaViewer({ url }: { url: string }): React.JSX.Element {
  if (isAudioUrl(url)) {
    return (
      <div data-testid="media-viewer" className="rounded-md border bg-card p-4">
        {/* eslint-disable-next-line jsx-a11y/media-has-caption -- submitted media carries no captions */}
        <audio data-testid="media-audio" src={url} controls className="w-full">
          <a href={url}>Download audio</a>
        </audio>
      </div>
    );
  }
  return (
    <div data-testid="media-viewer" className="rounded-md border bg-card p-4">
      {/* eslint-disable-next-line jsx-a11y/media-has-caption -- submitted media carries no captions */}
      <video data-testid="media-video" src={url} controls className="max-h-[70vh] w-full">
        <a href={url}>Download video</a>
      </video>
    </div>
  );
}
