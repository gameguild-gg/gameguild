import {Card, CardContent} from "@/components/ui/card"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {ExternalLink, Star} from "lucide-react"
import Image from "next/image"

interface ProjectCardProps {
  name: string
  description?: string
  tech: string
  rating: number
  featured?: boolean
  imageHeight?: "h-32" | "h-48"
  colSpan?: string
}

export function ProjectCard({
                              name,
                              description,
                              tech,
                              rating,
                              featured = false,
                              imageHeight = "h-32",
                              colSpan = ""
                            }: ProjectCardProps) {
  return (
    <Card className={`bg-slate-800/50 border-purple-500/20 overflow-hidden ${colSpan}`}>
      <div
        className={`relative ${imageHeight} ${featured ? 'bg-gradient-to-br from-blue-600 to-purple-600' : 'bg-gradient-to-br from-slate-700 to-slate-600'}`}>
        <Image
          src={`/placeholder.svg?height=${imageHeight === "h-48" ? "192" : "128"}&width=${featured ? "400" : "300"}`}
          alt={name}
          className="w-full h-full object-cover"
          width={featured ? 400 : 300}
          height={imageHeight === "h-48" ? 192 : 128}
        />
        {featured && (
          <Badge className="absolute top-4 left-4 bg-yellow-600 text-yellow-100">Featured</Badge>
        )}
      </div>
      <CardContent className={featured ? "p-6" : "p-4"}>
        <h3 className={`font-bold text-white mb-2 ${featured ? 'text-xl' : 'text-base'}`}>{name}</h3>
        {description && (
          <p className="text-gray-300 mb-4">{description}</p>
        )}
        {featured && (
          <div className="flex flex-wrap gap-2 mb-4">
            <Badge
              className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 backdrop-blur-sm shadow-md">
              Community
            </Badge>
            <Badge
              className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 backdrop-blur-sm shadow-md">
              Collaborative
            </Badge>
            <Badge
              className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 backdrop-blur-sm shadow-md">
              Open Source
            </Badge>
          </div>
        )}
        {!featured && (
          <Badge
            className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 mb-3 backdrop-blur-sm shadow-md">
            {tech}
          </Badge>
        )}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4 text-sm text-gray-400">
            <span className="flex items-center gap-1">
              <Star className={featured ? "w-4 h-4" : "w-3 h-3"}/>
              {rating}
            </span>
            <span>{featured ? "Community project" : "Community"}</span>
          </div>
          {featured && (
            <Button size="sm"
                    className="px-4 py-2 bg-gradient-to-r from-blue-600/80 to-purple-600/80 text-white rounded-lg hover:from-blue-700/80 hover:to-purple-700/80 transition-all duration-300 font-medium shadow-lg border border-white/10 backdrop-blur-sm">
              <ExternalLink className="w-4 h-4 mr-2"/>
              View Project
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  )
}
