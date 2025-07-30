import type { MetadataRoute } from 'next';

export default async function manifest(): Promise<MetadataRoute.Manifest> {
  return {
    name: 'Game Guild',
    short_name: 'Game Guild',
    description: '',
    start_url: '/',
    display: 'standalone',
  };
}
