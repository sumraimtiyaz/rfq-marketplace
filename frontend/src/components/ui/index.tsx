"use client";

import clsx from "clsx";
import Link from "next/link";
import {
  forwardRef,
  useEffect,
  useId,
  useRef,
  type ButtonHTMLAttributes,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from "react";
import {
  AlertCircleIcon,
  AlertTriangleIcon,
  CheckCircleIcon,
  ChevronDownIcon,
  InboxIcon,
  InfoIcon,
} from "@/components/ui/icons";

export { useToast, ToastProvider } from "@/components/ui/toast";

/* -------------------------------------------------------------------------- Button */

type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";
type ButtonSize = "sm" | "md" | "lg";

const buttonBase =
  "relative inline-flex items-center justify-center gap-2 rounded-field font-medium whitespace-nowrap " +
  "transition-[background-color,border-color,color,box-shadow,transform] duration-[120ms] ease-[cubic-bezier(0.22,1,0.36,1)] " +
  // A 1px press displacement is felt more than seen, and makes a click feel like it landed.
  "active:translate-y-px disabled:pointer-events-none disabled:opacity-50";

const buttonVariants: Record<ButtonVariant, string> = {
  primary: "bg-brand text-white shadow-sm hover:bg-brand-hover hover:shadow-md active:bg-brand-active",
  secondary:
    "border border-line-strong bg-surface text-ink shadow-xs hover:border-ink-subtle hover:bg-canvas hover:shadow-sm",
  ghost: "text-ink-muted hover:bg-canvas-sunken hover:text-ink",
  danger: "bg-danger text-white shadow-sm hover:bg-[#9b1e15] hover:shadow-md",
};

const buttonSizes: Record<ButtonSize, string> = {
  sm: "h-8 px-3 text-sm",
  md: "h-10 px-4 text-sm",
  lg: "h-11 px-5 text-[0.9375rem]",
};

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  icon?: ReactNode;
}

export function Button({
  variant = "primary",
  size = "md",
  loading = false,
  icon,
  disabled,
  className,
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      {...props}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={clsx(buttonBase, buttonVariants[variant], buttonSizes[size], className)}
    >
      {/*
        The label keeps its place while loading rather than being swapped for a spinner:
        a button that changes width mid-click shifts everything around it.
      */}
      {loading && <Spinner className="absolute size-4" />}
      <span className={clsx("inline-flex items-center gap-2", loading && "invisible")}>
        {icon}
        {children}
      </span>
    </button>
  );
}

export function LinkButton({
  href,
  variant = "primary",
  size = "md",
  icon,
  className,
  children,
}: {
  href: string;
  variant?: ButtonVariant;
  size?: ButtonSize;
  icon?: ReactNode;
  className?: string;
  children: ReactNode;
}) {
  return (
    <Link
      href={href}
      className={clsx(buttonBase, buttonVariants[variant], buttonSizes[size], className)}
    >
      {icon}
      {children}
    </Link>
  );
}

/* ------------------------------------------------------------------------- Spinner */

export function Spinner({ className }: { className?: string }) {
  return (
    <svg className={clsx("animate-spin", className ?? "size-5")} viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <circle className="opacity-25" cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="2.5" />
      <path
        className="opacity-90"
        d="M21 12a9 9 0 0 0-9-9"
        stroke="currentColor"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

/* --------------------------------------------------------------------- Form fields */

interface FieldProps {
  label: string;
  htmlFor: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: ReactNode;
}

export function Field({ label, htmlFor, error, hint, required, children }: FieldProps) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={htmlFor} className="flex items-center gap-1 text-sm font-medium text-ink">
        {label}
        {required && (
          <span className="text-danger" aria-hidden="true">
            *
          </span>
        )}
      </label>

      {children}

      {/*
        Error replaces hint rather than stacking with it: once a field is wrong, the
        correction is the only thing worth reading.
      */}
      {error ? (
        <p
          id={`${htmlFor}-error`}
          role="alert"
          className="animate-fade-in flex items-start gap-1.5 text-sm text-danger"
        >
          <AlertCircleIcon className="mt-px size-4 shrink-0" />
          <span>{error}</span>
        </p>
      ) : hint ? (
        <p id={`${htmlFor}-hint`} className="text-sm text-ink-subtle">
          {hint}
        </p>
      ) : null}
    </div>
  );
}

const controlBase =
  "block w-full rounded-field border bg-surface text-sm text-ink shadow-xs " +
  "placeholder:text-ink-subtle transition-[border-color,box-shadow,background-color] duration-[120ms] " +
  "disabled:cursor-not-allowed disabled:bg-canvas-sunken disabled:text-ink-subtle";

