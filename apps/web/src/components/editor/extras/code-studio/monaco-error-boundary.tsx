"use client"

import { Component, type ReactNode } from "react"

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
  retryKey: number
}

/**
 * Error boundary that catches Monaco "InstantiationService has been disposed"
 * errors and auto-recovers by remounting the child editor.
 *
 * This handles a known race condition in @monaco-editor/react where
 * editor.setModel() fires after the editor instance was disposed
 * (e.g. React StrictMode double-mount or fast unmount/remount cycles).
 */
export class MonacoErrorBoundary extends Component<Props, State> {
  static MAX_RETRIES = 2
  state: State = { hasError: false, retryKey: 0 }

  static getDerivedStateFromError(error: Error) {
    if (
      error.message?.includes("InstantiationService has been disposed") ||
      error.message?.includes("worker is shutting down") ||
      (error.message?.includes("Theme") && error.message?.includes("not found"))
    ) {
      return { hasError: true }
    }
    // Re-throw non-Monaco errors
    throw error
  }

  componentDidUpdate(_: Props, prevState: State) {
    if (this.state.hasError && !prevState.hasError) {
      if (this.state.retryKey < MonacoErrorBoundary.MAX_RETRIES) {
        // Schedule a remount on the next frame
        requestAnimationFrame(() => {
          this.setState((s) => ({ hasError: false, retryKey: s.retryKey + 1 }))
        })
      }
    }
  }

  render() {
    if (this.state.hasError) {
      if (this.state.retryKey >= MonacoErrorBoundary.MAX_RETRIES) {
        return <div style={{ width: "100%", height: "100%", display: "flex", alignItems: "center", justifyContent: "center", color: "#888", fontSize: 13 }}>Editor failed to load</div>
      }
      return null // Briefly empty while remounting
    }
    return <div key={this.state.retryKey} style={{ width: "100%", height: "100%" }}>{this.props.children}</div>
  }
}
