export const dynamic = 'force-static';

export async function GET(): Promise<Response> {
  return Response.json({
    name: 'Game Guild',
    short_name: 'GameGuild',
    description: 'Game development learning and community platform',
    start_url: '/',
    display: 'standalone',
    background_color: '#ffffff',
    theme_color: '#0f172a',
    icons: [
      {
        src: '/favicon.svg',
        sizes: 'any',
        type: 'image/svg+xml',
      },
    ],
  });
}
