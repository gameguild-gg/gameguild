export interface ErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}
