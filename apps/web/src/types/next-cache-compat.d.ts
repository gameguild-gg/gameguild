declare module 'next/cache' {
    /**
     * Backward-compatible overload used by existing server actions.
     * Next.js 16 types require a second argument, but legacy call sites still use a single tag.
     */
    export function revalidateTag(tag: string): void;
}
