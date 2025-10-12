/**
 * Simple utility function for demonstration
 */
export function formatName(firstName: string, lastName: string): string {
  const trimmedFirst = firstName.trim();
  const trimmedLast = lastName.trim();
  return `${trimmedFirst} ${trimmedLast}`.trim();
}

export function validateEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
}
