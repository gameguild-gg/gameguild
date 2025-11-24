import { Locale } from 'next-intl';

export type ParamsWithLocale<P = unknown> = P & { locale: Locale };

export type ParamsWithSlug<P = unknown> = P & { slug: string };

export type ParamsWithId<P = unknown> = P & { id: string };

export type PropsWithSlugParams<P = unknown> = P & { params: Promise<ParamsWithSlug> };

export type PropsWithIdParams<P = unknown> = P & { params: Promise<ParamsWithId> };

export type PropsWithLocaleParams<P = unknown> = P & { params: Promise<ParamsWithLocale> };
