// @emception/webcomponent — <emception-run> custom element. Phase 6.

export const ELEMENT_NAME = 'emception-run';

if (typeof customElements !== 'undefined' && !customElements.get(ELEMENT_NAME)) {
  // Stub registration so consumers see the tag exists; real impl in Phase 6.
  class EmceptionRunStub extends HTMLElement {
    connectedCallback() {
      this.textContent =
        '<emception-run> not yet implemented (Phase 6). See tools/emception/docs/dx-overhaul-plan.md.';
    }
  }
  customElements.define(ELEMENT_NAME, EmceptionRunStub);
}
