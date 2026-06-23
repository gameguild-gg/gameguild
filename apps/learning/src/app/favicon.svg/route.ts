export const dynamic = 'force-static';

const faviconSvg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" role="img" aria-label="GameGuild Learning">
  <rect width="64" height="64" rx="16" fill="#0f172a"/>
  <path d="M16 38c0-11 7-19 16-19s16 8 16 19v3c0 3-2 5-5 5h-3c-2 0-3-1-4-3l-1-3h-6l-1 3c-1 2-2 3-4 3h-3c-3 0-5-2-5-5v-3Z" fill="#38bdf8"/>
  <path d="M24 34h-4v-4h4v-4h4v4h4v4h-4v4h-4v-4Zm20-2a3 3 0 1 1-6 0 3 3 0 0 1 6 0Zm-6 9a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z" fill="#f8fafc"/>
</svg>`;

export async function GET(): Promise<Response> {
  return new Response(faviconSvg, {
    headers: {
      'Content-Type': 'image/svg+xml; charset=utf-8',
      'Cache-Control': 'public, max-age=31536000, immutable',
    },
  });
}
