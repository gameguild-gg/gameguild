import { EditorProvider } from "@/components/block-content-editor/engines/editor-provider"
import { EditorToolbar } from "@/components/block-content-editor/engines/editor-toolbar"
import { EditorField } from "@/components/block-content-editor/engines/editor-field"
import { EditorDialogs } from "@/components/block-content-editor/engines/editor-dialogs"
import { StudioLayout } from "./studio-layout"

export default function Page() {
  return (
    <EditorProvider>
      <StudioLayout header={<EditorToolbar />}>
        <EditorField />
      </StudioLayout>
      <EditorDialogs />
    </EditorProvider>
  )
}
