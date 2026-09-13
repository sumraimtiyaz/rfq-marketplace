"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import clsx from "clsx";
import { AuthShell } from "@/components/layout/AuthShell";
import { Button, Field, FormError, Input, useToast } from "@/components/ui";
import { BuildingIcon, CheckIcon, LockIcon, TruckIcon } from "@/components/ui/icons";
import { applyApiErrors } from "@/lib/forms";
import { homePathFor, useAuth } from "@/lib/auth";
import { registerSchema, type RegisterValues } from "@/lib/validations";
import type { Role } from "@/types";

const roleOptions: { value: Role; title: string; description: string; icon: typeof BuildingIcon }[] = [
  {
    value: "Buyer",
    title: "Buyer",
    description: "Publish requirements and compare quotations.",
    icon: BuildingIcon,
  },
  {
    value: "Supplier",
    title: "Supplier",
    description: "Browse requirements and quote for them.",
    icon: TruckIcon,
  },
];

/** Live checklist of the password rules, so nothing is discovered only on submit. */
const passwordRules = [
  { label: "8+ characters", test: (v: string) => v.length >= 8 },
  { label: "Upper & lower case", test: (v: string) => /[A-Z]/.test(v) && /[a-z]/.test(v) },
  { label: "A number", test: (v: string) => /[0-9]/.test(v) },
];

export default function RegisterPage() {
  const router = useRouter();
  const { register: createAccount } = useAuth();
  const { toast } = useToast();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    control,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { name: "", companyName: "", email: "", password: "", role: "Buyer" },
  });

  // useWatch subscribes to just this field; watch() returns a fresh function each render,
  // which the React Compiler cannot memoize safely.
  const selectedRole = useWatch({ control, name: "role" });
  const password = useWatch({ control, name: "password" }) ?? "";

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      const user = await createAccount(values);
      toast({
        tone: "success",
        title: `Welcome, ${user.companyName}`,
        description:
          user.role === "Buyer"
            ? "Publish your first requirement to start receiving quotations."
            : "Browse open requirements and submit your first quotation.",
      });
      router.replace(homePathFor(user.role));
    } catch (error) {
      // The API is the authority on validation; surface its messages on the right inputs.
      setFormError(
        applyApiErrors(error, values, setError, "Could not create your account. Please try again."),
      );
    }
  });

  return (
    <AuthShell
      title="Create your account"
      subtitle="Tell us how you'll use the marketplace."
      wide
      footer={
        <>
          Already registered?{" "}
          <Link href="/login" className="font-medium text-brand hover:underline">
            Sign in
          </Link>
        </>
      }
    >
      <form onSubmit={onSubmit} noValidate className="space-y-5">
        {formError && <FormError message={formError} />}

        <fieldset>
          <legend className="mb-2.5 block text-sm font-medium text-ink">I am a</legend>

          <div className="grid gap-2.5 sm:grid-cols-2">
            {roleOptions.map((option) => {
              const active = selectedRole === option.value;
              const OptionIcon = option.icon;

              return (
                <label
                  key={option.value}
                  className={clsx(
                    "relative cursor-pointer rounded-field border p-3.5 transition-[border-color,background-color,box-shadow] duration-[120ms]",
                    active
                      ? "border-brand bg-brand-softer shadow-[0_0_0_3px_var(--color-brand-soft)]"
                      : "border-line-strong bg-surface hover:border-ink-subtle hover:bg-canvas",
                  )}
                >
                  <input
                    type="radio"
                    value={option.value}
                    checked={active}
                    onChange={() => setValue("role", option.value, { shouldValidate: true })}
                    className="sr-only"
                    name="role"
                  />

                  <span className="flex items-start gap-3">
                    <span
                      className={clsx(
                        "flex size-9 shrink-0 items-center justify-center rounded-field transition-colors",
                        active ? "bg-brand text-white" : "bg-canvas-sunken text-ink-muted",
                      )}
                    >
                      <OptionIcon className="size-4.5" />
                    </span>

                    <span className="min-w-0">
                      <span
                        className={clsx(
                          "block text-sm font-medium",
                          active ? "text-brand" : "text-ink",
                        )}
                      >
                        {option.title}
                      </span>
                      <span className="mt-0.5 block text-xs leading-snug text-ink-muted">
                        {option.description}
                      </span>
                    </span>
                  </span>
                </label>
              );
            })}
          </div>

          {errors.role && (
            <p role="alert" className="mt-1.5 text-sm text-danger">
              {errors.role.message}
            </p>
          )}
        </fieldset>

        <div className="grid gap-5 sm:grid-cols-2">
          <Field label="Your name" htmlFor="name" error={errors.name?.message} required>
            <Input
              id="name"
              autoComplete="name"
              placeholder="Rahul Mehta"
              invalid={!!errors.name}
              {...register("name")}
            />
          </Field>

          <Field
            label="Company name"
            htmlFor="companyName"
            error={errors.companyName?.message}
            required
          >
            <Input
              id="companyName"
              autoComplete="organization"
              placeholder="Meridian Office Solutions"
              leadingIcon={<BuildingIcon className="size-4" />}
              invalid={!!errors.companyName}
              {...register("companyName")}
            />
          </Field>
        </div>

        <Field
          label="Email"
          htmlFor="email"
          error={errors.email?.message}
          hint="Shown to the other side of the marketplace alongside your company name."
          required
        >
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
            autoComplete="new-password"
            placeholder="••••••••"
            leadingIcon={<LockIcon className="size-4" />}
            invalid={!!errors.password}
            {...register("password")}
          />

          <ul className="flex flex-wrap gap-x-4 gap-y-1 pt-0.5">
            {passwordRules.map((rule) => {
              const met = rule.test(password);

              return (
                <li
                  key={rule.label}
                  className={clsx(
                    "flex items-center gap-1.5 text-xs transition-colors",
                    met ? "text-positive" : "text-ink-subtle",
                  )}
                >
                  <span
                    className={clsx(
                      "flex size-3.5 items-center justify-center rounded-full transition-colors",
                      met ? "bg-positive text-white" : "bg-canvas-sunken",
                    )}
                    aria-hidden="true"
                  >
                    {met && <CheckIcon className="size-2.5" strokeWidth={3} />}
                  </span>
                  {rule.label}
                </li>
              );
            })}
          </ul>
        </Field>

        <Button type="submit" size="lg" loading={isSubmitting} className="w-full">
          Create account
        </Button>
      </form>
    </AuthShell>
  );
}
