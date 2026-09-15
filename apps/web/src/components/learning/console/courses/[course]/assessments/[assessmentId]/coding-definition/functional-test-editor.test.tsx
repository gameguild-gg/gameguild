import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { FunctionalTestGroup } from "@/lib/coding-assignment/types";
import {
  defaultFunctionalContentForType,
  FunctionalTestEditor,
  makeFunctionParameter,
  parseFunctionalContent,
} from "./functional-test-editor";

Object.defineProperties(HTMLElement.prototype, {
  hasPointerCapture: { value: vi.fn(() => false) },
  setPointerCapture: { value: vi.fn() },
  releasePointerCapture: { value: vi.fn() },
  scrollIntoView: { value: vi.fn() },
});

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

const emptyTest: FunctionalTestGroup = {
  kind: "functional",
  Name: null,
  Function: {
    FunctionName: "",
    Parameters: [],
    ReturnType: { Type: "string" },
  },
  Cases: [],
};

const populatedTest: FunctionalTestGroup = {
  kind: "functional",
  Name: "Conversion cases",
  Weight: 2,
  Function: {
    FunctionName: "convert",
    Parameters: [
      { Name: "", Type: "string" },
      { Name: "count", Type: "integer" },
    ],
    ReturnType: { Type: "integer" },
  },
  Cases: [
    {
      Inputs: [
        { Type: "string", Content: "hello" },
        { Type: "integer", Content: 2 },
      ],
      Expected: { Type: "integer", Content: 5 },
    },
    {
      Inputs: [{ Type: "string", Content: "legacy" }],
      Expected: { Type: "integer", Content: 6 },
    },
  ],
};

