export function dispatchCodeExecution(
  execute: (fileId: string) => Promise<void>,
  fileId: string,
  onFailure: () => void,
): void {
  try {
    void execute(fileId).catch(() => {
      onFailure()
    })
  } catch {
    onFailure()
  }
}
