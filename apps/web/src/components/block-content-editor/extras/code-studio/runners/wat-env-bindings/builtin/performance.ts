/**
 * Performance bindings (performance namespace)
 */
export function createPerformanceBindings(): Record<string, any> {
  return {
    now: () => performance.now(),
  }
}
