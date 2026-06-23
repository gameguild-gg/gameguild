export const dynamic = 'force-static';

export async function GET(): Promise<Response> {
  return Response.json({
    name: 'Game Guild Learning',
    short_name: 'GameGuild',
    description: 'GameGuild course and classroom experience',
    start_url: '/',
    display: 'standalone',
    background_color: '#020617',
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
