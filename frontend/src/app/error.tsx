"use client";

import { useEffect } from "react";

/**
 * Last-resort boundary for render-time crashes. Data-fetching failures are handled far more
 * specifically inside each page; this only catches what escapes them.
 */
export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("Unhandled UI error:", error);
  }, [error]);

  return (
    <main className="flex min-h-dvh flex-col items-center justify-center px-4 text-center">
      <div className="flex size-14 items-center justify-center rounded-full bg-danger-soft text-danger ring-8 ring-danger-soft/40">
        <svg
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth={1.75}
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
          className="size-6"
        >
          <path d="M10.3 3.9 2.4 17.5A2 2 0 0 0 4.1 20.5h15.8a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" />
          <path d="M12 9v4.5M12 17h.01" />
        </svg>
      </div>

      <h1 className="mt-6 text-2xl font-semibold text-ink">Something went wrong</h1>
      <p className="mt-2 max-w-md text-sm text-ink-muted">
        An unexpected error stopped this page from loading. Trying again usually helps.
      </p>

      {error.digest && (
        <p className="mt-3 rounded-field bg-canvas-sunken px-2.5 py-1 font-mono text-xs text-ink-subtle">
          Reference: {error.digest}
        </p>
      )}

      <button
        type="button"
        onClick={reset}
        className="mt-7 inline-flex h-10 items-center rounded-field bg-brand px-5 text-sm font-medium text-white shadow-sm transition-[background-color,box-shadow] duration-[120ms] hover:bg-brand-hover hover:shadow-md"
      >
        Try again
      </button>
    </main>
  );
}
