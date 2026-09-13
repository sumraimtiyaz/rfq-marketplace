"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import clsx from "clsx";
import { AlertCircleIcon, CheckCircleIcon, CloseIcon, InfoIcon } from "@/components/ui/icons";

/**
 * Transient confirmation of something that already happened.
 *
 * Used only for feedback that would otherwise be invisible - an action that succeeds and
 * then navigates away, where an inline message would unmount before it could be read.
 * Anything the user has to act on stays inline, next to the thing it concerns: a toast is
 * the wrong place for an error you need to fix, because it disappears.
 */

type ToastTone = "success" | "error" | "info";

interface Toast {
  id: number;
  tone: ToastTone;
  title: string;
  description?: string;
}

interface ToastContextValue {
  toast: (toast: Omit<Toast, "id">) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

const VISIBLE_MS = 5000;

const toneStyles: Record<ToastTone, { icon: typeof CheckCircleIcon; className: string }> = {
  success: { icon: CheckCircleIcon, className: "text-positive" },
  error: { icon: AlertCircleIcon, className: "text-danger" },
  info: { icon: InfoIcon, className: "text-info" },
};

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const nextId = useRef(0);
  const timers = useRef(new Map<number, ReturnType<typeof setTimeout>>());

  const dismiss = useCallback((id: number) => {
    setToasts((current) => current.filter((t) => t.id !== id));
    const timer = timers.current.get(id);
    if (timer) {
      clearTimeout(timer);
      timers.current.delete(id);
    }
  }, []);

  const toast = useCallback(
    (incoming: Omit<Toast, "id">) => {
      const id = nextId.current++;
      setToasts((current) => [...current, { ...incoming, id }]);
      timers.current.set(
        id,
        setTimeout(() => dismiss(id), VISIBLE_MS),
      );
    },
    [dismiss],
  );

  // Clearing on unmount stops a timer firing setState against a gone component.
  useEffect(() => {
    const pending = timers.current;
    return () => {
      pending.forEach(clearTimeout);
      pending.clear();
    };
  }, []);

  const value = useMemo(() => ({ toast }), [toast]);

  return (
    <ToastContext.Provider value={value}>
      {children}

      {/*
        aria-live="polite" so a screen reader announces the message after finishing its
        current sentence, rather than interrupting. The region exists even when empty so
        assistive tech is already watching it when the first toast arrives.
      */}
      <div
        aria-live="polite"
        aria-atomic="false"
        className="pointer-events-none fixed inset-x-0 bottom-0 z-[60] flex flex-col items-center gap-2 p-4 sm:items-end sm:p-6"
      >
        {toasts.map((item) => {
          const { icon: ToneIcon, className } = toneStyles[item.tone];

          return (
            <div
              key={item.id}
              role="status"
              className="animate-toast-in pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-card border border-line bg-surface p-3.5 shadow-lg"
            >
              <ToneIcon className={clsx("mt-px size-5 shrink-0", className)} />

              <div className="min-w-0 flex-1">
                <p className="text-sm font-medium text-ink">{item.title}</p>
                {item.description && (
                  <p className="mt-0.5 text-sm text-ink-muted">{item.description}</p>
                )}
              </div>

              <button
                type="button"
                onClick={() => dismiss(item.id)}
                aria-label="Dismiss notification"
                className="-m-1 rounded-md p-1 text-ink-subtle transition-colors hover:bg-canvas hover:text-ink"
              >
                <CloseIcon className="size-4" />
              </button>
            </div>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error("useToast must be used inside a ToastProvider.");
  }
  return context;
}
