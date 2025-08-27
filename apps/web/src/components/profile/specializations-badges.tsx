import {Badge} from "@/components/ui/badge"

const SPECIALIZATIONS = [
  "Community Building",
  "Game Testing",
  "Content Creation",
  "Collaboration",
  "Project Documentation",
  "User Experience",
  "Team Coordination",
  "Creative Problem Solving",
]

export function SpecializationsBadges() {
  return (
    <div className="flex flex-wrap gap-3">
      {SPECIALIZATIONS.map((spec, index) => (
        <Badge
          key={index}
          className="rounded-full bg-gradient-to-r from-purple-500/15 to-blue-500/15 border border-purple-400/25 text-white px-3 py-1 backdrop-blur-sm shadow-sm"
        >
          {spec}
        </Badge>
      ))}
    </div>
  )
}
