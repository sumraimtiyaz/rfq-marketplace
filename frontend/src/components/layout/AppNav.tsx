"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import clsx from "clsx";
import { Logo } from "@/components/layout/AuthShell";
import { Button } from "@/components/ui";
import {
  CloseIcon,
  DocumentIcon,
  InboxIcon,
  LogOutIcon,
  MenuIcon,
  PlusIcon,
  SearchIcon,
} from "@/components/ui/icons";
import { useAuth } from "@/lib/auth";
import type { Role } from "@/types";

interface NavLink {
  href: string;
  label: string;
  icon: typeof DocumentIcon;
}

const navByRole: Record<Role, NavLink[]> = {
  Buyer: [
    { href: "/buyer/rfqs", label: "My RFQs", icon: DocumentIcon },
    { href: "/buyer/rfqs/new", label: "Create RFQ", icon: PlusIcon },
  ],
  Supplier: [
    { href: "/supplier/rfqs", label: "Browse RFQs", icon: SearchIcon },
    { href: "/supplier/quotations", label: "My quotations", icon: InboxIcon },
  ],
};

/** Two initials from the company name, for the avatar. */
function initialsOf(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0]?.toUpperCase() ?? "")
    .join("");
}

export function AppNav() {
  const pathname = usePathname();
  const { user, logout } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);
  const [signingOut, setSigningOut] = useState(false);

  if (!user) return null;

  const links = navByRole[user.role];

  const isActive = (href: string) =>
    href === pathname || (pathname.startsWith(`${href}/`) && !href.endsWith("/new"));

  const handleSignOut = async () => {
    setSigningOut(true);
    try {
      await logout();
    } finally {
      setSigningOut(false);
    }
  };

  return (
    <header className="sticky top-0 z-40 border-b border-line bg-surface/80 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-6xl items-center gap-4 px-4 sm:px-6">
        <Link href={links[0].href} className="shrink-0">
          <Logo />
        </Link>

        <nav className="hidden items-center gap-1 sm:flex" aria-label="Main">
          {links.map((link) => {
            const LinkIcon = link.icon;
            const active = isActive(link.href);

            return (
              <Link
                key={link.href}
                href={link.href}
                aria-current={active ? "page" : undefined}
                className={clsx(
                  "inline-flex items-center gap-2 rounded-field px-3 py-1.5 text-sm font-medium transition-colors duration-[120ms]",
                  active
                    ? "bg-brand-soft text-brand"
                    : "text-ink-muted hover:bg-canvas-sunken hover:text-ink",
                )}
              >
                <LinkIcon className="size-4" />
                {link.label}
              </Link>
            );
          })}
        </nav>

        <div className="ml-auto flex items-center gap-3">
          <div className="hidden items-center gap-2.5 sm:flex">
            <div className="text-right">
              <p className="text-sm font-medium leading-tight text-ink">{user.companyName}</p>
              <p className="text-xs leading-tight text-ink-subtle">{user.role}</p>
            </div>
            <div
              className="flex size-9 items-center justify-center rounded-full bg-brand-soft text-xs font-semibold text-brand"
              aria-hidden="true"
            >
              {initialsOf(user.companyName)}
            </div>
          </div>

          {/*
            Wrapped rather than given "hidden sm:inline-flex" directly: Button's base class
            already sets inline-flex, and two display utilities of equal specificity are
            resolved by stylesheet order, not class order - so the button stayed visible.
          */}
          <div className="hidden sm:block">
            <Button
              variant="secondary"
              size="sm"
              onClick={handleSignOut}
              loading={signingOut}
              icon={<LogOutIcon className="size-4" />}
            >
              Sign out
            </Button>
          </div>

          <button
            type="button"
            onClick={() => setMenuOpen((open) => !open)}
            aria-expanded={menuOpen}
            aria-label={menuOpen ? "Close menu" : "Open menu"}
            className="rounded-field border border-line-strong p-2 text-ink-muted transition-colors hover:bg-canvas sm:hidden"
          >
            {menuOpen ? <CloseIcon className="size-5" /> : <MenuIcon className="size-5" />}
          </button>
        </div>
      </div>

      {menuOpen && (
        <div className="animate-fade-in border-t border-line bg-surface px-4 py-3 sm:hidden">
          <div className="flex items-center gap-2.5 pb-3">
            <div
              className="flex size-9 items-center justify-center rounded-full bg-brand-soft text-xs font-semibold text-brand"
              aria-hidden="true"
            >
              {initialsOf(user.companyName)}
            </div>
            <div className="min-w-0">
              <p className="truncate text-sm font-medium text-ink">{user.companyName}</p>
              <p className="text-xs text-ink-subtle">{user.role}</p>
            </div>
          </div>

          <nav className="flex flex-col gap-1 border-t border-line pt-3" aria-label="Mobile">
            {links.map((link) => {
              const LinkIcon = link.icon;
              const active = isActive(link.href);

              return (
                <Link
                  key={link.href}
                  href={link.href}
                  onClick={() => setMenuOpen(false)}
                  aria-current={active ? "page" : undefined}
                  className={clsx(
                    "inline-flex items-center gap-2.5 rounded-field px-3 py-2.5 text-sm font-medium transition-colors",
                    active ? "bg-brand-soft text-brand" : "text-ink-muted hover:bg-canvas-sunken",
                  )}
                >
                  <LinkIcon className="size-4" />
                  {link.label}
                </Link>
              );
            })}
          </nav>

          <Button
            variant="secondary"
            onClick={handleSignOut}
            loading={signingOut}
            icon={<LogOutIcon className="size-4" />}
            className="mt-3 w-full"
          >
            Sign out
          </Button>
        </div>
      )}
    </header>
  );
}
