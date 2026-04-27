// @emception/ide — Phase 8 public surface.

export type { EmceptionAPI } from '@emception/core';
export { default as Ide } from './components/Ide';
export type { IdeProps, InjectedEmceptionAPI } from './components/ide-types';
export { ELEMENT_NAME, EmceptionIdeElement, registerEmceptionIde } from './webcomponent/emception-ide';

