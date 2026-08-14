/**
 * PagesPlugin — automatic, Word-style pagination for `<LexicalSurface />`.
 *
 * Ported from `lexical-playground/src/plugins/PagesExtension` to the classic
 * (`@lexical/react`) plugin API. The playground stores page setup in the
 * editor state; here it is driven by React props (`pageSettings`, `enabled`)
 * coming from the toolbar context, and applied to the editor root as CSS
 * custom properties.
 *
 * How it works
 *   1. Root children are wrapped into `PageNode > PageContentNode` groups.
 *   2. On every content change the affected page is measured
 *      (`scrollHeight` vs `--page-height`).
 *   3. Overflowing block-level children are physically moved to the next
 *      page (creating one if needed); underflowing pages pull content back.
 *
 * Because `PageContentNode` is a shadow root, all other plugins (selection,
 * draggable, tables, …) keep working inside each page unchanged.
 */
"use client";

import { useEffect, useRef } from "react";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import {
  $addUpdateTag,
  $createParagraphNode,
  $getNearestNodeFromDOMNode,
  $getNearestRootOrShadowRoot,
  $getNodeByKey,
  $getRoot,
  $getSelection,
  $isRangeSelection,
  $setSelection,
  COMMAND_PRIORITY_HIGH,
  COMMAND_PRIORITY_LOW,
  DELETE_CHARACTER_COMMAND,
  DELETE_LINE_COMMAND,
  DELETE_WORD_COMMAND,
  HISTORY_MERGE_TAG,
  type LexicalEditor,
  type NodeKey,
  RootNode,
  SELECTION_CHANGE_COMMAND,
  SKIP_SCROLL_INTO_VIEW_TAG,
} from "lexical";
import { mergeRegister } from "@lexical/utils";

import { $createPageNode, $isPageNode, PageNode } from "./page-node";
import {
  $createPageContentNode,
  $isPageContentNode,
  PageContentNode,
} from "./page-content-node";
import { pageBoxPx, type PageSettings } from "./page-settings";

const PAGE_PROPS = [
  "--page-width",
  "--page-height",
  "--page-margin-top",
  "--page-margin-right",
  "--page-margin-bottom",
  "--page-margin-left",
] as const;

// Inline fixed height applied to every sheet. Measurement code temporarily
// switches the page to natural height and MUST restore to this value.
const PAGE_FIXED_HEIGHT = "var(--page-height)";
// Reflow hysteresis to avoid 1-2px jitter causing page content ping-pong.
const REFLOW_HYSTERESIS_PX = 2;
// System bottom inset for paged documents.
const PAGE_DOCUMENT_BOTTOM_INSET_PX = 64;

type ReconcilableContentNode = PageContentNode & {
  reconcileObservedMutation?: (dom: HTMLElement, editor: LexicalEditor) => void;
};

