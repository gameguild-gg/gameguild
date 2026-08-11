import {
  AppsV1Api,
  CoreV1Api,
  CustomObjectsApi,
  KubeConfig,
} from "@kubernetes/client-node";

// Singleton — initialized once per process. loadFromCluster reads the service
// account mount that Kubernetes injects into the pod. Will throw at module
// load when run outside a pod; routes that import this module under test
// replace it via `vi.mock("../lib/k8s")`.
const kc = new KubeConfig();
kc.loadFromCluster();

export const k8sCore: CoreV1Api = kc.makeApiClient(CoreV1Api);
export const k8sApps: AppsV1Api = kc.makeApiClient(AppsV1Api);
export const k8sCustom: CustomObjectsApi = kc.makeApiClient(CustomObjectsApi);
