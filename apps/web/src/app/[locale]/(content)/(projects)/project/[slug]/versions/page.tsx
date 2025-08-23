"use client"

import * as React from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Download, Plus, Calendar } from "lucide-react"
import { useProject } from "@/components/projects/project-context"

export default function VersionsPage() {
  const project = useProject()

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Versions</h2>
          <p className="text-muted-foreground">Manage different versions of your game.</p>
        </div>
        <Button>
          <Plus className="h-4 w-4 mr-2" />
          Upload New Version
        </Button>
      </div>

      <div className="grid gap-4">
        {project.versions && project.versions.length > 0 ? (
          project.versions.map((version, index) => (
            <Card key={version.id} className="dark-card">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <CardTitle className="text-lg">Version {version.version}</CardTitle>
                    {index === 0 && <Badge variant="default">Latest</Badge>}
                  </div>
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Calendar className="h-4 w-4" />
                    {new Date(version.createdAt).toLocaleDateString()}
                  </div>
                </div>
                {version.notes && <CardDescription>{version.notes}</CardDescription>}
              </CardHeader>
              <CardContent>
                <div className="flex items-center justify-between">
                  <div className="text-sm text-muted-foreground">
                    Build available for download
                  </div>
                  <Button variant="outline" size="sm">
                    <Download className="h-4 w-4 mr-2" />
                    Download
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))
        ) : (
          <Card className="dark-card">
            <CardContent className="p-12 text-center">
              <div className="p-3 bg-muted/20 rounded-full w-fit mx-auto mb-4">
                <Download className="h-8 w-8 text-muted-foreground" />
              </div>
              <h3 className="text-lg font-semibold mb-2">No versions uploaded</h3>
              <p className="text-muted-foreground mb-6">
                Upload your first build to start distributing your game.
              </p>
              <Button>
                <Plus className="h-4 w-4 mr-2" />
                Upload First Version
              </Button>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  )
}
