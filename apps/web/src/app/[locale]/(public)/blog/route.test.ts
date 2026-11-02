import { describe, expect, it } from 'vitest';

import { GET } from './route';

describe('legacy /blog index redirect route handler', () => {
  it('308-redirects to the internal blog index with noindex', () => {
    const response = GET();

    expect(response.status).toBe(308);
    expect(response.headers.get('Location')).toBe('/blogs');
    expect(response.headers.get('X-Robots-Tag')).toBe('noindex');
  });
});
