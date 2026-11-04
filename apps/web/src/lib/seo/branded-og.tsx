/**
 * Shared branded OpenGraph image template (issue #1209).
 *
 * Every public route without its own content image renders this: dark
 * GameGuild background, wordmark, and the route title. Kept dependency-free
 * (system font stack — no font file in the ~500KB ImageResponse bundle cap).
 */
import { ImageResponse } from 'next/og';

export const OG_IMAGE_SIZE = { width: 1200, height: 630 } as const;
export const OG_IMAGE_CONTENT_TYPE = 'image/png';

export interface BrandedOgImageProps {
  readonly title: string;
  readonly subtitle?: string;
}

export function brandedOgAlt(title: string): string {
  return `${title} — GameGuild`;
}

/** Branded fallback OG image: dark bg, wordmark, route title. */
export function brandedOgImage({ title, subtitle }: BrandedOgImageProps): ImageResponse {
  return new ImageResponse(
    (
      <div
        style={{
          width: '100%',
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
          padding: '72px',
          backgroundColor: '#070a12',
          backgroundImage:
            'radial-gradient(circle at 85% 15%, rgba(56, 189, 248, 0.18) 0%, rgba(7, 10, 18, 0) 55%), radial-gradient(circle at 10% 90%, rgba(129, 140, 248, 0.14) 0%, rgba(7, 10, 18, 0) 50%)',
          color: '#f8fafc',
          fontFamily: 'sans-serif',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: '56px',
              height: '56px',
              borderRadius: '14px',
              backgroundColor: '#0ea5e9',
              color: '#f8fafc',
              fontSize: '30px',
              fontWeight: 700,
            }}
          >
            GG
          </div>
          <div style={{ display: 'flex', flexDirection: 'column' }}>
            <span style={{ fontSize: '34px', fontWeight: 700, letterSpacing: '-0.5px' }}>GameGuild</span>
            {subtitle ? (
              <span style={{ fontSize: '20px', color: '#94a3b8' }}>{subtitle}</span>
            ) : null}
          </div>
        </div>
        <div
          style={{
            display: 'flex',
            fontSize: title.length > 40 ? '60px' : '76px',
            fontWeight: 700,
            lineHeight: 1.1,
            letterSpacing: '-1.5px',
            maxWidth: '1000px',
          }}
        >
          {title}
        </div>
        <div style={{ display: 'flex', fontSize: '24px', color: '#64748b' }}>gameguild.gg</div>
      </div>
    ),
    { ...OG_IMAGE_SIZE },
  );
}
