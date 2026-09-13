import { NextResponse, type NextRequest } from "next/server";

/**
 * Routing guard, not a security boundary. (Next.js 16 renamed this convention from
 * `middleware.ts` to `proxy.ts`.)
 *
 * The token is signed and only the API holds the key, so this cannot and does not try to verify
 * it. It reads the unverified payload purely to decide where to send the browser, which avoids a
 * flash of the wrong page on load. Every actual permission decision is made by the API, which
 * validates the signature on every request - a forged cookie gets a user nowhere.
 */

const AUTH_COOKIE = "rfq_token";

const BUYER_PREFIX = "/buyer";
const SUPPLIER_PREFIX = "/supplier";
const AUTH_PAGES = ["/login", "/register"];

interface TokenClaims {
  role?: string;
  exp?: number;
}

function readClaims(token: string): TokenClaims | null {
  try {
    const payload = token.split(".")[1];
    if (!payload) return null;

    const json = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
    const claims = JSON.parse(json) as Record<string, unknown>;

    // ASP.NET Core emits the role under the full XML schema claim URI.
    const role =
      claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ?? claims["role"];

    return {
      role: typeof role === "string" ? role : undefined,
      exp: typeof claims.exp === "number" ? claims.exp : undefined,
    };
  } catch {
    return null;
  }
}

function homePathFor(role: string | undefined): string {
  return role === "Buyer" ? "/buyer/rfqs" : "/supplier/rfqs";
}

export default function proxy(request: NextRequest) {
  const { pathname, search } = request.nextUrl;
  const token = request.cookies.get(AUTH_COOKIE)?.value;
  const claims = token ? readClaims(token) : null;

  const expired = claims?.exp !== undefined && claims.exp * 1000 <= Date.now();
  const signedIn = !!claims && !expired;

  if (AUTH_PAGES.includes(pathname)) {
    if (signedIn) {
      return NextResponse.redirect(new URL(homePathFor(claims.role), request.url));
    }
    return NextResponse.next();
  }

  if (!signedIn) {
    const login = new URL("/login", request.url);
    // Remember where they were headed so sign-in can return them there.
    if (pathname !== "/") login.searchParams.set("next", `${pathname}${search}`);

    const response = NextResponse.redirect(login);
    if (token) response.cookies.delete(AUTH_COOKIE); // stale cookie, clear it
    return response;
  }

  const wrongArea =
    (pathname.startsWith(BUYER_PREFIX) && claims.role !== "Buyer") ||
    (pathname.startsWith(SUPPLIER_PREFIX) && claims.role !== "Supplier");

  if (wrongArea) {
    return NextResponse.redirect(new URL(homePathFor(claims.role), request.url));
  }

  if (pathname === "/") {
    return NextResponse.redirect(new URL(homePathFor(claims.role), request.url));
  }

  return NextResponse.next();
}

export const config = {
  // Everything except Next internals, the API proxy and static assets.
  matcher: ["/((?!_next/static|_next/image|api/|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp|ico)$).*)"],
};
