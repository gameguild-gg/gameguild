import * as React from "react"
import { Input as InputPrimitive } from "@base-ui/react/input"
import { cn } from "cn"

function Input({ className, type, defaultValue, ...props }: React.ComponentProps<"input">) {
  // Base UI keeps Field.Control's initial defaultValue in its uncontrolled
  // state. When server-rendered data changes after a form action, remount only
  // the uncontrolled input so it reflects the persisted value without React's
  // uncontrolled-defaultValue warning. Controlled inputs remain untouched.
  const inputKey = props.value === undefined ? `default:${JSON.stringify(defaultValue)}` : undefined

  return (
    <InputPrimitive
      key={inputKey}
      type={type}
      defaultValue={defaultValue}
      data-slot="input"
      className={cn(
        "h-9 w-full min-w-0 rounded-md border border-input bg-transparent px-2.5 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 md:text-sm dark:bg-input/30 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40",
        className
      )}
      {...props}
    />
  )
}

export { Input }
