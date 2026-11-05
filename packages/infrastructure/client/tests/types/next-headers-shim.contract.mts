/// <reference path="../../src/integrations/next/next-shims.d.ts" />
import type { headers } from 'next/headers';

type Assert<T extends true> = T;
type RequestHeaders = Awaited<ReturnType<typeof headers>>;
type HeaderValue = ReturnType<RequestHeaders['get']>;
type IsAny<T> = 0 extends 1 & T ? true : false;

export type HeaderValueIsTyped = Assert<IsAny<HeaderValue> extends true ? false : true>;
export type HeaderValueIsNullableString = Assert<[HeaderValue, string | null] extends [string | null, HeaderValue] ? true : false>;
export type HeadersAreReadOnly = Assert<'set' extends keyof RequestHeaders ? false : true>;
