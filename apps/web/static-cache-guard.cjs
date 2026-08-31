// Entrypoint wrapper for the Next standalone server (Dockerfile CMD).
//
// 1) Static-miss cache guard: during rolling deploys, old pods briefly serve
//    404s for the new build's content-hashed /_next/static chunks. Any
//    long-lived cache-control stamped on such a miss poisons CDN and browser
//    caches, leaving every visitor with dead JS (site-wide hydration
//    failure). Any >=400 response under /_next/static is forced to no-store.
//    Patched at setHeader/writeHead time because headers are already flushed
//    by res.end() on Next's static-miss path.
// 2) Purge after rollout completes: purging at pod boot is too early —
//    during a rolling deploy the OLD pods are still terminating and serve
//    404s for the NEW build's chunks, so anything Cloudflare caches in
//    that window re-poisons the zone right after the purge. Instead the
//    server starts immediately (the pod must become Ready or the rollout
//    deadlocks) and a background watcher waits for our Deployment's
//    rollout to complete (+ termination grace for old pods) before
//    purging. Rollout completion is read from the Progressing condition
//    (status=True, reason=NewReplicaSetAvailable) — the same signal
//    `kubectl rollout status` uses. Comparing updatedReplicas/readyReplicas
//    to spec.replicas is NOT enough with multiple replicas + maxSurge:
//    mid-rollout it transiently holds while a surge pod is still
//    ContainerCreating (e.g. 2 replicas: 1 new ready + 1 old ready + 1
//    new creating => updated==2==spec, ready==2==spec) and a purge fired
//    at that moment lands while old pods still serve the previous build —
//    they re-poison the zone right after the purge. A purge only fires
//    when this pod was CREATED BEFORE the rollout finished (i.e. this pod
//    is a member of that rollout); a pod recreated later (node drain,
//    eviction, restart) does not wipe the zone cache. Enabled only when
//    CF_PURGE_TOKEN and CF_ZONE_ID are set; absence is fine. Requires
//    read access to pods/replicasets/deployments in this namespace for
//    the pod's service account (see
//    infra/helm/manifests/web-rollout-reader-rbac.yaml).
"use strict";

const http = require("http");

const origSetHeader = http.OutgoingMessage.prototype.setHeader;
const origWriteHead = http.ServerResponse.prototype.writeHead;

function isStaticMiss(req, statusCode) {
  const url = (req && req.url) || "";
  return url.startsWith("/_next/static/") && statusCode >= 400;
}

http.OutgoingMessage.prototype.setHeader = function (name, value) {
  if (
    typeof name === "string" &&
    name.toLowerCase() === "cache-control" &&
    isStaticMiss(this.req, this.statusCode)
  ) {
    value = "no-store";
  }
  return origSetHeader.call(this, name, value);
};

http.ServerResponse.prototype.writeHead = function (statusCode, ...rest) {
  if (isStaticMiss(this.req, statusCode)) {
    let hasCc = false;
    for (const arg of rest) {
      if (arg && typeof arg === "object") {
        if (Array.isArray(arg)) {
          for (let i = 0; i + 1 < arg.length; i += 2) {
            if (
              String(arg[i]).toLowerCase() === "cache-control" &&
              !hasCc
            ) {
              arg[i + 1] = "no-store";
              hasCc = true;
            }
          }
        } else {
          for (const key of Object.keys(arg)) {
            if (key.toLowerCase() === "cache-control") {
              arg[key] = "no-store";
              hasCc = true;
            }
          }
        }
      }
    }
    const res = origWriteHead.call(this, statusCode, ...rest);
    if (!hasCc && !this.headersSent) {
      try {
        origSetHeader.call(this, "Cache-Control", "no-store");
      } catch {
        // headers already flushed; nothing more to do
      }
    }
    return res;
  }
  return origWriteHead.call(this, statusCode, ...rest);
};

// ── Cloudflare purge, post-rollout ────────────────────────────────────

const fs = require("fs");
const https = require("https");

