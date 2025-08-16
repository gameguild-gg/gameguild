"use client"

import * as React from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Badge } from "@/components/ui/badge"
import { X } from "lucide-react"
import { useCourse } from "@/components/course-context"
import { updateCourse } from "@/lib/local-db"
import { useToast } from "@/hooks/use-toast"
import type { CourseLevel } from "@/lib/types"

export default function CourseDetailsPage() {
  const course = useCourse()
  const { toast } = useToast()
  const [state, setState] = React.useState(course)
  const [newTag, setNewTag] = React.useState("")
  const [newObjective, setNewObjective] = React.useState("")

  React.useEffect(() => {
    setState(course)
  }, [course])

  const handleSave = () => {
    const updated = updateCourse(course.id, state)
    if (updated) setState(updated)
    toast({ title: "Course details updated" })
  }

  const addTag = () => {
    if (newTag.trim() && !state.tags.includes(newTag.trim())) {
      setState({ ...state, tags: [...state.tags, newTag.trim()] })
      setNewTag("")
    }
  }

  const removeTag = (tag: string) => {
    setState({ ...state, tags: state.tags.filter((t) => t !== tag) })
  }

  const addObjective = () => {
    if (newObjective.trim()) {
      setState({ ...state, learningObjectives: [...state.learningObjectives, newObjective.trim()] })
      setNewObjective("")
    }
  }

  const removeObjective = (index: number) => {
    setState({
      ...state,
      learningObjectives: state.learningObjectives.filter((_, i) => i !== index),
    })
  }

  return (
    <div className="space-y-8">
      <Card className="dark-card">
        <CardHeader>
          <CardTitle>Basic Information</CardTitle>
          <CardDescription>Core details about your course.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid gap-2">
            <Label htmlFor="title">Course Title</Label>
            <Input id="title" value={state.title} onChange={(e) => setState({ ...state, title: e.target.value })} />
          </div>

          <div className="grid gap-2">
            <Label htmlFor="shortDescription">Short Description</Label>
            <Input
              id="shortDescription"
              value={state.shortDescription || ""}
              onChange={(e) => setState({ ...state, shortDescription: e.target.value })}
              placeholder="Brief one-line description"
            />
          </div>

          <div className="grid gap-2">
            <Label htmlFor="description">Full Description</Label>
            <Textarea
              id="description"
              rows={6}
              value={state.description}
              onChange={(e) => setState({ ...state, description: e.target.value })}
            />
          </div>

          <div className="grid md:grid-cols-3 gap-4">
            <div className="grid gap-2">
              <Label>Level</Label>
              <Select value={state.level} onValueChange={(v) => setState({ ...state, level: v as CourseLevel })}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="beginner">Beginner</SelectItem>
                  <SelectItem value="intermediate">Intermediate</SelectItem>
                  <SelectItem value="advanced">Advanced</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="category">Category</Label>
              <Input
                id="category"
                value={state.category}
                onChange={(e) => setState({ ...state, category: e.target.value })}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="duration">Duration (hours)</Label>
              <Input
                id="duration"
                type="number"
                min={1}
                value={state.duration}
                onChange={(e) => setState({ ...state, duration: Number.parseInt(e.target.value) })}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="dark-card">
        <CardHeader>
          <CardTitle>Tags</CardTitle>
          <CardDescription>Add relevant tags to help students find your course.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-2">
            {state.tags.map((tag) => (
              <Badge key={tag} variant="secondary" className="flex items-center gap-1">
                {tag}
                <X className="h-3 w-3 cursor-pointer" onClick={() => removeTag(tag)} />
              </Badge>
            ))}
          </div>
          <div className="flex gap-2">
            <Input
              placeholder="Add a tag..."
              value={newTag}
              onChange={(e) => setNewTag(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && addTag()}
            />
            <Button onClick={addTag} variant="outline">
              Add
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card className="dark-card">
        <CardHeader>
          <CardTitle>Learning Objectives</CardTitle>
          <CardDescription>What will students learn from this course?</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            {state.learningObjectives.map((objective, index) => (
              <div key={index} className="flex items-center gap-2 p-3 bg-muted/20 rounded-lg">
                <span className="flex-1">{objective}</span>
                <X
                  className="h-4 w-4 cursor-pointer text-muted-foreground hover:text-foreground"
                  onClick={() => removeObjective(index)}
                />
              </div>
            ))}
          </div>
          <div className="flex gap-2">
            <Input
              placeholder="Add learning objective..."
              value={newObjective}
              onChange={(e) => setNewObjective(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && addObjective()}
            />
            <Button onClick={addObjective} variant="outline">
              Add
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card className="dark-card">
        <CardHeader>
          <CardTitle>Instructor Information</CardTitle>
          <CardDescription>Information about the course instructor.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-2">
            <Label htmlFor="instructor">Instructor Name</Label>
            <Input
              id="instructor"
              value={state.instructor}
              onChange={(e) => setState({ ...state, instructor: e.target.value })}
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="instructorBio">Instructor Bio</Label>
            <Textarea
              id="instructorBio"
              rows={4}
              value={state.instructorBio || ""}
              onChange={(e) => setState({ ...state, instructorBio: e.target.value })}
              placeholder="Brief bio about the instructor..."
            />
          </div>
        </CardContent>
      </Card>

      <Button onClick={handleSave} size="lg">
        Save Changes
      </Button>
    </div>
  )
}

