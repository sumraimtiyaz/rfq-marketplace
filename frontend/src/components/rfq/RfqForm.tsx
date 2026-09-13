"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import clsx from "clsx";
import { Button, Card, Field, FormError, Input, Textarea } from "@/components/ui";
import { CalendarIcon, MapPinIcon, PackageIcon } from "@/components/ui/icons";
import { applyApiErrors } from "@/lib/forms";
import { todayIsoDate } from "@/lib/format";
import { rfqSchema, type RfqValues } from "@/lib/validations";
import type { RfqDetail } from "@/types";

const DESCRIPTION_MAX = 5000;

/** Shared by create and edit; the only difference is the initial values and the button label. */
export function RfqForm({
  initial,
  submitLabel,
  onSubmit,
  onCancel,
}: {
  initial?: RfqDetail;
  submitLabel: string;
  onSubmit: (values: RfqValues) => Promise<RfqDetail>;
  onCancel: () => void;
}) {
  const router = useRouter();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RfqValues>({
    resolver: zodResolver(rfqSchema),
    defaultValues: {
      productName: initial?.productName ?? "",
      description: initial?.description ?? "",
      quantity: initial?.quantity ?? ("" as unknown as number),
      deliveryLocation: initial?.deliveryLocation ?? "",
      deadline: initial?.deadline?.slice(0, 10) ?? "",
    },
  });

  // Subscribed per-field so the counter updates without re-rendering the whole form.
  const description = useWatch({ control, name: "description" }) ?? "";
  const remaining = DESCRIPTION_MAX - description.length;

  const submit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      const rfq = await onSubmit(values);
      router.push(`/buyer/rfqs/${rfq.id}`);
    } catch (error) {
      setFormError(applyApiErrors(error, values, setError, "Could not save this RFQ. Please try again."));
    }
  });

  return (
    <Card className="animate-rise-in overflow-hidden">
      <form onSubmit={submit} noValidate>
        <div className="space-y-6 p-5 sm:p-6">
          {formError && <FormError message={formError} />}

          {/* What is being bought */}
          <section className="space-y-5">
            <Field
              label="Product or service"
              htmlFor="productName"
              error={errors.productName?.message}
              required
            >
              <Input
                id="productName"
                placeholder="Ergonomic office chairs"
                autoComplete="off"
                invalid={!!errors.productName}
                {...register("productName")}
              />
            </Field>

            <Field
              label="Description"
              htmlFor="description"
              error={errors.description?.message}
              hint="Specifications, quality standards, certifications — anything a supplier needs to price accurately."
              required
            >
              <Textarea
                id="description"
                rows={6}
                maxLength={DESCRIPTION_MAX}
                placeholder="Mesh-back ergonomic chairs with adjustable lumbar support and a 5-year warranty…"
                invalid={!!errors.description}
                {...register("description")}
              />
              {/* Only appears once it could plausibly matter, rather than nagging from an empty field. */}
              {description.length > DESCRIPTION_MAX * 0.8 && (
                <p
                  className={clsx(
                    "text-right text-xs tabular-nums",
                    remaining < 100 ? "text-warning" : "text-ink-subtle",
                  )}
                >
                  {remaining.toLocaleString()} characters left
                </p>
              )}
            </Field>
          </section>

          {/* Commercial terms */}
          <section className="space-y-5 border-t border-line pt-6">
            <div className="grid gap-5 sm:grid-cols-2">
              <Field label="Quantity" htmlFor="quantity" error={errors.quantity?.message} required>
                <Input
                  id="quantity"
                  type="number"
                  min={1}
                  step={1}
                  inputMode="numeric"
                  placeholder="500"
                  leadingIcon={<PackageIcon className="size-4" />}
                  invalid={!!errors.quantity}
                  {...register("quantity")}
                />
              </Field>

              <Field
                label="Delivery location"
                htmlFor="deliveryLocation"
                error={errors.deliveryLocation?.message}
                required
              >
                <Input
                  id="deliveryLocation"
                  placeholder="Ahmedabad, Gujarat"
                  autoComplete="off"
                  leadingIcon={<MapPinIcon className="size-4" />}
                  invalid={!!errors.deliveryLocation}
                  {...register("deliveryLocation")}
                />
              </Field>
            </div>

            <Field
              label="Deadline"
              htmlFor="deadline"
              error={errors.deadline?.message}
              hint="The last day suppliers can submit a quotation."
              required
            >
              <Input
                id="deadline"
                type="date"
                min={todayIsoDate()}
                leadingIcon={<CalendarIcon className="size-4" />}
                invalid={!!errors.deadline}
                className="sm:max-w-64"
                {...register("deadline")}
              />
            </Field>
          </section>
        </div>

        {/* Actions sit on a tinted footer so the form has a clear end. */}
        <div className="flex flex-col-reverse gap-2 border-t border-line bg-canvas-sunken/50 px-5 py-4 sm:flex-row sm:justify-end sm:px-6">
          <Button type="button" variant="secondary" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {submitLabel}
          </Button>
        </div>
      </form>
    </Card>
  );
}
