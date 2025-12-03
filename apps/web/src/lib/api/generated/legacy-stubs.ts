// Legacy API endpoint stubs for disabled/removed modules
// These functions mimic the shape of generated SDK calls used by legacy components.

export async function getApiProjectsSlugBySlug(_args: { path: { slug: string }; headers?: Record<string, string> }) {
  return { data: null, error: { message: 'Not implemented (STUB)' }, response: { status: 501 } } as any;
}

export async function postApiProjects(_args: { body: any; headers?: Record<string, string> }) {
  return { data: null, error: { message: 'Not implemented (STUB)' }, response: { status: 501 } } as any;
}
