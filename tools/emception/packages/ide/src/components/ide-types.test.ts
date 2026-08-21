import { deriveStorageKey, WORKSPACE_STORAGE_KEY } from './ide-types.js';

// ─── WORKSPACE_STORAGE_KEY ───────────────────────────────────────────────────

describe('WORKSPACE_STORAGE_KEY', () => {
    it('is package-specific without host-product branding', () => {
        expect(WORKSPACE_STORAGE_KEY).toBe('emception.workspace.v1');
    });
});

// ─── deriveStorageKey ────────────────────────────────────────────────────────

describe('deriveStorageKey', () => {
    it('returns the default key when called with no argument', () => {
        expect(deriveStorageKey()).toBe(WORKSPACE_STORAGE_KEY);
    });

    it('returns the default key when called with undefined', () => {
        expect(deriveStorageKey(undefined)).toBe(WORKSPACE_STORAGE_KEY);
    });

    it('returns the default key when called with an empty string', () => {
        // Empty string is falsy and falls back to the default key.
        expect(deriveStorageKey('')).toBe(WORKSPACE_STORAGE_KEY);
    });

    it('returns a namespaced key for a non-empty workspace name', () => {
        expect(deriveStorageKey('demo')).toBe('emception:ws:demo');
    });

    it('uses the workspace name verbatim (no sanitising)', () => {
        expect(deriveStorageKey('hello world')).toBe('emception:ws:hello world');
    });

    it('different names produce different keys', () => {
        expect(deriveStorageKey('a')).not.toBe(deriveStorageKey('b'));
    });

    it('the default key is NOT returned for a non-empty name', () => {
        expect(deriveStorageKey('any')).not.toBe(WORKSPACE_STORAGE_KEY);
    });
});
