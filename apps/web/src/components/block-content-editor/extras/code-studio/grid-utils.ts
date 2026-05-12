import type { AspectRatio } from "./types"

// Helper to get grid dimensions from aspect ratio
export function getGridDimensions(aspectRatio: AspectRatio) {
  switch (aspectRatio) {
    case "2:1": return { cols: 24, rows: 12 } // Landscape
    case "1:1": return { cols: 12, rows: 12 } // Square
    case "1:2": return { cols: 12, rows: 24 } // Portrait
  }
}

// Helper to get container dimensions from aspect ratio
export function getContainerDimensions(aspectRatio: AspectRatio) {
  switch (aspectRatio) {
    case "2:1": return { maxWidth: "1200px", maxHeight: "600px" } // Landscape 2:1
    case "1:1": return { maxWidth: "600px", maxHeight: "600px" } // Square 1:1
    case "1:2": return { maxWidth: "600px", maxHeight: "1200px" } // Portrait 1:2
  }
}
