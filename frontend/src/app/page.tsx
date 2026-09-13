import Link from "next/link";
import { Logo } from "@/components/layout/AuthShell";

const audiences = [
  {
    title: "For buyers",
    body: "Post a requirement once and compare every supplier response side by side.",
    points: ["Publish in under a minute", "Quotations ranked cheapest first", "Close when you're done"],
  },
  {
    title: "For suppliers",
    body: "Search live requirements by product and location, then quote in a couple of clicks.",
    points: ["Filter by what you can fulfil", "One quotation per requirement", "Track everything you've sent"],
  },
];

/**
 * Only reached by signed-out visitors: the proxy sends signed-in users straight to their
 * own side of the marketplace.
 */
export default function LandingPage() {
  return (
    <main className="relative flex min-h-dvh flex-col items-center justify-center px-4 py-14">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-0 h-96 bg-gradient-to-b from-brand-soft/70 to-transparent"
      />

      <div className="relative w-full max-w-2xl text-center">
        <div className="flex justify-center">
          <Logo size="lg" />
        </div>

        <h1 className="mt-9 text-4xl font-semibold text-ink sm:text-5xl">
          Requirements in.
          <br />
          <span className="text-brand">Quotations out.</span>
        </h1>

        <p className="mx-auto mt-4 max-w-lg text-base text-ink-muted sm:text-lg">
          Buyers publish what they need. Suppliers discover those requirements and respond with a
          price and a delivery time.
        </p>

        <div className="mt-9 flex flex-col justify-center gap-3 sm:flex-row">
          <Link
            href="/register"
            className="inline-flex h-11 items-center justify-center rounded-field bg-brand px-6 text-sm font-medium text-white shadow-sm transition-[background-color,box-shadow,transform] duration-[120ms] hover:bg-brand-hover hover:shadow-md active:translate-y-px"
          >
            Create an account
          </Link>
          <Link
            href="/login"
            className="inline-flex h-11 items-center justify-center rounded-field border border-line-strong bg-surface px-6 text-sm font-medium text-ink shadow-xs transition-[background-color,border-color,box-shadow,transform] duration-[120ms] hover:border-ink-subtle hover:bg-canvas hover:shadow-sm active:translate-y-px"
          >
            Sign in
          </Link>
        </div>

        <div className="mt-12 grid gap-3 text-left sm:grid-cols-2">
          {audiences.map((audience) => (
            <div
              key={audience.title}
              className="rounded-card border border-line bg-surface p-5 shadow-sm"
            >
              <h2 className="text-sm font-semibold text-ink">{audience.title}</h2>
              <p className="mt-1.5 text-sm text-ink-muted">{audience.body}</p>

              <ul className="mt-4 space-y-2 border-t border-line pt-4">
                {audience.points.map((point) => (
                  <li key={point} className="flex items-start gap-2 text-sm text-ink-muted">
                    <svg
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth={2.5}
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      aria-hidden="true"
                      className="mt-0.5 size-3.5 shrink-0 text-brand"
                    >
                      <path d="m5 12.5 4.5 4.5L19 7.5" />
                    </svg>
                    {point}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </main>
  );
}
