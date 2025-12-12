import { useState, useEffect } from "react"

/**
 * Hook para detectar se dark mode está ativado (Tailwind)
 * Similar ao comportamento do Tailwind CSS que verifica:
 * 1. Se a classe 'dark' existe no elemento html
 * 2. Se sistema operacional está em dark mode
 */
export function useDarkMode(): boolean {
  // Inicializa com o valor atual (SSR-safe)
  const [isDark, setIsDark] = useState<boolean>(() => {
    if (typeof document !== 'undefined') {
      return document.documentElement.classList.contains('dark')
    }
    return false
  })

  useEffect(() => {
    // Verifica o estado atual imediatamente
    const hasDarkClass = document.documentElement.classList.contains('dark')
    setIsDark(hasDarkClass)

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

    return () => {
      observer.disconnect()
    }
  }, [])

  return isDark
}
