export interface GeneratorConfig {
  openApiSource: string;
  generatedSourceLabel: string;
  watch: boolean;
  force: boolean;
}

export function resolveGeneratorConfig(
  args: string[],
  environment: Record<string, string | undefined>
): GeneratorConfig {
  const openApiIndex = args.indexOf('--openapi');
  const artifactPath = openApiIndex >= 0 ? args[openApiIndex + 1] : undefined;

  if (openApiIndex >= 0 && (!artifactPath || artifactPath.startsWith('--'))) {
    throw new Error('--openapi requires a JSON artifact path');
  }

  const openApiSource =
    artifactPath ??
    environment.OPENAPI_ARTIFACT ??
    environment.OPENAPI_URL ??
    'http://localhost:8080/swagger/v1/swagger.json';
  return {
    openApiSource,
    // Generated artifacts must be reproducible regardless of whether the same
    // specification was read from a local API URL or a captured CI artifact.
    generatedSourceLabel: 'gameguild-openapi',
    watch: args.includes('--watch'),
    force: args.includes('--force'),
  };
}
