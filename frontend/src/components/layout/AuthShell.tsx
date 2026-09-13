import Link from "next/link";
import type { ReactNode } from "react";

export function Logo({ className, size = "md" }: { className?: string; size?: "md" | "lg" }) {
  const mark = size === "lg" ? "size-9 text-base rounded-[0.6rem]" : "size-7 text-sm rounded-lg";
  const word = size === "lg" ? "text-lg" : "text-base";

  return (
    <span className={className}>
      <span className="inline-flex items-center gap-2.5">
        <span
          className={`flex ${mark} items-center justify-center bg-brand font-bold text-white shadow-sm`}
          aria-hidden="true"
        >
          R
        </span>
        <span className={`${word} font-semibold tracking-[-0.02em] text-ink`}>RFQ Marketplace</span>
      </span>
    </span>
  );
}

/**
 * Centred card layout shared by sign-in and registration.
 *
 * The background carries a very soft brand wash at the top so the card reads as sitting on
 * a surface rather than floating on flat grey - visible enough to give depth, faint enough
 * that it never competes with the form.
 */
export function AuthShell({
  title,
  subtitle,
  children,
  footer,
  wide = false,
}: {
  title: string;
  subtitle: string;
  children: ReactNode;
  footer: ReactNode;
  wide?: boolean;
}) {
  return (
    <main className="relative flex min-h-dvh flex-col items-center justify-center px-4 py-10">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-0 h-72 bg-gradient-to-b from-brand-soft/70 to-transparent"
      />

      <div className={`relative w-full ${wide ? "max-w-lg" : "max-w-md"}`}>
        <Link href="/" className="mb-7 flex justify-center">
          <Logo size="lg" />
        </Link>

        <div className="animate-rise-in rounded-panel border border-line bg-surface p-6 shadow-lg sm:p-8">
          <h1 className="text-xl font-semibold text-ink">{title}</h1>
          <p className="mt-1.5 text-sm text-ink-muted">{subtitle}</p>
          <div className="mt-6">{children}</div>
        </div>

        <p className="mt-6 text-center text-sm text-ink-muted">{footer}</p>
      </div>
    </main>
  );
}
