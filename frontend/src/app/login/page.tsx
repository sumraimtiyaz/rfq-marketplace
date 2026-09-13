"use client";

import { Suspense, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { AuthShell } from "@/components/layout/AuthShell";
import { Button, Field, FormError, Input, Spinner } from "@/components/ui";
import { BuildingIcon, LockIcon, TruckIcon } from "@/components/ui/icons";
import { ApiError } from "@/lib/api";
import { homePathFor, useAuth } from "@/lib/auth";
import { loginSchema, type LoginValues } from "@/lib/validations";

const DEMO_PASSWORD = "Demo@1234";

const demoAccounts = [
  { email: "buyer@demo.com", label: "Buyer", icon: BuildingIcon },
  { email: "supplier@demo.com", label: "Supplier", icon: TruckIcon },
];

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { login } = useAuth();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      const user = await login(values);
      // Honour where the guard sent them from, but never bounce a user into the other role's area.
      const next = searchParams.get("next");
      const destination =
        next && next.startsWith(`/${user.role.toLowerCase()}`) ? next : homePathFor(user.role);
      router.replace(destination);
    } catch (error) {
      setFormError(error instanceof ApiError ? error.detail : "Could not sign in. Please try again.");
    }
  });

  const fillDemoAccount = (email: string) => {
    setValue("email", email, { shouldValidate: true });
    setValue("password", DEMO_PASSWORD, { shouldValidate: true });
    setFormError(null);
  };

  return (
    <>
      <form onSubmit={onSubmit} noValidate className="space-y-4">
        {formError && <FormError message={formError} />}

        <Field label="Email" htmlFor="email" error={errors.email?.message} required>
          <Input
            id="email"
            type="email"
            autoComplete="email"
            placeholder="you@company.com"
            invalid={!!errors.email}
            {...register("email")}
          />
        </Field>

        <Field label="Password" htmlFor="password" error={errors.password?.message} required>
          <Input
            id="password"
            type="password"
            autoComplete="current-password"
            placeholder="••••••••"
            leadingIcon={<LockIcon className="size-4" />}
            invalid={!!errors.password}
            {...register("password")}
          />
        </Field>

        <Button type="submit" size="lg" loading={isSubmitting} className="w-full">
          Sign in
        </Button>
      </form>

      {/* Evaluators land here first; one click should get them into a populated account. */}
      <div className="mt-7">
        <div className="flex items-center gap-3">
          <span className="h-px flex-1 bg-line" />
          <span className="text-xs font-medium text-ink-subtle">Or try a demo account</span>
          <span className="h-px flex-1 bg-line" />
        </div>

        <div className="mt-4 grid gap-2 sm:grid-cols-2">
          {demoAccounts.map((account) => {
            const AccountIcon = account.icon;

            return (
              <button
                key={account.email}
                type="button"
                onClick={() => fillDemoAccount(account.email)}
                className="group flex items-center gap-3 rounded-field border border-line-strong bg-surface p-3 text-left shadow-xs transition-[border-color,box-shadow,background-color] duration-[120ms] hover:border-brand hover:bg-brand-softer hover:shadow-sm"
              >
                <span className="flex size-9 shrink-0 items-center justify-center rounded-field bg-canvas-sunken text-ink-muted transition-colors group-hover:bg-brand-soft group-hover:text-brand">
                  <AccountIcon className="size-4.5" />
                </span>
                <span className="min-w-0">
                  <span className="block text-sm font-medium text-ink">{account.label}</span>
                  <span className="block truncate text-xs text-ink-subtle">{account.email}</span>
                </span>
              </button>
            );
          })}
        </div>

        <p className="mt-3 text-center text-xs text-ink-subtle">
          Fills the form — then press Sign in.
        </p>
      </div>
    </>
  );
}

export default function LoginPage() {
  return (
    <AuthShell
      title="Sign in"
      subtitle="Access your requirements and quotations."
      footer={
        <>
          Don&apos;t have an account?{" "}
          <Link href="/register" className="font-medium text-brand hover:underline">
            Create one
          </Link>
        </>
      }
    >
      <Suspense
        fallback={
          <div className="flex justify-center py-10">
            <Spinner className="size-6 text-brand" />
          </div>
        }
      >
        <LoginForm />
      </Suspense>
    </AuthShell>
  );
}
