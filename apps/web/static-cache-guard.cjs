// Entrypoint wrapper for the Next standalone server (Dockerfile CMD).
//
// 1) Static-miss cache guard: during rolling deploys, old pods briefly serve
//    404s for the new build's content-hashed /_next/static chunks. Any
//    long-lived cache-control stamped on such a miss poisons CDN and browser
//    caches, leaving every visitor with dead JS (site-wide hydration
//    failure). Any >=400 response under /_next/static is forced to no-store.
//    Patched at setHeader/writeHead time because headers are already flushed
//    by res.end() on Next's static-miss path.
// 2) Purge-on-deploy: each new pod purges the Cloudflare zone cache on boot
//    (best-effort) so entries cached during the rollout window are dropped.
//    Enabled only when CF_PURGE_TOKEN and CF_ZONE_ID are set; absence is fine.
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

async function purgeCloudflareOnDeploy() {
  const token = process.env.CF_PURGE_TOKEN;
  const zone = process.env.CF_ZONE_ID;
  if (!token || !zone) return;

  try {
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
    console.log(
      `[static-cache-guard] Cloudflare purge on deploy: HTTP ${res.status} success=${body.success}`,
    );
  } catch (err) {
    console.warn(
      `[static-cache-guard] Cloudflare purge failed (continuing startup): ${err && err.message}`,
    );
  }
}

purgeCloudflareOnDeploy().finally(() => {
  require("./apps/web/server.js");
});
