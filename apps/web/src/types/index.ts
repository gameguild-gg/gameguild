import { Locale } from 'next-intl';
import { ReactNode } from 'react';

export type RequiredFields<T, K extends keyof T> = T & { [P in K]-?: T[P] };
export type OptionalFields<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;
export type BaseIdentifier = number | string;

export type ParamsWithId<P = unknown> = P & { id: string };

export type ParamsWithLocale<P = unknown> = P & { locale: Locale };

export type ParamsWithSlug<P = unknown> = P & { slug: string };

export type PropsWithIdParams<P = unknown> = P & { params: Promise<ParamsWithId> };

export type PropsWithSlugParams<P = unknown> = P & { params: Promise<ParamsWithSlug> };

export type PropsWithModal<P = unknown> = P & { modal?: ReactNode };

export type PropsWithLocaleParams<P = unknown> = P & {
  params: Promise<ParamsWithLocale>;
};

export interface ErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export type PropsWithLocaleSlugParams<P = unknown> = P & {
  params: Promise<ParamsWithLocale<ParamsWithSlug>>;
};

export type Identifiable<T = unknown, TIdentifier extends BaseIdentifier = string> = T & { id: TIdentifier };

export type InvertRecord<T extends Record<string, PropertyKey>> = { [V in T[keyof T]]: { [K in keyof T]: T[K] extends V ? K : never }[keyof T] };

export const invertRecord = <T extends Record<string, PropertyKey>>(object: T): InvertRecord<T> => {
  return Object.fromEntries(Object.entries(object).map(([key, value]) => [value, key])) as InvertRecord<T>;
};

export type ActionCompleteResult<T> = T extends object
  ? { status: HttpStatusCode; data?: T }
  : {
      success: HttpStatusCode;
    };
export type ActionErrorResult = { status: HttpStatusCode; success: false; error: Error };
export type ActionValidationErrorResult<T> = T extends object
  ? {
      status: HttpStatusCode;
      success: false;
      fieldErrors: Partial<Record<keyof T, Error>>;
    }
  : never;

export type ActionResult<T = unknown> = ActionCompleteResult<T> | ActionErrorResult | ActionValidationErrorResult<T>;

export type OGImageDescriptor = {
  url: string | URL;
  alt?: string;
  width?: string | number;
  height?: string | number;
};

export const HttpStatus = {
  CONTINUE: 100,
  SWITCHING_PROTOCOLS: 101,
  PROCESSING: 102,
  EARLYHINTS: 103,
  OK: 200,
  CREATED: 201,
  ACCEPTED: 202,
  NON_AUTHORITATIVE_INFORMATION: 203,
  NO_CONTENT: 204,
  RESET_CONTENT: 205,
  PARTIAL_CONTENT: 206,
  MULTI_STATUS: 207,
  ALREADY_REPORTED: 208,
  CONTENT_DIFFERENT: 210,
  AMBIGUOUS: 300,
  MOVED_PERMANENTLY: 301,
  FOUND: 302,
  SEE_OTHER: 303,
  NOT_MODIFIED: 304,
  TEMPORARY_REDIRECT: 307,
  PERMANENT_REDIRECT: 308,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  PAYMENT_REQUIRED: 402,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  METHOD_NOT_ALLOWED: 405,
  NOT_ACCEPTABLE: 406,
  PROXY_AUTHENTICATION_REQUIRED: 407,
  REQUEST_TIMEOUT: 408,
  CONFLICT: 409,
  GONE: 410,
  LENGTH_REQUIRED: 411,
  PRECONDITION_FAILED: 412,
  PAYLOAD_TOO_LARGE: 413,
  URI_TOO_LONG: 414,
  UNSUPPORTED_MEDIA_TYPE: 415,
  REQUESTED_RANGE_NOT_SATISFIABLE: 416,
  EXPECTATION_FAILED: 417,
  I_AM_A_TEAPOT: 418,
  MISDIRECTED: 421,
  UNPROCESSABLE_ENTITY: 422,
  LOCKED: 423,
  FAILED_DEPENDENCY: 424,
  PRECONDITION_REQUIRED: 428,
  TOO_MANY_REQUESTS: 429,
  UNRECOVERABLE_ERROR: 456,
  INTERNAL_SERVER_ERROR: 500,
  NOT_IMPLEMENTED: 501,
  BAD_GATEWAY: 502,
  SERVICE_UNAVAILABLE: 503,
  GATEWAY_TIMEOUT: 504,
  HTTP_VERSION_NOT_SUPPORTED: 505,
  INSUFFICIENT_STORAGE: 507,
  LOOP_DETECTED: 508,
} as const;

export type HttpStatusCode = (typeof HttpStatus)[keyof typeof HttpStatus];

export const HttpStatusName = invertRecord(HttpStatus) as Record<HttpStatusCode, keyof typeof HttpStatus>;

export type ExtractHttpStatusCode<Prefix extends string, S extends HttpStatusCode = HttpStatusCode> = S extends `${Prefix}${string}` ? S : never;

// Informational responses (100 – 199)
export type HTTP_STATUS_CODES_1XX = ExtractHttpStatusCode<'1'>;

// Successful responses (200 – 299)
export type HTTP_STATUS_CODES_2xx = ExtractHttpStatusCode<'2'>;

// Redirection messages (300 – 399)
export type HTTP_STATUS_CODES_3xx = ExtractHttpStatusCode<'3'>;

// Client error responses (400 – 499)
export type HTTP_STATUS_CODES_4xx = ExtractHttpStatusCode<'4'>;

// Server error responses (500 – 599)
export type HTTP_STATUS_CODES_5xx = ExtractHttpStatusCode<'5'>;
