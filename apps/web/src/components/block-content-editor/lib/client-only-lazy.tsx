"use client"

import {
  Suspense,
  useEffect,
  useState,
  type ComponentType,
  type LazyExoticComponent,
  type ReactNode,
} from "react"

export function ClientOnlyLazy<TProps extends object>({
  component: Component,
  props,
  fallback,
}: {
  component: LazyExoticComponent<ComponentType<TProps>>
  props: TProps
  fallback: ReactNode
}) {
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  if (!mounted) {
    return <>{fallback}</>
  }

  return (
    <Suspense fallback={fallback}>
      <Component {...props} />
    </Suspense>
  )
}
