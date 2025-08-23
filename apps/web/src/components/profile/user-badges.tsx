import {Badge} from "@/components/ui/badge"
import {Code, Gamepad2, Palette} from "lucide-react"

const USER_BADGES = [
  {
    icon: Code,
    label: "Developer",
    className: "bg-gradient-to-r from-blue-500/20 to-purple-500/20 border-blue-400/30 text-blue-200"
  },
  {
    icon: Palette,
    label: "Creator",
    className: "bg-gradient-to-r from-purple-500/20 to-indigo-500/20 border-purple-400/30 text-purple-200"
  },
  {
    icon: Gamepad2,
    label: "Gamer",
    className: "bg-gradient-to-r from-emerald-500/20 to-teal-500/20 border-emerald-400/30 text-emerald-200"
  }
]

export function UserBadges() {
  return (
    <div className="flex flex-wrap gap-2">
      {USER_BADGES.map((badge) => {
        const Icon = badge.icon
        return (
          <Badge
            key={badge.label}
            className={`inline-flex items-center gap-1 px-3 py-1 rounded-full border backdrop-blur-sm ${badge.className}`}
          >
            <Icon className="w-3 h-3"/>
            {badge.label}
          </Badge>
        )
      })}
    </div>
  )
}
