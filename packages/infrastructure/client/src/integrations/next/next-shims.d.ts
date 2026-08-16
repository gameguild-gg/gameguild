declare module 'next/navigation' {
  export function redirect(url: string): never;
}

declare module 'next/headers' {
  interface CookieStoreValue {
    value: string;
  }

  interface CookieStoreSetOptions {
    path?: string;
    domain?: string;
    expires?: Date;
    httpOnly?: boolean;
    maxAge?: number;
    sameSite?: boolean | 'lax' | 'strict' | 'none';
    secure?: boolean;
  }

  interface CookieStore {
    get(name: string): CookieStoreValue | undefined;
    set(name: string, value: string, options?: CookieStoreSetOptions): void;
  }

  export function cookies(): CookieStore | Promise<CookieStore>;
}