const controlState = (invalid?: boolean) =>
  invalid
    ? "border-danger focus:border-danger focus:shadow-[0_0_0_3px_var(--color-danger-soft)]"
    : "border-line-strong hover:border-ink-subtle focus:border-brand focus:shadow-[0_0_0_3px_var(--color-brand-soft)]";

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  invalid?: boolean;
  /** Rendered inside the field on the leading edge; padding adjusts automatically. */
  leadingIcon?: ReactNode;
  /** Short static text such as a currency symbol or unit. */
  prefix?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { invalid, leadingIcon, prefix, className, ...props },
  ref,
) {
  const hasAdornment = !!leadingIcon || !!prefix;

  const field = (
    <input
      ref={ref}
      aria-invalid={invalid || undefined}
      className={clsx(
        controlBase,
        controlState(invalid),
        "h-10 px-3",
        leadingIcon && "pl-9",
        prefix && "pl-8",
        className,
      )}
      {...props}
    />
  );

  if (!hasAdornment) return field;

  return (
    <div className="relative">
      {leadingIcon && (
        <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink-subtle">
          {leadingIcon}
        </span>
      )}
      {prefix && (
        <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-sm text-ink-muted">
          {prefix}
        </span>
      )}
      {field}
    </div>
  );
});

export const Textarea = forwardRef<
  HTMLTextAreaElement,
  TextareaHTMLAttributes<HTMLTextAreaElement> & { invalid?: boolean }
>(function Textarea({ invalid, className, ...props }, ref) {
  return (
    <textarea
      ref={ref}
      aria-invalid={invalid || undefined}
      className={clsx(controlBase, controlState(invalid), "min-h-28 resize-y px-3 py-2.5", className)}
      {...props}
    />
  );
});

export const Select = forwardRef<
  HTMLSelectElement,
  SelectHTMLAttributes<HTMLSelectElement> & { invalid?: boolean }
>(function Select({ invalid, className, children, ...props }, ref) {
  return (
    <div className="relative">
      <select
        ref={ref}
        aria-invalid={invalid || undefined}
        className={clsx(
          controlBase,
          controlState(invalid),
          "h-10 cursor-pointer appearance-none pl-3 pr-9",
          className,
        )}
        {...props}
      >
        {children}
      </select>
      <ChevronDownIcon className="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-ink-subtle" />
    </div>
  );
});

/* ---------------------------------------------------------------------- Containers */

export function Card({
  className,
  interactive = false,
  children,
}: {
  className?: string;
  /** Adds hover elevation. Only for cards that are themselves a link or button. */
  interactive?: boolean;
  children: ReactNode;
}) {
  return (
    <div
      className={clsx(
        "rounded-card border border-line bg-surface shadow-sm",
        interactive &&
          "transition-[box-shadow,border-color,transform] duration-[180ms] ease-[cubic-bezier(0.22,1,0.36,1)] hover:-translate-y-0.5 hover:border-line-strong hover:shadow-md",
        className,
      )}
    >
      {children}
    </div>
  );
}

export function PageHeader({
  title,
  description,
  actions,
  breadcrumb,
}: {
  title: string;
  description?: string;
  actions?: ReactNode;
  breadcrumb?: ReactNode;
}) {
  return (
    <div className="mb-6">
      {breadcrumb && <div className="mb-3">{breadcrumb}</div>}

      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h1 className="text-[1.75rem] font-semibold text-ink">{title}</h1>
          {description && <p className="mt-1.5 text-[0.9375rem] text-ink-muted">{description}</p>}
        </div>
        {actions && <div className="flex shrink-0 flex-wrap items-center gap-2">{actions}</div>}
      </div>
    </div>
  );
}

/** Back link above a page title. */
export function BackLink({ href, children }: { href: string; children: ReactNode }) {
  return (
    <Link
      href={href}
      className="group inline-flex items-center gap-1.5 text-sm text-ink-muted transition-colors hover:text-ink"
    >
      <svg
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.75}
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
        className="size-4 transition-transform duration-[120ms] group-hover:-translate-x-0.5"
      >
        <path d="M19 12H5M11 6l-6 6 6 6" />
      </svg>
      {children}
    </Link>
  );
}

/* --------------------------------------------------------------------------- Badge */

type BadgeTone = "neutral" | "positive" | "warning" | "danger" | "brand" | "info";

const badgeTones: Record<BadgeTone, string> = {
  neutral: "border-line-strong bg-canvas text-ink-muted",
  positive: "border-positive-line bg-positive-soft text-positive",
  warning: "border-warning-line bg-warning-soft text-warning",
  danger: "border-danger-line bg-danger-soft text-danger",
  brand: "border-brand-line bg-brand-soft text-brand",
  info: "border-info-line bg-info-soft text-info",
};

