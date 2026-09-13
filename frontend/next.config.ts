import type { NextConfig } from "next";

/**
 * The browser talks to /api/* on its own origin and Next proxies that to the ASP.NET Core API.
 *
 * That keeps the authentication cookie first-party in every environment: were the browser to
 * call the API's own domain directly, the cookie would be cross-site and would need
 * SameSite=None, which some browsers block outright. It also means no CORS preflight.
 */
const backendUrl = process.env.BACKEND_URL ?? "http://localhost:5080";

const nextConfig: NextConfig = {
  reactStrictMode: true,

  // "standalone" emits a self-contained server bundle that the production Dockerfile copies
  // instead of the whole node_modules tree. It is a SELF-HOSTING option, and a managed
  // platform that builds its own output format has no use for it.
  //
  // Opt in explicitly rather than trying to detect the platform: the Dockerfile sets
  // NEXT_OUTPUT=standalone, and every other build - Vercel, CI, a local `npm run build` -
  // gets Next's default output. Keying off a platform's own variable would mean a build
  // silently doing the wrong thing anywhere that variable happens to be missing.
  output: process.env.NEXT_OUTPUT === "standalone" ? "standalone" : undefined,
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${backendUrl}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
