import { $createCodeNode, CodeNode } from "@lexical/code";
import { ListItemNode, ListNode } from "@lexical/list";
import { HeadingNode } from "@lexical/rich-text";
import { $patchStyleText } from "@lexical/selection";
import {
  $createParagraphNode,
  $createTextNode,
  $getRoot,
  $isRangeSelection,
  createEditor,
} from "lexical";
import { describe, expect, it } from "vitest";
import {
  $readBlockFormatState,
  $readTextFormatState,
  upsertCssProperty,
} from "./format-state";

function createFormattingEditor() {
  return createEditor({
    nodes: [CodeNode, HeadingNode, ListItemNode, ListNode],
  });
}

describe("shared formatting state", () => {
  it("reads inline styles and text formats from a range selection", () => {
    const editor = createFormattingEditor();

    editor.update(
      () => {
        const paragraph = $createParagraphNode();
        const text = $createTextNode("formatted text");
        paragraph.append(text);
        paragraph.setFormat("center");
        $getRoot().append(paragraph);

        const selection = text.select(0, text.getTextContentSize());
        expect($isRangeSelection(selection)).toBe(true);
        selection.formatText("bold");
        selection.formatText("underline");
        $patchStyleText(selection, {
          "background-color": "#abcdef",
          color: "#123456",
          "font-family": "Verdana",
          "font-size": "24px",
        });

        expect($readTextFormatState(selection)).toMatchObject({
          bgColor: "#abcdef",
          fontColor: "#123456",
          fontFamily: "Verdana",
          fontSize: "24px",
          isBold: true,
          isItalic: false,
          isUnderline: true,
        });
        expect($readBlockFormatState(text)).toEqual({
          blockType: "paragraph",
          elementFormat: "center",
        });
      },
      { discrete: true },
    );
  });

  it("reads effective font styles from an enclosing code node", () => {
    const editor = createFormattingEditor();

    editor.update(
      () => {
        const code = $createCodeNode();
        const text = $createTextNode("const value = 1");
        code.setStyle("color: red; font-size: 30px; font-family: Consolas");
        code.append(text);
        $getRoot().append(code);

        const selection = text.select(0, text.getTextContentSize());
        expect($readTextFormatState(selection)).toMatchObject({
          fontFamily: "Consolas",
          fontSize: "30px",
        });
        expect($readBlockFormatState(text).blockType).toBe("code");
      },
      { discrete: true },
    );
  });

  it("replaces one CSS property without discarding the others", () => {
    expect(
      upsertCssProperty(
        "color: red; font-size: 16px; background-color: white",
        "font-size",
        "20px",
      ),
    ).toBe("color: red; background-color: white; font-size: 20px");
  });
});
