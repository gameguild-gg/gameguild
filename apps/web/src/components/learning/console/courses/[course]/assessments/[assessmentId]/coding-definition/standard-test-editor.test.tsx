import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { StandardTest } from "@/lib/coding-assignment/client";
import { StandardTestEditor } from "./standard-test-editor";

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

describe("StandardTestEditor", () => {
  it("renders defaults and dispatches every editable field", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const onVisibilityChange = vi.fn();
    const onRemove = vi.fn();
    const test = {
      kind: "standard",
      Name: null,
      Stdout: "",
    } as StandardTest;
    render(
      <StandardTestEditor
        index={3}
        test={test}
        visibility="Public"
        errors={[
          { field: "tests[3].Weight", code: "weight", message: "Weight is invalid" },
          { field: "tests[3].Stdout", code: "stdout", message: "Stdout is required" },
        ]}
        onChange={onChange}
        onVisibilityChange={onVisibilityChange}
        onRemove={onRemove}
      />,
    );

    expect(screen.getByTestId("standard-name-3")).toHaveValue("");
    expect(screen.getByTestId("standard-weight-3")).toHaveValue(1);
    expect(screen.getByTestId("standard-exitCode-3")).toHaveValue(null);
    expect(screen.getByTestId("standard-stdin-3")).toHaveValue("");
    expect(screen.getByTestId("standard-stderr-3")).toHaveValue("");
    expect(screen.getByText("Weight is invalid")).toBeInTheDocument();
    expect(screen.getByText("Stdout is required")).toBeInTheDocument();

    fireEvent.change(screen.getByTestId("standard-name-3"), {
      target: { value: "Echo" },
    });
    fireEvent.change(screen.getByTestId("standard-weight-3"), {
      target: { value: "2.5" },
    });
    fireEvent.change(screen.getByTestId("standard-exitCode-3"), {
      target: { value: "2" },
    });
    fireEvent.change(screen.getByTestId("standard-stdin-3"), {
      target: { value: "input" },
    });
    fireEvent.change(screen.getByTestId("standard-stdout-3"), {
      target: { value: "output" },
    });
    fireEvent.change(screen.getByTestId("standard-stderr-3"), {
      target: { value: "warning" },
    });
    await user.click(screen.getByTestId("standard-visibility-3"));
    await user.click(await screen.findByRole("option", { name: "Private" }));
    await user.click(screen.getByTestId("standard-remove-3"));

    expect(onChange).toHaveBeenCalledWith(3, { Name: "Echo" });
    expect(onChange).toHaveBeenCalledWith(3, { Weight: 2.5 });
    expect(onChange).toHaveBeenCalledWith(3, { ExitCode: 2 });
    expect(onChange).toHaveBeenCalledWith(3, { Stdin: "input" });
    expect(onChange).toHaveBeenCalledWith(3, { Stdout: "output" });
    expect(onChange).toHaveBeenCalledWith(3, { Stderr: "warning" });
    expect(onVisibilityChange).toHaveBeenCalledWith(3, "Private");
    expect(onRemove).toHaveBeenCalledWith(3);
  });

  it("renders persisted optional values without validation messages", () => {
    const onChange = vi.fn();
    const test: StandardTest = {
      kind: "standard",
      Name: "Persisted",
      Weight: 4,
      ExitCode: 1,
      Stdin: "stdin",
      Stdout: "stdout",
      Stderr: "stderr",
    };
    render(
      <StandardTestEditor
        index={0}
        test={test}
        visibility="Private"
        errors={[]}
        onChange={onChange}
        onVisibilityChange={vi.fn()}
        onRemove={vi.fn()}
      />,
    );

    expect(screen.getByTestId("standard-name-0")).toHaveValue("Persisted");
    expect(screen.getByTestId("standard-weight-0")).toHaveValue(4);
    expect(screen.getByTestId("standard-exitCode-0")).toHaveValue(1);
    expect(screen.getByTestId("standard-stdin-0")).toHaveValue("stdin");
    expect(screen.getByTestId("standard-stderr-0")).toHaveValue("stderr");
    expect(screen.queryByText("Weight is invalid")).not.toBeInTheDocument();

    fireEvent.change(screen.getByTestId("standard-exitCode-0"), {
      target: { value: "" },
    });
    expect(onChange).toHaveBeenCalledWith(0, { ExitCode: null });
  });
});
