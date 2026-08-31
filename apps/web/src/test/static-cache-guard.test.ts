import { createRequire } from "node:module";

const require = createRequire(import.meta.url);

// Requiring the guard is safe: the Next server boot and the purge trigger
// are gated behind `require.main === module`.
const guard = require("../../static-cache-guard.cjs") as {
  rolloutStatus: (dep: DeploymentLike) => {
    complete: boolean;
    finishedAtMs: number;
    reason: string;
  };
  isMemberOfFinishedRollout: (podCreatedMs: number, finishedAtMs: number) => boolean;
};

interface ConditionLike {
  type: string;
  status: string;
  reason: string;
  lastUpdateTime: string;
}

interface DeploymentLike {
  metadata: { generation: number };
  spec: { replicas: number };
  status: {
    observedGeneration?: number;
    updatedReplicas?: number;
    readyReplicas?: number;
    conditions: ConditionLike[];
  };
}

// ── Fixtures: field values observed live on prod/web-production on
// 2026-08-31 (2 replicas, RollingUpdate maxSurge=1 maxUnavailable=0) ──

/**
 * THE TRAP. Reconstructed from the observed rollout of RS
 * web-production-5c59b7dd85: old ReplicaSet scaled 2→1 while the second
 * surge pod was still ContainerCreating. At that instant both new-RS pods
 * existed (updatedReplicas=2==spec.replicas) and exactly one new + one old
 * pod were Ready (readyReplicas=2==spec.replicas). The OLD heuristic
 * (updatedReplicas === spec.replicas && readyReplicas === spec.replicas)
 * evaluated "rollout complete" here and pod pk2v2 purged mid-rollout,
 * while its sibling surge pod was still creating and an old pod was still
 * serving — the exact re-poison window this guard exists to prevent.
 */
const MID_ROLLOUT_TRANSIENT: DeploymentLike = {
  metadata: { generation: 152 },
  spec: { replicas: 2 },
  status: {
    observedGeneration: 152,
    updatedReplicas: 2,
    readyReplicas: 2,
    conditions: [
      {
        type: "Available",
        status: "True",
        reason: "MinimumReplicasAvailable",
        lastUpdateTime: "2026-08-31T06:40:51Z",
      },
      {
        type: "Progressing",
        status: "True",
        reason: "ReplicaSetUpdated",
        lastUpdateTime: "2026-08-31T20:08:25Z",
      },
    ],
  },
};

/** Early rollout: old pods 2/2 ready, first surge pod ready, second not yet created. */
const MID_ROLLOUT_EARLY: DeploymentLike = {
  metadata: { generation: 152 },
  spec: { replicas: 2 },
  status: {
    observedGeneration: 152,
    updatedReplicas: 1,
    readyReplicas: 3,
    conditions: [
      {
        type: "Progressing",
        status: "True",
        reason: "ReplicaSetUpdated",
        lastUpdateTime: "2026-08-31T20:07:39Z",
      },
    ],
  },
};

/** Captured via kubectl get deploy web-production -n prod -o json at completion. */
const COMPLETED: DeploymentLike = {
  metadata: { generation: 152 },
  spec: { replicas: 2 },
  status: {
    observedGeneration: 152,
    updatedReplicas: 2,
    readyReplicas: 2,
    conditions: [
      {
        type: "Available",
        status: "True",
        reason: "MinimumReplicasAvailable",
        lastUpdateTime: "2026-08-31T06:40:51Z",
      },
      {
        type: "Progressing",
        status: "True",
        reason: "NewReplicaSetAvailable",
        lastUpdateTime: "2026-08-31T20:10:23Z",
      },
    ],
  },
};

/** What the auto-rollback CronJob reacts to (infra/k8s/auto-rollback.yaml). */
const PROGRESS_DEADLINE_EXCEEDED: DeploymentLike = {
  metadata: { generation: 153 },
  spec: { replicas: 2 },
  status: {
    observedGeneration: 153,
    updatedReplicas: 1,
    readyReplicas: 1,
    conditions: [
      {
        type: "Progressing",
        status: "False",
        reason: "ProgressDeadlineExceeded",
        lastUpdateTime: "2026-08-31T21:00:00Z",
      },
    ],
  },
};

describe("static-cache-guard rolloutStatus", () => {
  it("does NOT report complete in the mid-rollout transient that satisfied the old count-comparison heuristic", () => {
    // Documents the production bug: the trap state really did satisfy
    // updatedReplicas === spec.replicas && readyReplicas === spec.replicas.
    expect(MID_ROLLOUT_TRANSIENT.status.updatedReplicas).toBe(
      MID_ROLLOUT_TRANSIENT.spec.replicas,
    );
    expect(MID_ROLLOUT_TRANSIENT.status.readyReplicas).toBe(
      MID_ROLLOUT_TRANSIENT.spec.replicas,
    );

    const status = guard.rolloutStatus(MID_ROLLOUT_TRANSIENT);
    expect(status.complete).toBe(false);
    expect(status.reason).toBe("ReplicaSetUpdated");
  });

  it("does not report complete early in the rollout", () => {
    const status = guard.rolloutStatus(MID_ROLLOUT_EARLY);
    expect(status.complete).toBe(false);
    expect(status.reason).toBe("ReplicaSetUpdated");
  });

  it("reports complete on the captured finished rollout with the finish timestamp", () => {
    const status = guard.rolloutStatus(COMPLETED);
    expect(status.complete).toBe(true);
    expect(status.reason).toBe("NewReplicaSetAvailable");
    expect(status.finishedAtMs).toBe(Date.parse("2026-08-31T20:10:23Z"));
  });

  it("does not report complete while the controller has not observed the generation", () => {
    const status = guard.rolloutStatus({
      ...COMPLETED,
      status: { ...COMPLETED.status, observedGeneration: 151 },
    });
    expect(status.complete).toBe(false);
  });

  it("does not report complete when the rollout failed (ProgressDeadlineExceeded)", () => {
    const status = guard.rolloutStatus(PROGRESS_DEADLINE_EXCEEDED);
    expect(status.complete).toBe(false);
    expect(status.reason).toBe("ProgressDeadlineExceeded");
  });
});

describe("static-cache-guard isMemberOfFinishedRollout", () => {
  const finishedAt = Date.parse("2026-08-31T20:10:23Z");

  it("purges for a pod created during the rollout that finished (pk2v2: created 20:05:18Z)", () => {
    expect(
      guard.isMemberOfFinishedRollout(Date.parse("2026-08-31T20:05:18Z"), finishedAt),
    ).toBe(true);
  });

  it("skips purge for a pod recreated after the rollout finished (restart/drain/eviction)", () => {
    expect(
      guard.isMemberOfFinishedRollout(Date.parse("2026-08-31T20:30:00Z"), finishedAt),
    ).toBe(false);
  });

  it("includes a pod created exactly when the rollout finished", () => {
    expect(guard.isMemberOfFinishedRollout(finishedAt, finishedAt)).toBe(true);
  });

  it("is false when either timestamp is unknown", () => {
    expect(guard.isMemberOfFinishedRollout(Number.NaN, finishedAt)).toBe(false);
    expect(
      guard.isMemberOfFinishedRollout(Date.parse("2026-08-31T20:05:18Z"), Number.NaN),
    ).toBe(false);
  });
});
