export function formatTestingEventStatus(status?: string | null) {
  return (status ?? 'Draft').replace(/([a-z])([A-Z])/g, '$1 $2');
}
