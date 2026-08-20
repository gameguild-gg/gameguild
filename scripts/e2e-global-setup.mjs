// Playwright globalSetup — boots/gates the full stack (docker DBs + API /health
// + web) before tests when E2E_RUN=1; teardown runs after all workers finish.
import { startStackSupervisor } from "./e2e-stack.mjs";

export default async function globalSetup() {
  if (!process.env.E2E_RUN) return () => {};
  return startStackSupervisor();
}
