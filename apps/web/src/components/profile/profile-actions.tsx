import {Button} from "@/components/ui/button"
import {MessageSquare, Share2, Users} from "lucide-react"

const PROFILE_ACTIONS = [
  {
    icon: MessageSquare,
    label: "Message",
    className: "bg-blue-600/80 border-blue-400/70 hover:bg-blue-600/90 hover:border-blue-400/80 ring-blue-300/50 hover:ring-blue-300/60 focus-visible:ring-blue-300/70"
  },
  {
    icon: Users,
    label: "Follow",
    className: "bg-purple-600/80 border-purple-400/70 hover:bg-purple-600/90 hover:border-purple-400/80 ring-purple-300/50 hover:ring-purple-300/60 focus-visible:ring-purple-300/70"
  },
  {
    icon: Share2,
    label: "Share",
    className: "bg-indigo-600/80 border-indigo-400/70 hover:bg-indigo-600/90 hover:border-indigo-400/80 ring-indigo-300/50 hover:ring-indigo-300/60 focus-visible:ring-indigo-300/70"
  }
]

export function ProfileActions() {
  return (
    <div className="absolute top-4 right-6 z-20 flex gap-2">
      {PROFILE_ACTIONS.map((action) => {
        const Icon = action.icon
        return (
          <Button
            key={action.label}
            size="sm"
            variant="outline"
            className={`px-5 py-2.5 border-2 text-white rounded-xl ring-1 transition-all duration-300 font-semibold backdrop-blur-2xl backdrop-saturate-150 backdrop-brightness-90 backdrop-contrast-125 shadow-2xl focus-visible:ring-2 focus-visible:outline-none drop-shadow-lg ${action.className}`}
          >
            <Icon className="w-4 h-4 mr-2"/>
            {action.label}
          </Button>
        )
      })}
    </div>
  )
}
