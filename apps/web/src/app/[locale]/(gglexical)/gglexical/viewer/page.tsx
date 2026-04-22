"use client"

import { ViewerProvider } from "@/components/editor/engines/viewer-provider"
import { ViewerToolbar } from "@/components/editor/engines/viewer-toolbar"
import { ViewerField } from "@/components/editor/engines/viewer-field"
import { ViewerDialogs } from "@/components/editor/engines/viewer-dialogs"
import { ViewerLayout } from "./viewer-layout"

export default function PreviewPage() {
  return (
    <ViewerProvider>
      <ViewerLayout>
        <ViewerToolbar />
        <ViewerField />
      </ViewerLayout>
      <ViewerDialogs />
    </ViewerProvider>
  )
}
