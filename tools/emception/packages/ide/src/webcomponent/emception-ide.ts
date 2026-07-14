/**
 * <emception-ide> — light-DOM custom element wrapping the React <Ide> component.
 *
 * No shadow root; attributes are mapped to IdeProps.
 *
 * Attribute → prop mapping (all optional):
 *   title              → title
 *   manifest-url       → manifestUrl
 *   workspace-url      → workspaceUrl
 *   workspace-name     → workspaceName
 *   canvas-path        → canvasPath
 *   theme              → theme
 *   fullscreen         → fullscreen (boolean, presence = true)
 *   read-only          → readOnly  (boolean, presence = true)
 *   show-hidden-files  → showHiddenFiles (boolean, presence = true)
 *   show-solution-files → showSolutionFiles (boolean, presence = true)
 *   enable-file-explorer → enableFileExplorer (boolean default true; "false" → false)
 *   enable-tabs          → enableTabs
 *   enable-terminal      → enableTerminal
 *   enable-canvas        → enableCanvas
 *   enable-docking       → enableDocking
 *   enable-workspace     → enableWorkspace
 *
 * JS-only props (not attribute-settable): workspaceConfig, api, onStdout,
 *   onStderr, stdin, onFullscreenChange.
 */

import React from 'react';
import { createRoot, type Root } from 'react-dom/client';
import Ide from '../components/Ide';
import type { IdeProps, WorkspaceConfig } from '../components/ide-types';

export const ELEMENT_NAME = 'emception-ide' as const;

/** Boolean attributes — presence means true; value "false" means false. */
const BOOL_ATTRS = [
    'fullscreen',
    'read-only',
    'show-hidden-files',
    'show-solution-files',
    'enable-file-explorer',
    'enable-tabs',
    'enable-terminal',
    'enable-canvas',
    'enable-docking',
    'enable-workspace',
] as const;

/** String attributes */
const STRING_ATTRS = [
    'title',
    'manifest-url',
    'workspace-url',
    'workspace-name',
    'canvas-path',
    'theme',
] as const;

const ALL_OBSERVED = [...BOOL_ATTRS, ...STRING_ATTRS] as const;

function parseBool(el: Element, attr: string, defaultVal = true): boolean {
    if (!el.hasAttribute(attr)) return defaultVal;
    return el.getAttribute(attr)?.toLowerCase() !== 'false';
}

function attrToCamel(attr: string): string {
    return attr.replace(/-([a-z])/g, (_, c: string) => c.toUpperCase());
}

export class EmceptionIdeElement extends HTMLElement {
    private _root: Root | null = null;

    // JS-only props (not attributes)
    workspaceConfig: WorkspaceConfig | undefined = undefined;
    api: IdeProps['api'] = undefined;
    onStdout: IdeProps['onStdout'] = undefined;
    onStderr: IdeProps['onStderr'] = undefined;
    stdin: IdeProps['stdin'] = undefined;
    onFullscreenChange: IdeProps['onFullscreenChange'] = undefined;

    static get observedAttributes(): readonly string[] {
        return ALL_OBSERVED;
    }

    connectedCallback(): void {
        this._root = createRoot(this);
        this._render();
    }

    disconnectedCallback(): void {
        this._root?.unmount();
        this._root = null;
    }

    attributeChangedCallback(): void {
        if (this._root) this._render();
    }

    /** Call after setting JS-only props to trigger a re-render. */
    update(): void {
        if (this._root) this._render();
    }

    private _buildProps(): IdeProps {
        const props: IdeProps = {};

        // String attrs
        for (const attr of STRING_ATTRS) {
            const val = this.getAttribute(attr);
            if (val !== null) {
                (props as Record<string, unknown>)[attrToCamel(attr)] = val;
            }
        }

        // Boolean attrs (default true for enable-* attrs, false for others)
        const boolDefaults: Record<string, boolean> = {
            fullscreen: false,
            'read-only': false,
            'show-hidden-files': false,
            'show-solution-files': false,
            'enable-file-explorer': true,
            'enable-tabs': true,
            'enable-terminal': true,
            'enable-canvas': true,
            'enable-docking': true,
            'enable-workspace': true,
        };

        for (const attr of BOOL_ATTRS) {
            if (this.hasAttribute(attr)) {
                (props as Record<string, unknown>)[attrToCamel(attr)] = parseBool(this, attr, boolDefaults[attr] ?? true);
            }
        }

        // JS-only props
        if (this.workspaceConfig !== undefined) props.workspaceConfig = this.workspaceConfig;
        if (this.api !== undefined) props.api = this.api;
        if (this.onStdout !== undefined) props.onStdout = this.onStdout;
        if (this.onStderr !== undefined) props.onStderr = this.onStderr;
        if (this.stdin !== undefined) props.stdin = this.stdin;
        if (this.onFullscreenChange !== undefined) props.onFullscreenChange = this.onFullscreenChange;

        return props;
    }

    private _render(): void {
        this._root?.render(React.createElement(Ide, this._buildProps()));
    }
}

/**
 * Register `<emception-ide>` with the custom elements registry.
 * Safe to call multiple times — skips if already registered.
 */
export function registerEmceptionIde(): void {
    if (typeof customElements !== 'undefined' && !customElements.get(ELEMENT_NAME)) {
        customElements.define(ELEMENT_NAME, EmceptionIdeElement);
    }
}