export function PagesPlugin({
  pageSettings,
  enabled,
}: {
  pageSettings: PageSettings;
  enabled: boolean;
}) {
  const [editor] = useLexicalComposerContext();
  const enabledRef = useRef(enabled);
  // Set by the reflow effect so the geometry effect can trigger a re-measure
  // without tearing down and rebuilding the page structure.
  const resizeRef = useRef<(() => void) | null>(null);

  useEffect(() => {
    enabledRef.current = enabled;
  }, [enabled]);

  // ── Dimensions: write CSS custom properties onto the editor root ──────────
  useEffect(() => {
    const rootElement = editor.getRootElement();
    if (!rootElement) return;

    if (enabled) {
      const box = pageBoxPx(pageSettings);
      if (box) {
        rootElement.style.setProperty("--page-width", `${box.widthPx}px`);
        rootElement.style.setProperty("--page-height", `${box.heightPx}px`);
        rootElement.style.setProperty("--page-margin-top", `${box.marginPx}px`);
        rootElement.style.setProperty(
          "--page-margin-right",
          `${box.marginPx}px`,
        );
        rootElement.style.setProperty(
          "--page-margin-bottom",
          `${box.marginPx}px`,
        );
        rootElement.style.setProperty(
          "--page-margin-left",
          `${box.marginPx}px`,
        );
        rootElement.style.paddingBottom = `${PAGE_DOCUMENT_BOTTOM_INSET_PX}px`;
        // Re-measure every page against the new geometry (no structure teardown).
        resizeRef.current?.();
      }
    } else {
      for (const prop of PAGE_PROPS) rootElement.style.removeProperty(prop);
      rootElement.style.zoom = "";
      rootElement.style.paddingBottom = "";
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    editor,
    enabled,
    pageSettings.size,
    pageSettings.orientation,
    pageSettings.margin,
  ]);

  // ── Safe delete interception (paged + pageless) ──────────────────────────
  useEffect(() => {
    const runSafeDeleteCharacter = (isBackward: boolean) => {
      const current = $getSelection();
      if (!$isRangeSelection(current)) return;
      let selection = current;

      // In dev mode Lexical freezes committed selections; always work on a
      // writable clone before mutating points/nodes.
      if (Object.isFrozen(selection) || Object.isFrozen(selection.anchor)) {
        const writable = selection.clone();
        $setSelection(writable);
        selection = writable;
      }

      if (!selection.isCollapsed()) {
        selection.deleteCharacter(isBackward);
        return;
      }

      // Page-boundary merge logic only applies while paged mode is enabled.
      if (enabledRef.current) {
        const anchorNode = selection.anchor.getNode();
        const nearestRoot =
          anchorNode.getKey() === "root"
            ? anchorNode
            : $getNearestRootOrShadowRoot(anchorNode);

        if ($isPageContentNode(nearestRoot)) {
          const contentChildrenSize = nearestRoot.getChildrenSize() ?? 0;
          const isEmpty =
            contentChildrenSize === 1 &&
            (nearestRoot.getTextContentSize() ?? 0) === 0;

          if (isEmpty && isBackward) {
            const pageNode = nearestRoot.getPageNode();
            const previousSibling = pageNode.getPreviousSibling();
            if ($isPageNode(previousSibling)) {
              pageNode.remove();
              previousSibling.getContentNode().selectEnd();
            }
            return;
          }

          if (isBackward && selection.anchor.offset === 0) {
            const topLevelElement = anchorNode.getTopLevelElement();
            if (
              topLevelElement !== null &&
              topLevelElement.getIndexWithinParent() === 0
            ) {
              const previousSibling = nearestRoot
                .getPageNode()
                .getPreviousSibling();
              if (!$isPageNode(previousSibling)) return;
              previousSibling.getContentNode().append(topLevelElement);
              topLevelElement.selectStart().deleteCharacter(true);
              return;
            }
          } else if (
            !isBackward &&
            selection.anchor.offset === anchorNode.getTextContentSize()
          ) {
            const topLevelElement = anchorNode.getTopLevelElement();
            if (
              topLevelElement !== null &&
              topLevelElement.getIndexWithinParent() === contentChildrenSize - 1
            ) {
              const nextSibling = nearestRoot.getPageNode().getNextSibling();
              if (!$isPageNode(nextSibling)) return;
              const nextPageContent = nextSibling.getContentNode();
              const nextPageFirstChild = nextPageContent.getFirstChild();
              if (!nextPageFirstChild) return;
              nearestRoot.append(nextPageFirstChild);
              nextPageFirstChild.selectStart().deleteCharacter(true);
              return;
            }
          }
        }
      }

      selection.deleteCharacter(isBackward);
    };

    const runSafeDeleteWord = (isBackward: boolean) => {
      const current = $getSelection();
      if (!$isRangeSelection(current)) return;
      let selection = current;
      if (Object.isFrozen(selection) || Object.isFrozen(selection.anchor)) {
        const writable = selection.clone();
        $setSelection(writable);
        selection = writable;
      }
      selection.deleteWord(isBackward);
    };

    const runSafeDeleteLine = (isBackward: boolean) => {
      const current = $getSelection();
      if (!$isRangeSelection(current)) return;
      let selection = current;
      if (Object.isFrozen(selection) || Object.isFrozen(selection.anchor)) {
        const writable = selection.clone();
        $setSelection(writable);
        selection = writable;
      }
      selection.deleteLine(isBackward);
    };

    const scheduleSafeDelete = (work: () => void) => {
      queueMicrotask(() => {
        if (!editor.isEditable()) return;
        editor.update(work, { discrete: true });
      });
    };

    return mergeRegister(
      editor.registerCommand(
        DELETE_CHARACTER_COMMAND,
        (isBackward: boolean) => {
          if (!editor.isEditable()) return false;
          scheduleSafeDelete(() => runSafeDeleteCharacter(isBackward));
          return true;
        },
        COMMAND_PRIORITY_HIGH,
      ),
      editor.registerCommand(
        DELETE_WORD_COMMAND,
        (isBackward: boolean) => {
          if (!editor.isEditable()) return false;
          scheduleSafeDelete(() => runSafeDeleteWord(isBackward));
          return true;
        },
        COMMAND_PRIORITY_HIGH,
      ),
      editor.registerCommand(
        DELETE_LINE_COMMAND,
        (isBackward: boolean) => {
          if (!editor.isEditable()) return false;
          scheduleSafeDelete(() => runSafeDeleteLine(isBackward));
          return true;
        },
        COMMAND_PRIORITY_HIGH,
      ),
    );
  }, [editor]);

  // ── Structure + reflow engine ─────────────────────────────────────────────
  useEffect(() => {
    const isEnabled = () => enabledRef.current;

    // Removes all PageNode wrappers, moving their content back to the root.
    const destroyPageStructure = () => {
      editor.update(
        () => {
          const root = $getRoot();
          for (const child of root.getChildren()) {
            if ($isPageNode(child)) {
              const contentNode = child.getContentNode();
              contentNode.getChildren().forEach((c) => child.insertBefore(c));
              child.remove();
            }
          }
        },
        { tag: HISTORY_MERGE_TAG },
      );
    };

    if (!enabled) {
      // Unwrap any pages left over from a previous paged session.
      const root = editor.getRootElement();
      if (root) {
        for (const prop of PAGE_PROPS) root.style.removeProperty(prop);
        root.style.zoom = "";
        root.style.paddingBottom = "";
      }
      destroyPageStructure();
      return;
    }

    let rafId: number | null = null;
    let previousPageKey: NodeKey | null = null;
    // `ResizeObserver.observe()` can emit an initial callback even without a
    // real size change. If we reflow on that synthetic pulse, a click can
    // unexpectedly move blocks and yank the caret to the top.
    const skipInitialResizeForPage = new Set<NodeKey>();
    const fixedPageHeights = new Map<NodeKey, number>();
    const pagesMarkedForMeasurement = new Set<NodeKey>();

    const clearMeasurementFlags = () => {
      fixedPageHeights.clear();
      pagesMarkedForMeasurement.clear();
    };
    const clearFixedHeight = (node: PageNode) =>
      fixedPageHeights.delete(node.getKey());
    const clearMeasurementFlag = (node: PageNode) =>
      pagesMarkedForMeasurement.delete(node.getKey());
    const getFixedHeight = (node: PageNode) =>
      fixedPageHeights.get(node.getKey());
    const isMarkedForMeasurement = (node: PageNode) =>
      pagesMarkedForMeasurement.has(node.getKey());
    const markForMeasurement = (node: PageNode) =>
      pagesMarkedForMeasurement.add(node.getKey());
    const setFixedHeight = (node: PageNode, height: number) =>
      fixedPageHeights.set(node.getKey(), height);

    const $getPagesMarkedForMeasurement = () => {
      const pages: PageNode[] = [];
      for (const key of pagesMarkedForMeasurement) {
        const page = $getNodeByKey(key);
        if ($isPageNode(page) && page.isAttached()) pages.push(page);
      }
      return pages;
    };

    const withNaturalPageHeight = <T,>(
      pageElement: HTMLElement,
      measure: () => T,
    ): T => {
      const prevHeight = pageElement.style.height;
      const prevMinHeight = pageElement.style.minHeight;
      pageElement.style.height = "auto";
      pageElement.style.minHeight = "unset";
      try {
        return measure();
      } finally {
        pageElement.style.height = prevHeight || PAGE_FIXED_HEIGHT;
        pageElement.style.minHeight = prevMinHeight || PAGE_FIXED_HEIGHT;
      }
    };

    // Measures the natural content height by temporarily removing fixed height.
    const measureHeight = (node: PageNode) => {
      clearMeasurementFlag(node);
      const element = editor.getElementByKey(node.getKey());
      if (!element) return 0;
      return withNaturalPageHeight(element, () => element.scrollHeight);
    };

    const getPageHeight = () => {
      const rootElement = editor.getRootElement();
      if (!rootElement) return 0;
      return parseInt(rootElement.style.getPropertyValue("--page-height"), 10);
    };

    // Which children from the next page fit back into this page.
    const $getUnderflowingChildren = (node: PageNode) => {
      const rootElement = editor.getRootElement();
      if (!rootElement) return [];
      const pageElement = node.getPageElement();
      if (!pageElement) return [];
      const contentElement = node.getPageContentElement();
      if (!contentElement) return [];
      const nextPage = node.getNextSibling();
      if (!$isPageNode(nextPage)) return [];
      const nextPageContentNode = nextPage.getContentNode();
      if (!nextPageContentNode) return [];
      const nextPageContentChildren = nextPageContentNode.getChildren();
      if (!nextPageContentChildren.length) return [];
      const nextPageContentElement = nextPage.getPageContentElement();
      if (!nextPageContentElement) return [];
      const pageHeight = getPageHeight();
      if (!pageHeight) return [];
      const nextPageChildNodes = Array.from(nextPageContentElement.childNodes);
      const appendedClones: ChildNode[] = [];
      let overflowAfterIndex = 0;
      withNaturalPageHeight(pageElement, () => {
        let currentPageHeight = pageElement.scrollHeight;
        while (currentPageHeight < pageHeight - REFLOW_HYSTERESIS_PX) {
          const nextChild = nextPageChildNodes[overflowAfterIndex]?.cloneNode(
            true,
          ) as ChildNode | undefined;
          if (!nextChild) break;
          contentElement.appendChild(nextChild);
          appendedClones.push(nextChild);
          currentPageHeight = pageElement.scrollHeight;
          if (currentPageHeight > pageHeight - REFLOW_HYSTERESIS_PX) break;
          overflowAfterIndex++;
          setFixedHeight(node, currentPageHeight);
        }
      });
      // Keep measurement artifacts out of the live DOM so caret mapping stays
      // stable when the user clicks after a delete/reflow.
      for (const clone of appendedClones) {
        clone.remove();
      }
      if (overflowAfterIndex === 0) return [];
      return nextPageContentChildren.slice(0, overflowAfterIndex);
    };

    // Which children overflow beyond the page height.
    const $getOverflowingChildren = (
      node: PageNode,
    ): ReturnType<PageContentNode["getChildren"]> => {
      const rootElement = editor.getRootElement();
      if (!rootElement) return [];
      const pageElement = node.getPageElement();
      if (!pageElement) return [];
      const contentElement = node.getPageContentElement();
      const contentNode = node.getContentNode();
      if (!contentElement || !contentNode) return [];
      const children = contentNode.getChildren();
      const childNodes = Array.from(contentElement.childNodes);
      const pageHeight = getPageHeight();
      if (!pageHeight) return [];
      if (children.length !== childNodes.length) {
        const reconcilable = contentNode as ReconcilableContentNode;
        // Guard: without a reconcile method, recursing would loop forever
        // because the DOM/Lexical child counts would never converge here.
        if (typeof reconcilable.reconcileObservedMutation !== "function") {
          return [];
        }
        childNodes.forEach((childNode) => (childNode as ChildNode).remove());
        reconcilable.reconcileObservedMutation(contentElement, editor);
        return $getOverflowingChildren(node);
      }
      let overflowAfterIndex = children.length - 1;
      withNaturalPageHeight(pageElement, () => {
        let currentPageHeight = pageElement.scrollHeight;
        while (currentPageHeight > pageHeight + REFLOW_HYSTERESIS_PX) {
          const lastChild = childNodes[overflowAfterIndex];
          if (lastChild) (lastChild as ChildNode).remove();
          currentPageHeight = pageElement.scrollHeight;
          setFixedHeight(node, currentPageHeight);
          if (currentPageHeight < pageHeight + REFLOW_HYSTERESIS_PX) break;
          overflowAfterIndex--;
        }
      });
      return children.slice(overflowAfterIndex || 1);
    };

    // Moves overflowing children onto the next page (creating it if needed).
    const $fixOverflow = (node: PageNode) => {
      const contentNode = node.getContentNode();
      const childrenSize = contentNode.getChildrenSize();
      if (childrenSize === 1) return;
      const overflowingChildren = $getOverflowingChildren(node);
      if (!overflowingChildren.length) return;
      const nextSibling = node.getNextSibling();
      if ($isPageNode(nextSibling)) {
        const nextContent = nextSibling.getContentNode();
        const nextPageFirstChild = nextContent.getFirstChild();
        if (!nextPageFirstChild) return;
        overflowingChildren.forEach((child) => {
          nextPageFirstChild.insertBefore(child);
        });
      } else {
        const newPage = $createPageNode();
        newPage.getContentNode().append(...overflowingChildren);
        node.insertAfter(newPage);
      }
    };

    // Pulls children back from the next page to fill remaining space.
    const $fixUnderflow = (node: PageNode) => {
      const contentNode = node.getContentNode();
      const childrenSize = contentNode.getChildrenSize();
      if (!childrenSize) return node.remove();
      const nextSibling = node.getNextSibling();
      if (!$isPageNode(nextSibling)) return;
      const nextContent = nextSibling.getContentNode();
      const nextPageChildrenSize = nextContent.getChildrenSize();
      if (nextPageChildrenSize === 0) return;
      const underflowingChildren = $getUnderflowingChildren(node);
      if (!underflowingChildren.length) return;
      contentNode.append(...underflowingChildren);

      // Safety net against oscillation: if the real DOM height overflows after
      // the move (e.g. due clone-vs-real measurement drift), rollback this pull
      // immediately instead of waiting for the next cycle to push it back.
      const pageElement = node.getPageElement();
      const pageHeight = getPageHeight();
      if (
        pageElement &&
        pageHeight > 0 &&
        pageElement.scrollHeight > pageHeight + REFLOW_HYSTERESIS_PX
      ) {
        const nextFirst = nextContent.getFirstChild();
        if (nextFirst) {
          for (let i = underflowingChildren.length - 1; i >= 0; i--) {
            const child = underflowingChildren[i];
            if (child) nextFirst.insertBefore(child);
          }
        } else {
          nextContent.append(...underflowingChildren);
        }
        return;
      }

      if (nextPageChildrenSize !== underflowingChildren.length) return;
      nextSibling.remove();
    };

    const $fixFlow = (node: PageNode) => {
      if (!node.isAttached()) return clearMeasurementFlag(node);
      const pageHeight = getPageHeight();
      const fixedPageHeight = getFixedHeight(node);
      const currentPageHeight = measureHeight(node);
      if (currentPageHeight === fixedPageHeight) return;
      if (!pageHeight || !currentPageHeight) return;
      if (currentPageHeight === 0) return;
      if (currentPageHeight > pageHeight + REFLOW_HYSTERESIS_PX) {
        $fixOverflow(node);
      } else if (currentPageHeight < pageHeight - REFLOW_HYSTERESIS_PX) {
        $fixUnderflow(node);
      } else {
        // Inside hysteresis band, consider layout stable for this page.
        setFixedHeight(node, currentPageHeight);
      }
    };

    const schedulePageMeasurement = () => {
      if (rafId) cancelAnimationFrame(rafId);
      rafId = requestAnimationFrame(() => {
        editor.update(
          () => {
            const pages = $getPagesMarkedForMeasurement();
            if (pages.length === 0) return;
            $addUpdateTag(SKIP_SCROLL_INTO_VIEW_TAG);
            $addUpdateTag(HISTORY_MERGE_TAG);
            for (const page of pages) $fixFlow(page);
          },
          // Commit synchronously so this deferred reflow never leaves a
          // non-committed pending editor state around. A lingering pending
          // state makes the next keydown skip re-cloning the selection,
          // which surfaces as the frozen-point `"key" is read-only` crash.
          { discrete: true },
        );
      });
    };

    // Scales pages down so they fit the available root width (capped at 1x).
    const updateZoom = () => {
      const rootElement = editor.getRootElement();
      if (!rootElement) return;
      const pageWidth = parseInt(
        rootElement.style.getPropertyValue("--page-width"),
        10,
      );
      if (!pageWidth) return;
      const prevZoom = rootElement.style.zoom || "1";
      const rootWidth = rootElement.getBoundingClientRect().width;
      const rootPadding =
        parseFloat(getComputedStyle(rootElement).paddingLeft) * 2;
      const nextZoom = Math.min(
        rootWidth / (pageWidth + rootPadding),
        1,
      ).toFixed(6);
      if (nextZoom === prevZoom) return;
      rootElement.style.zoom = nextZoom;
    };

    // Wraps every root-level child into PageNode groups.
    const fixPageStructure = () => {
      editor.update(
        () => {
          const root = $getRoot();
          const children = root.getChildren();
          const pages: PageNode[] = [];
          for (const child of children) {
            if ($isPageNode(child)) {
              pages.push(child);
            } else {
              const lastPage = pages[pages.length - 1];
              if ($isPageNode(lastPage)) {
                lastPage.getContentNode().append(child);
              } else {
                const newPage = $createPageNode();
                newPage.getContentNode().append(child);
                pages.push(newPage);
              }
            }
          }
          root.splice(0, root.getChildrenSize(), pages);
          for (const page of pages) markForMeasurement(page);
          if (pages.length === 0) {
            const newPage = $createPageNode();
            const paragraph = $createParagraphNode();
            newPage.getContentNode().append(paragraph);
            root.append(newPage);
            paragraph.selectStart();
            markForMeasurement(newPage);
          }
          schedulePageMeasurement();
        },
        { discrete: true },
      );
    };

    // Marks every page for measurement (used after page dimensions change).
    const resizePages = () => {
      editor.read(() => {
        const root = $getRoot();
        clearMeasurementFlags();
        for (const child of root.getChildren()) {
          if ($isPageNode(child)) markForMeasurement(child);
        }
        schedulePageMeasurement();
      });
    };
    resizeRef.current = resizePages;

    // Validates the root: must be all PageNodes with no stray content.
    const $enforcePageStructure = () => {
      if (!isEnabled()) return;
      const root = $getRoot();
      const children = root.getChildren();
      const isInvalid =
        !children.some($isPageNode) ||
        children.some((child) => !$isPageNode(child));
      if (isInvalid) queueMicrotask(fixPageStructure);
    };

    // Normalizes a PageNode to exactly one PageContentNode child.
    const $ensurePageNodeChildren = (pageNode: PageNode) => {
      const children = pageNode.getChildren();
      let content: PageContentNode | undefined;
      const strayChildren: typeof children = [];
      for (const child of children) {
        if ($isPageContentNode(child)) content = child;
        else strayChildren.push(child);
      }
      if (content && strayChildren.length === 0) return;
      if (!content) content = $createPageContentNode();
      if (strayChildren.length > 0) content.append(...strayChildren);
      else content.append($createParagraphNode());
      pageNode.clear();
      pageNode.append(content);
    };

    let rootObserver: ResizeObserver | null = null;
    let pageObserver: ResizeObserver | null = null;

    const attachObservers = (rootElement: HTMLElement) => {
      rootObserver = new ResizeObserver(updateZoom);
      pageObserver = new ResizeObserver((entries) => {
        const pageContent = entries[0]?.target as HTMLElement | undefined;
        if (!pageContent) return;
        editor.read(() => {
          const pageContentNode = $getNearestNodeFromDOMNode(pageContent);
          if (!$isPageContentNode(pageContentNode)) return;
          const pageNode = pageContentNode.getParent();
          if (!$isPageNode(pageNode)) return;
          const pageKey = pageNode.getKey();
          if (skipInitialResizeForPage.has(pageKey)) {
            skipInitialResizeForPage.delete(pageKey);
            return;
          }
          const previousPage = pageNode.getPreviousPage();
          if (previousPage) {
            clearFixedHeight(previousPage);
            markForMeasurement(previousPage);
          }
          markForMeasurement(pageNode);
          schedulePageMeasurement();
        });
      });
      rootObserver.observe(rootElement);
    };

    const unregisterRoot = editor.registerRootListener(
      (rootElement, prevRootElement) => {
        if (prevRootElement) {
          rootObserver?.disconnect();
          pageObserver?.disconnect();
          rootObserver = null;
          pageObserver = null;
        }
        if (rootElement) attachObservers(rootElement);
      },
    );

    const removePageTransform = editor.registerNodeTransform(
      PageNode,
      (pageNode) => {
        $ensurePageNodeChildren(pageNode);
        if (isMarkedForMeasurement(pageNode)) return;
        markForMeasurement(pageNode);
        schedulePageMeasurement();
      },
    );

    const removeRootTransform = editor.registerNodeTransform(
      RootNode,
      $enforcePageStructure,
    );

    const removePageContentTransform = editor.registerNodeTransform(
      PageContentNode,
      (node) => {
        const pageNode = node.getPageNode();
        if (isMarkedForMeasurement(pageNode)) return;
        markForMeasurement(pageNode);
        schedulePageMeasurement();
      },
    );

    const removeCommandListeners = mergeRegister(
      // Track the focused page so the pageObserver watches only its content.
      editor.registerCommand(
        SELECTION_CHANGE_COMMAND,
        () => {
          if (!isEnabled()) return false;
          const selection = $getSelection();
          if (!$isRangeSelection(selection)) return false;
          const anchorNode = selection.anchor.getNode();
          const nearestRoot =
            anchorNode.getKey() === "root"
              ? anchorNode
              : $getNearestRootOrShadowRoot(anchorNode);
          if (!$isPageContentNode(nearestRoot)) return false;
          const currentPage = nearestRoot.getPageNode();
          const currentPageKey = currentPage.getKey();
          const oldPreviousPageKey = previousPageKey;
          const pageContentElement = currentPage.getPageContentElement();
          if (!pageContentElement) return false;
          previousPageKey = currentPageKey;
          skipInitialResizeForPage.add(currentPageKey);
          pageObserver?.observe(pageContentElement);
          if (
            oldPreviousPageKey === null ||
            oldPreviousPageKey === currentPageKey
          ) {
            return false;
          }
          const previousPage = $getNodeByKey(oldPreviousPageKey);
          if (!$isPageNode(previousPage)) return false;
          const previousPageContent = previousPage.getPageContentElement();
          if (!previousPageContent) return false;
          skipInitialResizeForPage.delete(oldPreviousPageKey);
          pageObserver?.unobserve(previousPageContent);
          return false;
        },
        COMMAND_PRIORITY_LOW,
      ),
    );

    // Copy page CSS custom properties to :root for @page print rules.
    const handleBeforePrint = () => {
      const rootElement = editor.getRootElement();
      if (!rootElement) return;
      for (const prop of PAGE_PROPS) {
        const val = rootElement.style.getPropertyValue(prop);
        if (val) document.documentElement.style.setProperty(prop, val);
      }
    };
    const handleAfterPrint = () => {
      for (const prop of PAGE_PROPS) {
        document.documentElement.style.removeProperty(prop);
      }
    };
    window.addEventListener("beforeprint", handleBeforePrint);
    window.addEventListener("afterprint", handleAfterPrint);

    // Bootstrap: wrap existing content and run an initial reflow.
    queueMicrotask(() => {
      if (enabledRef.current) fixPageStructure();
    });

    return () => {
      unregisterRoot();
      removePageTransform();
      removeRootTransform();
      removePageContentTransform();
      removeCommandListeners();
      rootObserver?.disconnect();
      pageObserver?.disconnect();
      if (rafId !== null) cancelAnimationFrame(rafId);
      clearMeasurementFlags();
      skipInitialResizeForPage.clear();
      resizeRef.current = null;
      window.removeEventListener("beforeprint", handleBeforePrint);
      window.removeEventListener("afterprint", handleAfterPrint);
    };
    // Re-register only when the surface toggles paged mode on/off.
  }, [editor, enabled]);

  return null;
}
