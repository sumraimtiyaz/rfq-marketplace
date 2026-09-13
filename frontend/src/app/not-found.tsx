import Link from "next/link";

export default function NotFound() {
  return (
    <main className="flex min-h-dvh flex-col items-center justify-center px-4 text-center">
      <div className="flex size-14 items-center justify-center rounded-full bg-canvas-sunken text-ink-subtle ring-8 ring-canvas">
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
          <circle cx="11" cy="11" r="7" />
          <path d="m20 20-3.2-3.2" />
        </svg>
      </div>

      <p className="mt-6 text-sm font-medium text-brand">404</p>
      <h1 className="mt-1.5 text-2xl font-semibold text-ink">Page not found</h1>
      <p className="mt-2 max-w-sm text-sm text-ink-muted">
        The page you were looking for doesn&apos;t exist, or it moved.
      </p>

      <Link
        href="/"
        className="mt-7 inline-flex h-10 items-center rounded-field bg-brand px-5 text-sm font-medium text-white shadow-sm transition-[background-color,box-shadow] duration-[120ms] hover:bg-brand-hover hover:shadow-md"
      >
        Go home
      </Link>
    </main>
  );
}
