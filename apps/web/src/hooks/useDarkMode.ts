import { useState, useEffect } from "react"

/**
 * Hook para detectar se dark mode está ativado (Tailwind)
 * Similar ao comportamento do Tailwind CSS que verifica:
 * 1. Se a classe 'dark' existe no elemento html
 * 2. Se sistema operacional está em dark mode
 */
export function useDarkMode(): boolean {
  const [isDark, setIsDark] = useState<boolean>(false)
  const [isMounted, setIsMounted] = useState(false)

  useEffect(() => {
    setIsMounted(true)
    
    // Check initial dark mode state
    const checkDarkMode = () => {
      if (typeof document !== 'undefined') {
        // Verifica se a classe 'dark' existe no elemento html (Tailwind)
        const hasDarkClass = document.documentElement.classList.contains('dark')
        
        if (hasDarkClass) {
          setIsDark(true)
          return
        }

        // Se não houver classe 'dark', verifica preferência do sistema
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
        setIsDark(prefersDark)
      }
    }

    checkDarkMode()

    // Observer para mudanças de classe no elemento html (quando Tailwind muda o tema)
    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
          const hasDarkClass = document.documentElement.classList.contains('dark')
          setIsDark(hasDarkClass)
        }
      })
    })

    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ['class'],
    })

    // Listener para mudanças de preferência de sistema
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    const handleMediaChange = (e: MediaQueryListEvent) => {
      const hasDarkClass = document.documentElement.classList.contains('dark')
      if (!hasDarkClass) {
        setIsDark(e.matches)
      }
    }

    mediaQuery.addEventListener('change', handleMediaChange)

    return () => {
      observer.disconnect()
      mediaQuery.removeEventListener('change', handleMediaChange)
    }
  }, [])

  // Retorna false durante SSR para evitar hydration mismatch
  return isMounted ? isDark : false
}