const dotTones: Record<BadgeTone, string> = {
  neutral: "bg-ink-subtle",
  positive: "bg-positive",
  warning: "bg-warning",
  danger: "bg-danger",
  brand: "bg-brand",
  info: "bg-info",
};

export function Badge({
  tone = "neutral",
  dot = false,
  icon,
  children,
}: {
  tone?: BadgeTone;
  /** A small status dot. Use for lifecycle state, not for counts. */
  dot?: boolean;
  icon?: ReactNode;
  children: ReactNode;
}) {
  return (
    <span
      className={clsx(
        "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium whitespace-nowrap",
        badgeTones[tone],
      )}
    >
      {dot && <span className={clsx("size-1.5 rounded-full", dotTones[tone])} aria-hidden="true" />}
      {icon}
      {children}
    </span>
  );
}

/* ----------------------------------------------------------------------- UI states */

export function LoadingState({ label = "Loading…" }: { label?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-20 text-ink-muted" role="status">
      <Spinner className="size-7 text-brand" />
      <p className="text-sm">{label}</p>
    </div>
  );
}

/**
 * Skeletons that trace the real layout - a card outline with rows where the content will
 * be - so the page does not visibly rearrange itself the moment data lands.
 */
export function SkeletonRows({ rows = 3 }: { rows?: number }) {
  return (
    <div className="space-y-3" aria-hidden="true">
      {Array.from({ length: rows }).map((_, index) => (
        <div key={index} className="rounded-card border border-line bg-surface p-5 shadow-sm">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0 flex-1 space-y-2">
              <div className="skeleton h-4 w-2/5 rounded" />
              <div className="skeleton h-3 w-1/4 rounded" />
            </div>
            <div className="skeleton h-5 w-16 shrink-0 rounded-full" />
          </div>

          <div className="mt-5 grid grid-cols-2 gap-4 sm:grid-cols-4">
            {Array.from({ length: 4 }).map((__, cell) => (
              <div key={cell} className="space-y-1.5">
                <div className="skeleton h-2.5 w-12 rounded" />
                <div className="skeleton h-3.5 w-16 rounded" />
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

export function SkeletonStats({ count = 4 }: { count?: number }) {
  return (
    <div
      className={clsx("mb-6 grid gap-3 sm:grid-cols-2", count === 3 ? "lg:grid-cols-3" : "lg:grid-cols-4")}
      aria-hidden="true"
    >
      {Array.from({ length: count }).map((_, index) => (
        <div key={index} className="rounded-card border border-line bg-surface p-5 shadow-sm">
          <div className="skeleton h-3 w-24 rounded" />
          <div className="skeleton mt-3 h-7 w-12 rounded" />
        </div>
      ))}
    </div>
  );
}

export function EmptyState({
  title,
  description,
  action,
  icon,
}: {
  title: string;
  description?: string;
  action?: ReactNode;
  icon?: ReactNode;
}) {
  return (
    <Card className="animate-rise-in flex flex-col items-center justify-center px-6 py-16 text-center">
      <div className="flex size-12 items-center justify-center rounded-full bg-canvas-sunken text-ink-subtle ring-8 ring-canvas">
        {icon ?? <InboxIcon className="size-6" />}
      </div>
      <h2 className="mt-5 text-base font-semibold text-ink">{title}</h2>
      {description && <p className="mt-1.5 max-w-sm text-sm text-ink-muted">{description}</p>}
      {action && <div className="mt-6">{action}</div>}
    </Card>
  );
}

export function ErrorState({
  title = "Something went wrong",
  message,
  onRetry,
}: {
  title?: string;
  message?: string;
  onRetry?: () => void;
}) {
  return (
    <Card className="animate-rise-in flex flex-col items-center justify-center px-6 py-16 text-center">
      <div className="flex size-12 items-center justify-center rounded-full bg-danger-soft text-danger ring-8 ring-danger-soft/40">
        <AlertTriangleIcon className="size-6" />
      </div>
      <h2 className="mt-5 text-base font-semibold text-ink">{title}</h2>
      {message && <p className="mt-1.5 max-w-md text-sm text-ink-muted">{message}</p>}
      {onRetry && (
        <Button variant="secondary" size="sm" className="mt-6" onClick={onRetry}>
          Try again
        </Button>
      )}
    </Card>
  );
}

/* ------------------------------------------------------------------------- Notices */

type NoticeTone = "danger" | "positive" | "info" | "warning";

const noticeTones: Record<NoticeTone, { wrapper: string; icon: typeof InfoIcon }> = {
  danger: { wrapper: "border-danger-line bg-danger-soft text-danger", icon: AlertCircleIcon },
  positive: { wrapper: "border-positive-line bg-positive-soft text-positive", icon: CheckCircleIcon },
  info: { wrapper: "border-info-line bg-info-soft text-info", icon: InfoIcon },
  warning: { wrapper: "border-warning-line bg-warning-soft text-warning", icon: AlertTriangleIcon },
};

export function Notice({
  tone = "info",
  title,
  children,
}: {
  tone?: NoticeTone;
  title?: string;
  children?: ReactNode;
}) {
  const { wrapper, icon: ToneIcon } = noticeTones[tone];

  return (
    <div
      role={tone === "danger" ? "alert" : "status"}
      className={clsx("animate-fade-in flex items-start gap-2.5 rounded-field border px-3.5 py-3 text-sm", wrapper)}
    >
      <ToneIcon className="mt-px size-4.5 shrink-0" />
      <div className="min-w-0">
        {title && <p className="font-medium">{title}</p>}
        {children && <div className={clsx(title && "mt-0.5 opacity-90")}>{children}</div>}
      </div>
    </div>
  );
}

/** Non-field error shown above a form: a failed sign-in, a 409, a network drop. */
export function FormError({ message }: { message?: string | null }) {
  if (!message) return null;
  return <Notice tone="danger">{message}</Notice>;
}

export function SuccessNote({ children }: { children: ReactNode }) {
  return <Notice tone="positive">{children}</Notice>;
}

/* ---------------------------------------------------------------------------- Stat */

export function Stat({
  label,
  value,
  hint,
  icon,
  loading = false,
  tone = "neutral",
}: {
  label: string;
  value: ReactNode;
  hint?: string;
  icon?: ReactNode;
  loading?: boolean;
  tone?: "neutral" | "brand" | "positive" | "warning";
}) {
  const iconTones = {
    neutral: "bg-canvas-sunken text-ink-muted",
    brand: "bg-brand-soft text-brand",
    positive: "bg-positive-soft text-positive",
    warning: "bg-warning-soft text-warning",
  } as const;

  return (
    <Card className="p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-sm text-ink-muted">{label}</p>

          {loading ? (
            <div className="skeleton mt-2.5 h-7 w-12 rounded" />
          ) : (
            <p className="mt-1.5 text-2xl font-semibold tabular-nums text-ink">{value}</p>
          )}

          {hint && <p className="mt-1 text-xs text-ink-subtle">{hint}</p>}
        </div>

        {icon && (
          <div className={clsx("flex size-9 shrink-0 items-center justify-center rounded-field", iconTones[tone])}>
            {icon}
          </div>
        )}
      </div>
    </Card>
  );
}

/* -------------------------------------------------------------------- Confirmation */

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel = "Confirm",
  destructive = false,
  loading = false,
  onConfirm,
  onCancel,
}: {
  open: boolean;
  title: string;
  description: string;
  confirmLabel?: string;
  destructive?: boolean;
  loading?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  const titleId = useId();
  const panelRef = useRef<HTMLDivElement>(null);

  // Escape closes, and the confirm button takes focus so the dialog is immediately
  // operable from the keyboard. Body scroll locks so the page behind cannot move.
  useEffect(() => {
    if (!open) return;

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !loading) onCancel();
    };

    document.addEventListener("keydown", onKeyDown);
    panelRef.current?.querySelector<HTMLButtonElement>("[data-confirm]")?.focus();

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.removeEventListener("keydown", onKeyDown);
      document.body.style.overflow = previousOverflow;
    };
  }, [open, loading, onCancel]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center p-4 sm:items-center">
      <div
        className="animate-fade-in absolute inset-0 bg-ink/40 backdrop-blur-[2px]"
        onClick={loading ? undefined : onCancel}
        aria-hidden="true"
      />

      <div
        ref={panelRef}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="animate-dialog-in relative w-full max-w-md rounded-panel border border-line bg-surface p-6 shadow-xl"
      >
        <div className="flex gap-4">
          {destructive && (
            <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-danger-soft text-danger">
              <AlertTriangleIcon className="size-5" />
            </div>
          )}

          <div className="min-w-0">
            <h2 id={titleId} className="text-base font-semibold text-ink">
              {title}
            </h2>
            <p className="mt-1.5 text-sm text-ink-muted">{description}</p>
          </div>
        </div>

        <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button variant="secondary" onClick={onCancel} disabled={loading}>
            Cancel
          </Button>
          <Button
            data-confirm
            variant={destructive ? "danger" : "primary"}
            onClick={onConfirm}
            loading={loading}
          >
            {confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  );
}
