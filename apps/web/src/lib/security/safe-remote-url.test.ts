import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";

import { assertSafeRemoteUrl, UnsafeRemoteUrlError } from "./safe-remote-url";

const env = vi.hoisted(() => ({
  NODE_ENV: "production",
  REMOTE_ASSET_ALLOWED_HOSTS: undefined as string | undefined,
  ALLOW_UNSAFE_REMOTE_URL: undefined as string | undefined,
}));

vi.mock("node:process", () => ({
  default: {
    get env() {
      return env;
    },
  },
}));
vi.stubGlobal("process", { env });

beforeAll(() => {
  // The guard bypasses validation under NODE_ENV=test (existing fixtures use
  // local HTTP bases); these tests exercise the enforced path.
  env.NODE_ENV = "production";
});

afterEach(() => {
  env.REMOTE_ASSET_ALLOWED_HOSTS = undefined;
  env.ALLOW_UNSAFE_REMOTE_URL = undefined;
  env.NODE_ENV = "production";
});

describe("assertSafeRemoteUrl", () => {
  it("allows allowlisted https URLs", () => {
    expect(
      new URL(
        assertSafeRemoteUrl("https://cdn.gameguild.gg/logo.png").toString(),
      ).href,
    ).toBe("https://cdn.gameguild.gg/logo.png");
  });

  it("allows same-origin relative paths", () => {
    const target = assertSafeRemoteUrl("/api/assets/123/content");
    expect(target).toBe("/api/assets/123/content");
    expect(new URL(target, "https://app.gameguild.gg").origin).toBe(
      "https://app.gameguild.gg",
    );
  });

  it.each([
    "assets/file.png",
    "?page=2",
    "/api/assets?id=%2F%2Fevil.example.com",
  ])("preserves relative target %s verbatim", (target) => {
    expect(assertSafeRemoteUrl(target)).toBe(target);
  });

  it.each([
    "  //evil.example.com/x",
    "\t//evil.example.com/x",
    "\\\\evil.example.com/x",
    "/\\evil.example.com/x",
    "  https://evil.example.com/x",
  ])("rejects disguised cross-origin target %s", (target) => {
    expect(() => assertSafeRemoteUrl(target)).toThrow(UnsafeRemoteUrlError);
  });

  it("rejects http", () => {
    expect(() =>
      assertSafeRemoteUrl("http://cdn.gameguild.gg/logo.png"),
    ).toThrow(UnsafeRemoteUrlError);
  });

  it("rejects hosts outside the allowlist", () => {
    expect(() => assertSafeRemoteUrl("https://evil.example.com/x")).toThrow(
      /allowlist/,
    );
  });

  it.each([
    "https://127.0.0.1/x",
    "https://10.0.0.5/x",
    "https://172.16.0.9/x",
    "https://192.168.1.4/x",
    "https://169.254.169.254/latest/meta-data",
    "https://localhost/x",
    "https://[::1]/x",
  ])("rejects private or loopback host %s", (url) => {
    expect(() => assertSafeRemoteUrl(url)).toThrow(UnsafeRemoteUrlError);
  });

  it("rejects embedded credentials", () => {
    expect(() =>
      assertSafeRemoteUrl("https://user:pass@cdn.gameguild.gg/x"),
    ).toThrow(/credentials/);
  });

  it("rejects protocol-relative URLs", () => {
    expect(() => assertSafeRemoteUrl("//evil.example.com/x")).toThrow(
      UnsafeRemoteUrlError,
    );
  });

  it("honours REMOTE_ASSET_ALLOWED_HOSTS additions", () => {
    env.REMOTE_ASSET_ALLOWED_HOSTS = "extra.example.com";
    expect(
      new URL(assertSafeRemoteUrl("https://extra.example.com/x").toString())
        .hostname,
    ).toBe("extra.example.com");
    expect(() => assertSafeRemoteUrl("https://cdn.gameguild.gg/x")).toThrow(
      /allowlist/,
    );
  });

  it("ignores the development bypass in production", () => {
    env.ALLOW_UNSAFE_REMOTE_URL = "true";
    expect(() =>
      assertSafeRemoteUrl("http://localhost:8080/v1/assets"),
    ).toThrow(UnsafeRemoteUrlError);
  });

  it("ALLOW_UNSAFE_REMOTE_URL=true allows localhost http only in development", () => {
    env.NODE_ENV = "development";
    env.ALLOW_UNSAFE_REMOTE_URL = "true";
    expect(assertSafeRemoteUrl("http://localhost:8080/v1/assets")).toBe(
      "http://localhost:8080/v1/assets",
    );
  });
});

describe("remote asset literal addresses", () => {
  it.each([
    "https://[::ffff:127.0.0.1]/x",
    "https://[::ffff:127.255.255.255]/x",
    "https://[::ffff:10.0.0.1]/x",
    "https://[::ffff:172.16.0.1]/x",
    "https://[::ffff:172.31.255.255]/x",
    "https://[::ffff:192.168.1.1]/x",
    "https://[::ffff:169.254.169.254]/latest/meta-data",
    "https://[0:0:0:0:0:ffff:7f00:1]/x",
    "https://[::ffff:0:0]/x",
    "https://[::ffff:ff:ffff]/x",
    "https://[::]/x",
    "https://[0:0:0:0:0:0:0:0]/x",
    "https://[fe80::1]/x",
    "https://[febf:ffff:ffff:ffff:ffff:ffff:ffff:ffff]/x",
    "https://[fec0::1]/x",
    "https://[feff:ffff:ffff:ffff:ffff:ffff:ffff:ffff]/x",
    "https://[fc00::1]/x",
    "https://[fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff]/x",
    "https://[::1]/x",
    "https://0.0.0.0/x",
    "https://0.255.255.255/x",
    "https://127.1/x",
    "https://2130706433/x",
    "https://0x7f000001/x",
    "https://0177.0.0.1/x",
    "https://10.0.0.1/x",
    "https://172.16.0.1/x",
    "https://172.31.255.255/x",
    "https://192.168.1.1/x",
    "https://169.254.169.254/x",
    "https://localhost/x",
    "https://service.localhost/x",
  ])("rejects local literal even when explicitly allowlisted: %s", (raw) => {
    const canonical = new URL(raw);
    env.REMOTE_ASSET_ALLOWED_HOSTS = canonical.hostname;
    env.ALLOW_UNSAFE_REMOTE_URL = "true";
    expect(() => assertSafeRemoteUrl(raw)).toThrow(
      /private or loopback address/,
    );
    expect(() => assertSafeRemoteUrl(canonical)).toThrow(
      /private or loopback address/,
    );
  });

  it.each([
    "https://8.8.8.8/x",
    "https://[::ffff:8.8.8.8]/x",
    "https://[2001:4860:4860::8888]/x",
    "https://172.15.255.255/x",
    "https://172.32.0.1/x",
    "https://169.253.255.255/x",
    "https://169.255.0.1/x",
    "https://192.167.255.255/x",
    "https://192.169.0.1/x",
    "https://126.255.255.255/x",
    "https://128.0.0.1/x",
    "https://11.0.0.1/x",
    "https://1.0.0.1/x",
  ])("retains allowlisted non-local literal: %s", (raw) => {
    const canonical = new URL(raw);
    env.REMOTE_ASSET_ALLOWED_HOSTS = canonical.hostname;
    expect(assertSafeRemoteUrl(raw).toString()).toBe(canonical.href);
    expect(assertSafeRemoteUrl(canonical).toString()).toBe(canonical.href);
  });
});
