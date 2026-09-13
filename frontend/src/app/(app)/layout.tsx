"use client";

import { useEffect, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import { AppNav } from "@/components/layout/AppNav";
import { ErrorState, LoadingState } from "@/components/ui";
import { useAuth } from "@/lib/auth";

/**
 * Shell for every signed-in page.
 *
 * The middleware already redirects visitors without a usable cookie, so reaching here without a
 * user means the API rejected the session (expired or revoked). Confirming with the API rather
 * than trusting the cookie is what makes the two layers agree.
 */
export default function AppLayout({ children }: { children: ReactNode }) {
  const { user, isLoading, isUnavailable, refetch } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (!isLoading && !user && !isUnavailable) {
      router.replace(`/login?next=${encodeURIComponent(pathname)}`);
    }
  }, [isLoading, user, isUnavailable, router, pathname]);

  if (isLoading) {
    return (
      <div className="flex min-h-dvh items-center justify-center">
        <LoadingState label="Loading your workspace…" />
      </div>
    );
  }

  if (isUnavailable) {
    return (
      <div className="mx-auto flex min-h-dvh max-w-lg items-center px-4">
        <div className="w-full">
          <ErrorState
            title="Can't reach the server"
            message="We couldn't confirm your session. The API may be starting up or temporarily unavailable."
            onRetry={refetch}
          />
        </div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="flex min-h-dvh items-center justify-center">
        <LoadingState label="Redirecting to sign in…" />
      </div>
    );
  }

  return (
    <div className="flex min-h-dvh flex-col">
      <AppNav />

      {/* Keyed on pathname so each navigation replays the entry animation. */}
      <main key={pathname} className="mx-auto w-full max-w-6xl flex-1 px-4 py-7 sm:px-6 sm:py-9">
        {children}
      </main>

      <footer className="border-t border-line px-4 py-5 text-center text-xs text-ink-subtle">
        RFQ Marketplace — signed in as {user.email}
      </footer>
    </div>
  );
}
