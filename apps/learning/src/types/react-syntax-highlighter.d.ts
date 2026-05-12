declare module 'react-syntax-highlighter' {
    import type { ComponentType, ReactNode } from 'react';

    export interface SyntaxHighlighterProps {
        language?: string;
        style?: unknown;
        PreTag?: keyof JSX.IntrinsicElements | ComponentType<unknown>;
        customStyle?: Record<string, string | number>;
        codeTagProps?: Record<string, unknown>;
        wrapLines?: boolean;
        children?: ReactNode;
    }

    export const Prism: ComponentType<SyntaxHighlighterProps>;
}

declare module 'react-syntax-highlighter/dist/esm/styles/prism' {
    export const vscDarkPlus: Record<string, unknown>;
}