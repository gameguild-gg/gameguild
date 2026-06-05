"use client"

import { ViewerProvider } from "@/components/block-content-editor/engines/viewer-provider"
import { ViewerToolbar } from "@/components/block-content-editor/engines/viewer-toolbar"
import { ViewerField } from "@/components/block-content-editor/engines/viewer-field"
import { ViewerDialogs } from "@/components/block-content-editor/engines/viewer-dialogs"
import { ViewerLayout } from "./viewer-layout"

export default function PreviewPage() {
  return (
    <ViewerProvider>
      <ViewerLayout header={<ViewerToolbar />}>
        <ViewerField />
      </ViewerLayout>
      <ViewerDialogs />
    </ViewerProvider>
  )
}