describe("FunctionalTestEditor", () => {
  it("renders empty defaults and dispatches group-level edits", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const onVisibilityChange = vi.fn();
    const onRemove = vi.fn();
    const { rerender } = render(
      <FunctionalTestEditor
        index={2}
        test={emptyTest}
        visibility="Public"
        errors={[
          { field: "Tests.Public[2].Weight", code: "weight", message: "Weight is invalid" },
          { field: "Tests.Public[2].FunctionName", code: "name", message: "Function name is required" },
        ]}
        onChange={onChange}
        onVisibilityChange={onVisibilityChange}
        onRemove={onRemove}
      />,
    );

    expect(screen.getByTestId("functional-name-2")).toHaveValue("");
    expect(screen.getByTestId("functional-weight-2")).toHaveValue(1);
    expect(screen.getByTestId("functional-no-params-2")).toBeInTheDocument();
    expect(screen.getByTestId("functional-no-cases-2")).toBeInTheDocument();
    expect(screen.getByText("Weight is invalid")).toBeInTheDocument();
    expect(screen.getByText("Function name is required")).toBeInTheDocument();

    fireEvent.change(screen.getByTestId("functional-name-2"), { target: { value: "Edge cases" } });
    fireEvent.change(screen.getByTestId("functional-weight-2"), { target: { value: "3.5" } });
    fireEvent.change(screen.getByTestId("functional-functionName-2"), { target: { value: "solve" } });
    await user.click(screen.getByTestId("functional-visibility-2"));
    await user.click(await screen.findByRole("option", { name: "Private" }));
    await user.click(screen.getByTestId("functional-remove-2"));
    await user.click(screen.getByTestId("functional-add-param-2"));
    await user.click(screen.getByTestId("functional-add-case-2"));

    expect(onChange).toHaveBeenCalledWith(2, { Name: "Edge cases" });
    expect(onChange).toHaveBeenCalledWith(2, { Weight: 3.5 });
    expect(onChange).toHaveBeenCalledWith(2, {
      Function: { ...emptyTest.Function, FunctionName: "solve" },
    });
    expect(onChange).toHaveBeenCalledWith(2, {
      Function: {
        ...emptyTest.Function,
        Parameters: [{ Name: "", Type: "string" }],
      },
    });
    expect(onChange).toHaveBeenCalledWith(2, {
      Cases: [{ Inputs: [], Expected: { Type: "string", Content: "" } }],
    });
    expect(onVisibilityChange).toHaveBeenCalledWith(2, "Private");
    expect(onRemove).toHaveBeenCalledWith(2);

    rerender(
      <FunctionalTestEditor
        index={2}
        test={{
          ...emptyTest,
          Cases: [{ Inputs: [], Expected: { Type: "string", Content: "" } }],
        }}
        visibility="Public"
        errors={[]}
        onChange={onChange}
        onVisibilityChange={onVisibilityChange}
        onRemove={onRemove}
      />,
    );
    expect(screen.getByText("No parameters in signature.")).toBeInTheDocument();
  });

  it("edits signature parameters, return type, cases, and missing legacy inputs", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <FunctionalTestEditor
        index={0}
        test={populatedTest}
        visibility="Private"
        errors={[]}
        onChange={onChange}
        onVisibilityChange={vi.fn()}
        onRemove={vi.fn()}
      />,
    );

    expect(screen.getAllByText("param 1 (string)")).toHaveLength(2);
    expect(screen.getAllByText("count (integer)")).toHaveLength(2);
    expect(screen.getByTestId("functional-weight-0")).toHaveValue(2);

    fireEvent.change(screen.getByTestId("functional-param-name-0-1"), {
      target: { value: "amount" },
    });
    await user.click(screen.getByTestId("functional-param-type-0-1"));
    await user.click(await screen.findByRole("option", { name: "Boolean" }));
    await user.click(screen.getByTestId("functional-returnType-0"));
    await user.click(await screen.findByRole("option", { name: "Float" }));
    await user.click(screen.getByTestId("functional-param-remove-0-0"));

    fireEvent.change(screen.getByTestId("functional-case-input-value-0-0-0"), {
      target: { value: "changed" },
    });
    fireEvent.change(screen.getByTestId("functional-case-input-value-0-0-1"), {
      target: { value: "not-a-number" },
    });
    fireEvent.change(screen.getByTestId("functional-case-input-value-0-1-1"), {
      target: { value: "7" },
    });
    fireEvent.change(screen.getByTestId("functional-case-expected-value-0-0"), {
      target: { value: "invalid" },
    });
    await user.click(screen.getByTestId("functional-case-remove-0-1"));

    expect(onChange).toHaveBeenCalledWith(0, {
      Function: {
        ...populatedTest.Function,
        Parameters: [populatedTest.Function.Parameters[0], { Name: "amount", Type: "integer" }],
      },
    });
    expect(onChange).toHaveBeenCalledWith(0, {
      Function: {
        ...populatedTest.Function,
        Parameters: [populatedTest.Function.Parameters[0], { Name: "count", Type: "boolean" }],
      },
    });
    expect(onChange).toHaveBeenCalledWith(0, {
      Function: { ...populatedTest.Function, ReturnType: { Type: "float" } },
    });
    expect(onChange).toHaveBeenCalledWith(0, {
      Function: {
        ...populatedTest.Function,
        Parameters: [populatedTest.Function.Parameters[1]],
      },
    });
    expect(onChange).toHaveBeenCalledWith(0, {
      Cases: [
        populatedTest.Cases[0],
        {
          ...populatedTest.Cases[1],
          Inputs: [
            populatedTest.Cases[1]!.Inputs[0],
            { Type: "integer", Content: 7 },
          ],
        },
      ],
    });
    expect(onChange).toHaveBeenCalledWith(0, {
      Cases: [populatedTest.Cases[0]],
    });
  });

  it("coerces all supported wire values and builds typed defaults", () => {
    expect(parseFunctionalContent("string", "text")).toBe("text");
    expect(parseFunctionalContent("integer", "4")).toBe(4);
    expect(parseFunctionalContent("integer", "bad")).toBe(0);
    expect(parseFunctionalContent("float", "4.5")).toBe(4.5);
    expect(parseFunctionalContent("float", "bad")).toBe(0);
    expect(parseFunctionalContent("boolean", "true")).toBe(true);
    expect(parseFunctionalContent("boolean", "1")).toBe(true);
    expect(parseFunctionalContent("boolean", "false")).toBe(false);

    expect(defaultFunctionalContentForType("integer", 2)).toBe(2);
    expect(defaultFunctionalContentForType("integer", "2")).toBe(0);
    expect(defaultFunctionalContentForType("float", 2.5)).toBe(2.5);
    expect(defaultFunctionalContentForType("float", false)).toBe(0);
    expect(defaultFunctionalContentForType("boolean", true)).toBe(true);
    expect(defaultFunctionalContentForType("boolean", "true")).toBe(false);
    expect(defaultFunctionalContentForType("string", "kept")).toBe("kept");
    expect(defaultFunctionalContentForType("string", 1)).toBe("");
    expect(makeFunctionParameter("float", 1.5)).toEqual({ Type: "float", Content: 1.5 });
  });
});