const ROLLOUT_WATCH_TIMEOUT_MS = 15 * 60 * 1000; // > progressDeadlineSeconds (600s) + an auto-rollback undo rollout
const TERMINATION_GRACE_MS = 45 * 1000; // let old pods finish serving + terminate
const POLL_INTERVAL_MS = 5 * 1000;
const PURGE_ATTEMPTS = 3;
const PURGE_RETRY_DELAY_MS = 5 * 1000;

function readSaFile(name) {
  try {
    return fs
      .readFileSync(`/var/run/secrets/kubernetes.io/serviceaccount/${name}`, "utf8")
      .trim();
  } catch {
    return null;
  }
}

function k8sGet(path, token, ca) {
  return new Promise((resolve, reject) => {
    const req = https.request(
      {
        hostname: "kubernetes.default.svc",
        path,
        method: "GET",
        ca,
        headers: { Authorization: `Bearer ${token}` },
        timeout: 10 * 1000,
      },
      (res) => {
        let body = "";
        res.on("data", (c) => (body += c));
        res.on("end", () => {
          if (res.statusCode !== 200) {
            reject(new Error(`k8s API ${path} -> HTTP ${res.statusCode}`));
            return;
          }
          try {
            resolve(JSON.parse(body));
          } catch (e) {
            reject(e);
          }
        });
      },
    );
    req.on("timeout", () => req.destroy(new Error("k8s API timeout")));
    req.on("error", reject);
    req.end();
  });
}

async function findOwnDeployment() {
  const token = readSaFile("token");
  const ca = readSaFile("ca.crt");
  const ns = readSaFile("namespace");
  const podName = process.env.POD_NAME || process.env.HOSTNAME;
  if (!token || !ca || !ns || !podName) {
    throw new Error("missing in-cluster service account or pod identity");
  }

  const pod = await k8sGet(`/api/v1/namespaces/${ns}/pods/${podName}`, token, ca);
  const rsRef = (pod.metadata.ownerReferences || []).find(
    (r) => r.kind === "ReplicaSet",
  );
  if (!rsRef) throw new Error("pod is not owned by a ReplicaSet");

  const rs = await k8sGet(
    `/apis/apps/v1/namespaces/${ns}/replicasets/${rsRef.name}`,
    token,
    ca,
  );
  const depRef = (rs.metadata.ownerReferences || []).find(
    (r) => r.kind === "Deployment",
  );
  if (!depRef) throw new Error("replicaset is not owned by a Deployment");

  return {
    ns,
    token,
    ca,
    deployment: depRef.name,
    podCreatedMs: Date.parse((pod.metadata || {}).creationTimestamp || ""),
  };
}

/**
 * Parse a Deployment object into its rollout state.
 *
 * Completion MUST be read from the Progressing condition
 * (status=True, reason=NewReplicaSetAvailable, observedGeneration caught
 * up) — the same signal `kubectl rollout status` and the cluster's
 * auto-rollback CronJob use. Comparing updatedReplicas/readyReplicas to
 * spec.replicas yields FALSE positives mid-rollout with maxSurge (see
 * header comment) and caused purges to fire while old pods were still
 * serving the previous build.
 */
function rolloutStatus(dep) {
  const gen = (dep.metadata || {}).generation || 0;
  const st = dep.status || {};
  const prog = (st.conditions || []).find((c) => c.type === "Progressing");
  const complete =
    (st.observedGeneration || 0) >= gen &&
    !!prog &&
    prog.status === "True" &&
    prog.reason === "NewReplicaSetAvailable";
  const finishedAtMs =
    prog && prog.lastUpdateTime ? Date.parse(prog.lastUpdateTime) : NaN;
  return {
    complete,
    finishedAtMs,
    reason: (prog && prog.reason) || "Unknown",
  };
}

/**
 * True when this pod is a member of the rollout that finished at
 * finishedAtMs: the pod was created before the rollout finished. Pods
 * recreated after that (node drain, eviction, crash restart of the same
 * template) did not witness a content change and must not wipe the zone.
 * Both timestamps come from the API server, so no clock-skew slack is
 * needed.
 */
