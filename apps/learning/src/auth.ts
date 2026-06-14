import { CredentialsProvider, encodeSession, GameGuildAuth, processSession, resolveCookieOptions, SessionStore } from '@game-guild/client';
import { cookies } from 'next/headers';
import { cache } from 'react';

const result = GameGuildAuth({
    providers: [CredentialsProvider()],
    apiUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295',
    secret:
        process.env.AUTH_SECRET ||
        process.env.NEXTAUTH_SECRET ||
        (process.env.NODE_ENV === 'development' ? 'game-guild-learning-local-development-secret' : undefined),
    debug: process.env.NODE_ENV === 'development',
});

export const { handlers, auth, signIn, signOut, signUp, update } = result;
export const authConfig = result.config;

export const getToken = cache(async (): Promise<string | null> => {
    const cookieStore = await cookies();
    const cookieOptions = resolveCookieOptions(authConfig.cookies, authConfig.cookies.secure);
    const sessionStore = new SessionStore(cookieOptions);

    const encrypted = sessionStore.read((name: string) => cookieStore.get(name)?.value);
    if (!encrypted) {
        return null;
    }

    const { token, updated } = await processSession(encrypted, authConfig);

    if (updated && token) {
        try {
            const newEncrypted = await encodeSession(token, authConfig);
            sessionStore.write(newEncrypted, (name: string, value: string, opts: unknown) => {
                cookieStore.set(name, value, opts as Parameters<typeof cookieStore.set>[2]);
            });
        } catch {
            // Cookie may be read-only in some contexts.
        }
    }

    return token?.accessToken ?? null;
});
