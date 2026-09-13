"use client";

import { useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Card, Field, FormError, Input, Notice, Textarea, useToast } from "@/components/ui";
import { TruckIcon } from "@/components/ui/icons";
import { applyApiErrors } from "@/lib/forms";
import { formatCurrency } from "@/lib/format";
import { useSubmitQuotation } from "@/lib/queries";
import { quotationSchema, type QuotationValues } from "@/lib/validations";

const MESSAGE_MAX = 2000;

/**
 * `onSubmitted` is a callback rather than local state because submitting invalidates the RFQ
 * query: `hasQuoted` flips to true, the parent swaps this form out, and any success state held
 * here would be unmounted before the user could read it.
 */
export function QuotationForm({
  rfqId,
  onSubmitted,
}: {
  rfqId: string;
  onSubmitted: () => void;
}) {
  const submitQuotation = useSubmitQuotation(rfqId);
  const { toast } = useToast();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<QuotationValues>({
    resolver: zodResolver(quotationSchema),
    defaultValues: {
      quotedPrice: "" as unknown as number,
      estimatedDeliveryDays: "" as unknown as number,
      message: "",
    },
  });

  // Echoing the typed figure back as formatted currency catches a misplaced zero
  // before it is submitted, which is the one mistake here that cannot be undone.
  const rawPrice = useWatch({ control, name: "quotedPrice" });
  const parsedPrice = Number(rawPrice);
  const pricePreview =
    Number.isFinite(parsedPrice) && parsedPrice > 0 ? formatCurrency(parsedPrice) : null;

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await submitQuotation.mutateAsync(values);
      toast({
        tone: "success",
        title: "Quotation submitted",
        description: "The buyer can now see your price and lead time.",
      });
      onSubmitted();
    } catch (error) {
      setFormError(
        applyApiErrors(error, values, setError, "Could not submit your quotation. Please try again."),
      );
    }
  });

  return (
    <Card className="animate-rise-in overflow-hidden">
      <div className="border-b border-line p-5 sm:p-6">
        <h2 className="text-lg font-semibold text-ink">Submit your quotation</h2>
        <p className="mt-1 text-sm text-ink-muted">
          One quotation per RFQ, so make it your best offer.
        </p>
      </div>

      <form onSubmit={onSubmit} noValidate>
        <div className="space-y-5 p-5 sm:p-6">
          {formError && <FormError message={formError} />}

          <Notice tone="warning">
            A submitted quotation cannot be edited or withdrawn. Check your figures before sending.
          </Notice>

          <div className="grid gap-5 sm:grid-cols-2">
            <Field
              label="Quoted price"
              htmlFor="quotedPrice"
              error={errors.quotedPrice?.message}
              hint={pricePreview ? `That's ${pricePreview} for the full quantity.` : "Total price for the full quantity."}
              required
            >
              <Input
                id="quotedPrice"
                type="number"
                min={0.01}
                step="0.01"
                inputMode="decimal"
                placeholder="1250000"
                prefix="₹"
                invalid={!!errors.quotedPrice}
                {...register("quotedPrice")}
              />
            </Field>

            <Field
              label="Estimated delivery (days)"
              htmlFor="estimatedDeliveryDays"
              error={errors.estimatedDeliveryDays?.message}
              hint="Working days from order confirmation."
              required
            >
              <Input
                id="estimatedDeliveryDays"
                type="number"
                min={1}
                step={1}
                inputMode="numeric"
                placeholder="20"
                leadingIcon={<TruckIcon className="size-4" />}
                invalid={!!errors.estimatedDeliveryDays}
                {...register("estimatedDeliveryDays")}
              />
            </Field>
          </div>

          <Field
            label="Message"
            htmlFor="message"
            error={errors.message?.message}
            hint="Optional. Warranty, certifications, payment terms — anything that sets your offer apart."
          >
            <Textarea
              id="message"
              rows={4}
              maxLength={MESSAGE_MAX}
              placeholder="We can supply premium ergonomic chairs with a 5-year warranty and free installation."
              invalid={!!errors.message}
              {...register("message")}
            />
          </Field>
        </div>

        <div className="flex justify-end border-t border-line bg-canvas-sunken/50 px-5 py-4 sm:px-6">
          <Button type="submit" loading={isSubmitting}>
            Submit quotation
          </Button>
        </div>
      </form>
    </Card>
  );
}