function isMemberOfFinishedRollout(podCreatedMs, finishedAtMs) {
  return (
    Number.isFinite(podCreatedMs) &&
    Number.isFinite(finishedAtMs) &&
    podCreatedMs <= finishedAtMs
  );
}

async function waitForRollout(ctx) {
  const deadline = Date.now() + ROLLOUT_WATCH_TIMEOUT_MS;
  for (;;) {
    const dep = await k8sGet(
      `/apis/apps/v1/namespaces/${ctx.ns}/deployments/${ctx.deployment}`,
      ctx.token,
      ctx.ca,
    );
    const status = rolloutStatus(dep);
    if (status.complete) {
      return { deployment: ctx.deployment, ...status };
    }
    if (Date.now() > deadline) {
      return { deployment: ctx.deployment, timedOut: true, ...status };
    }
    await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS));
  }
}

async function purgeCloudflareOnce() {
  const token = process.env.CF_PURGE_TOKEN;
  const zone = process.env.CF_ZONE_ID;
  const res = await fetch(
    `https://api.cloudflare.com/client/v4/zones/${zone}/purge_cache`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ purge_everything: true }),
      signal: AbortSignal.timeout(15000),
    },
  );
  const body = await res.json().catch(() => ({}));
  return { ok: res.status === 200 && body.success === true, res, body };
}

async function purgeCloudflare() {
  for (let attempt = 1; attempt <= PURGE_ATTEMPTS; attempt++) {
    try {
      const { ok, res, body } = await purgeCloudflareOnce();
      if (ok) {
        console.log(
          `[static-cache-guard] Cloudflare purge post-rollout: HTTP ${res.status} success=true (attempt ${attempt})`,
        );
        return true;
      }
      console.warn(
        `[static-cache-guard] purge attempt ${attempt}/${PURGE_ATTEMPTS}: HTTP ${res.status} success=${body.success} errors=${JSON.stringify(body.errors || [])}`,
      );
    } catch (err) {
      console.warn(
        `[static-cache-guard] purge attempt ${attempt}/${PURGE_ATTEMPTS} error: ${err && err.message}`,
      );
    }
    if (attempt < PURGE_ATTEMPTS) {
      await new Promise((r) => setTimeout(r, PURGE_RETRY_DELAY_MS));
    }
  }
  console.warn(
    "[static-cache-guard] purge failed after all attempts — manual purge may be needed",
  );
  return false;
}

async function purgeCloudflareAfterRollout() {
  if (!process.env.CF_PURGE_TOKEN || !process.env.CF_ZONE_ID) {
    console.log(
      "[static-cache-guard] CF_PURGE_TOKEN/CF_ZONE_ID unset — post-rollout purge disabled",
    );
    return;
  }
  try {
    const ctx = await findOwnDeployment();
    console.log(
      `[static-cache-guard] watching rollout of ${ctx.ns}/${ctx.deployment} for post-rollout purge`,
    );
    const { finishedAtMs, reason, timedOut } = await waitForRollout(ctx);
    if (timedOut) {
      console.warn(
        `[static-cache-guard] rollout watch timed out (last reason: ${reason}) — skipping purge (manual purge may be needed)`,
      );
      return;
    }
    if (!isMemberOfFinishedRollout(ctx.podCreatedMs, finishedAtMs)) {
      console.log(
        "[static-cache-guard] rollout finished before this pod was created — skipping purge (pod restart)",
      );
      return;
    }
    await new Promise((r) => setTimeout(r, TERMINATION_GRACE_MS));
    await purgeCloudflare();
  } catch (err) {
    console.warn(
      `[static-cache-guard] post-rollout purge failed (continuing): ${err && err.message}`,
    );
  }
}

// Serve immediately — blocking startup on the purge would deadlock the
// rollout this pod must complete (it can never become Ready otherwise).
if (require.main === module) {
  require("./apps/web/server.js");
  purgeCloudflareAfterRollout();
}

module.exports = {
  rolloutStatus,
  isMemberOfFinishedRollout,
  waitForRollout,
  purgeCloudflareAfterRollout,
};
