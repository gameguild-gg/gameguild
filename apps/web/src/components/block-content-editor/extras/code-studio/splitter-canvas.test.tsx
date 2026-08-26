import { render } from "@testing-library/react"
import { describe, expect, it, vi } from "vitest"

const panelMocks = vi.hoisted(() => ({
  group: vi.fn(),
}))

vi.mock("react-resizable-panels", () => ({
  Group: ({ children, ...props }: { children?: React.ReactNode }) => {
    panelMocks.group(props)
    return <div data-testid="v4-group">{children}</div>
  },
  Panel: ({ children }: { children?: React.ReactNode }) => <section>{children}</section>,
  Separator: () => <div role="separator" />,
  PanelGroup: ({ children }: { children?: React.ReactNode }) => <div data-testid="legacy-group">{children}</div>,
  PanelResizeHandle: () => <div role="separator" />,
}))

import { SplitterCanvas } from "./splitter-canvas"

describe("SplitterCanvas", () => {
  it("uses the v4 layout callback and preserves the panel order", () => {
    const onSplitResize = vi.fn()

    render(
      <SplitterCanvas
        editable={false}
        root={{
          kind: "split",
          id: "root",
          direction: "horizontal",
          sizes: [40, 60],
          children: [
            { kind: "leaf", id: "editor", type: "full-editor" },
            { kind: "leaf", id: "output", type: "output" },
          ],
        }}
        renderLeaf={(leaf) => <div>{leaf.id}</div>}
        onSplitResize={onSplitResize}
      />,
    )

    expect(panelMocks.group).toHaveBeenCalledTimes(1)

    const groupProps = panelMocks.group.mock.calls[0]?.[0] as {
      onLayoutChange: (layout: Record<string, number>) => void
      orientation: "horizontal" | "vertical"
    }
    expect(groupProps.orientation).toBe("horizontal")

    groupProps.onLayoutChange({ "leaf-editor": 35, "leaf-output": 65 })
    expect(onSplitResize).toHaveBeenCalledWith("root", [35, 65])
  })
})
