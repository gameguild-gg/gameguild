import {
  copyWorkspaceAssetForm,
  createWorkspaceFolderForm,
  restoreWorkspaceAssetRevisionForm,
  restrictWorkspaceFolderForm,
  updateProjectDeliverableUrlForm,
  uploadWorkspaceAssetForm,
} from '@/lib/workspace-actions';
import { getWorkspaceAssetRevisions, type WorkspaceLibrary } from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';

interface WorkspaceLibraryPanelProps {
  title: string;
  library: WorkspaceLibrary | null;
  resourceType: 'Team' | 'Project';
  resourceId: string;
  returnPath: string;
  externalUrl?: string | null;
}

export async function WorkspaceLibraryPanel({ title, library, resourceType, resourceId, returnPath, externalUrl }: WorkspaceLibraryPanelProps) {
  const revisions = new Map(await Promise.all((library?.assets ?? []).map(async (asset) => [asset.id, await getWorkspaceAssetRevisions(asset.id)] as const)));

  return <Card>
    <CardHeader><CardTitle>{title}</CardTitle><CardDescription>Uploads validate the parent workspace. Binary content is deduplicated while folders, copies and revisions remain logical resources.</CardDescription></CardHeader>
    <CardContent className="space-y-6">
      <div className={`grid gap-4 ${resourceType === 'Project' ? 'lg:grid-cols-3' : 'lg:grid-cols-2'}`}>
        <form action={uploadWorkspaceAssetForm} className="space-y-3 rounded-lg border p-4">
          <input type="hidden" name="resourceType" value={resourceType} /><input type="hidden" name="resourceId" value={resourceId} /><input type="hidden" name="returnPath" value={returnPath} />
          <div><Label htmlFor={`asset-file-${resourceId}`}>Upload file</Label><Input id={`asset-file-${resourceId}`} name="file" type="file" required /></div>
          <div><Label htmlFor={`asset-folder-${resourceId}`}>Folder</Label><select id={`asset-folder-${resourceId}`} name="folderId" className="mt-1 h-9 w-full rounded-md border bg-background px-3 text-sm"><option value="">Library root</option>{library?.folders.map((folder) => <option key={folder.id} value={folder.id}>{folder.name}</option>)}</select></div>
          <Button type="submit">Upload</Button>
        </form>
        <form action={createWorkspaceFolderForm} className="space-y-3 rounded-lg border p-4">
          <input type="hidden" name="resourceType" value={resourceType} /><input type="hidden" name="resourceId" value={resourceId} /><input type="hidden" name="returnPath" value={returnPath} />
          <div><Label htmlFor={`folder-${resourceId}`}>New virtual folder</Label><Input id={`folder-${resourceId}`} name="name" required /></div>
          <div><Label htmlFor={`parent-folder-${resourceId}`}>Parent folder</Label><select id={`parent-folder-${resourceId}`} name="parentFolderId" className="mt-1 h-9 w-full rounded-md border bg-background px-3 text-sm"><option value="">Library root</option>{library?.folders.map((folder) => <option key={folder.id} value={folder.id}>{folder.name}</option>)}</select></div>
          <Button type="submit">Create folder</Button>
        </form>
        {resourceType === 'Project' && <form action={updateProjectDeliverableUrlForm} className="space-y-3 rounded-lg border p-4">
          <input type="hidden" name="projectId" value={resourceId} /><input type="hidden" name="returnPath" value={returnPath} />
          <div><Label htmlFor={`external-deliverable-${resourceId}`}>External deliverable URL</Label><Input id={`external-deliverable-${resourceId}`} name="downloadUrl" type="url" defaultValue={externalUrl ?? ''} placeholder="https://drive.google.com/..." /></div>
          <p className="text-sm text-muted-foreground">Use a Google Drive, itch.io, repository release, or other HTTPS link when the build is hosted elsewhere.</p>
          {externalUrl && <a className="block break-all text-sm underline underline-offset-4" href={externalUrl} target="_blank" rel="noreferrer">Open current link</a>}
          <Button type="submit">Save link</Button>
        </form>}
      </div>

      <section className="space-y-3"><h2 className="text-sm font-medium">Folders and additional restrictions</h2>
        {library?.folders.map((folder) => <form key={folder.id} action={restrictWorkspaceFolderForm} className="grid gap-3 rounded-lg border p-4 md:grid-cols-[1fr_12rem_1fr_1fr_auto] md:items-end">
          <input type="hidden" name="folderId" value={folder.id} /><input type="hidden" name="returnPath" value={returnPath} />
          <div><p className="font-medium">{folder.name}</p><p className="text-xs text-muted-foreground">Restrictions only reduce inherited workspace access.</p></div>
          <div><Label htmlFor={`mode-${folder.id}`}>Restriction</Label><select id={`mode-${folder.id}`} name="mode" defaultValue={String(folder.restrictionMode)} className="mt-1 h-9 w-full rounded-md border bg-background px-3 text-sm"><option value="None">None</option><option value="SelectedTeams">Selected Teams</option><option value="TeamAuthorities">Team authorities</option>{resourceType === 'Project' && <option value="AllocatedProjectMembers">Allocated members</option>}</select></div>
          <div><Label htmlFor={`teams-${folder.id}`}>Team IDs</Label><Input id={`teams-${folder.id}`} name="teamIds" defaultValue={folder.allowedTeamIds?.join(', ') ?? ''} /></div>
          <div><Label htmlFor={`authorities-${folder.id}`}>Authorities</Label><Input id={`authorities-${folder.id}`} name="authorities" defaultValue={folder.allowedAuthorities?.join(', ') ?? ''} placeholder="Owner, Manager" /></div>
          <Button type="submit" size="sm">Save</Button>
        </form>)}
        {!library?.folders.length && <Empty message="No folders." />}
      </section>

      <section className="space-y-3"><h2 className="text-sm font-medium">Files and revisions</h2>
        {library?.assets.map((asset) => <div key={asset.id} className="space-y-3 rounded-lg border p-4">
          <div className="flex flex-wrap items-center justify-between gap-2"><div><p className="font-medium">{asset.displayName || asset.originalFilename || asset.id}</p><p className="text-xs text-muted-foreground">Revision {asset.currentRevisionNumber || 1}</p></div><Badge variant="outline">{asset.folderId ? library.folders.find((folder) => folder.id === asset.folderId)?.name || 'Folder' : 'Library root'}</Badge></div>
          <div className="grid gap-3 lg:grid-cols-[1fr_14rem_auto]"><form action={copyWorkspaceAssetForm} className="contents"><input type="hidden" name="referenceId" value={asset.id} /><input type="hidden" name="returnPath" value={returnPath} /><div><Label htmlFor={`copy-name-${asset.id}`}>Logical copy name</Label><Input id={`copy-name-${asset.id}`} name="displayName" defaultValue={`${asset.displayName || asset.originalFilename || 'File'} copy`} /></div><div><Label htmlFor={`copy-folder-${asset.id}`}>Copy to</Label><select id={`copy-folder-${asset.id}`} name="folderId" className="mt-1 h-9 w-full rounded-md border bg-background px-3 text-sm"><option value="">Library root</option>{library.folders.map((folder) => <option key={folder.id} value={folder.id}>{folder.name}</option>)}</select></div><Button type="submit" className="self-end" variant="secondary">Create copy</Button></form></div>
          <div className="flex flex-wrap gap-2">{(revisions.get(asset.id) ?? []).map((revision) => <form key={revision.id} action={restoreWorkspaceAssetRevisionForm}><input type="hidden" name="referenceId" value={asset.id} /><input type="hidden" name="revisionId" value={revision.id} /><input type="hidden" name="returnPath" value={returnPath} /><Button type="submit" size="sm" variant={revision.revisionNumber === asset.currentRevisionNumber ? 'secondary' : 'outline'} disabled={revision.revisionNumber === asset.currentRevisionNumber}>Revision {revision.revisionNumber}{revision.changeNote ? ` · ${revision.changeNote}` : ''}</Button></form>)}</div>
        </div>)}
        {!library?.assets.length && <Empty message="No files." />}
      </section>
    </CardContent>
  </Card>;
}

function Empty({ message }: { message: string }) { return <p className="py-8 text-center text-sm text-muted-foreground">{message}</p>; }
