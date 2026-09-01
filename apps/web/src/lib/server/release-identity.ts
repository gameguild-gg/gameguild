export interface ReleaseIdentity {
  version: string;
  releaseSha: string;
  sourceTree: string;
  imageDigest: string;
  builtAt: string;
  deployedAt: string;
}

type ReleaseEnvironment = Readonly<Record<string, string | undefined>>;

const readValue = (environment: ReleaseEnvironment, name: string): string => {
  const value = environment[name]?.trim();
  return value ? value : 'Unknown';
};

export function readReleaseIdentity(environment: ReleaseEnvironment = process.env): ReleaseIdentity {
  const version = ['VERSION', 'GAMEGUILD_VERSION', 'npm_package_version']
    .map((name) => readValue(environment, name))
    .find((value) => value !== 'Unknown') ?? 'Unknown';

  return {
    version,
    releaseSha: readValue(environment, 'RELEASE_SHA'),
    sourceTree: readValue(environment, 'SOURCE_TREE'),
    imageDigest: readValue(environment, 'IMAGE_DIGEST'),
    builtAt: readValue(environment, 'BUILT_AT'),
    deployedAt: readValue(environment, 'DEPLOYED_AT'),
  };
}
