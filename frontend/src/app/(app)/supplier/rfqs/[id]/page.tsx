"use client";

import { useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import {
  BackLink,
  Card,
  ErrorState,
  LinkButton,
  LoadingState,
  PageHeader,
  SuccessNote,
} from "@/components/ui";
import { CheckCircleIcon, ClockIcon, LockIcon } from "@/components/ui/icons";
import { RfqSummary } from "@/components/rfq/RfqSummary";
import { QuotationForm } from "@/components/quotation/QuotationForm";
import { ApiError } from "@/lib/api";
import { useRfq } from "@/lib/queries";
import { isRfqClosed } from "@/lib/rfq-status";
import type { ReactNode } from "react";

/** Explains why the quotation form is not available, when it is not. */
function NotAcceptingNotice({
  icon,
  title,
  description,
}: {
  icon: ReactNode;
  title: string;
  description: string;
}) {
  return (
    <Card className="animate-rise-in p-5 sm:p-6">
      <div className="flex gap-4">
        <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-canvas-sunken text-ink-muted">
          {icon}
        </div>
        <div className="min-w-0">
          <h2 className="text-base font-semibold text-ink">{title}</h2>
          <p className="mt-1 text-sm text-ink-muted">{description}</p>
          <div className="mt-5">
            <LinkButton href="/supplier/rfqs" variant="secondary" size="sm">
              Browse other RFQs
            </LinkButton>
          </div>
        </div>
      </div>
    </Card>
  );
}

export default function SupplierRfqDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: rfq, isPending, isError, error, refetch } = useRfq(id);

  // Held here, not in the form: submitting flips hasQuoted, which swaps the form out.
  const [justSubmitted, setJustSubmitted] = useState(false);

  if (isPending) {
    return <LoadingState label="Loading RFQ…" />;
  }

  if (isError) {
    const isApi = error instanceof ApiError;
    return (
      <div className="mx-auto max-w-lg">
        <ErrorState
          title={isApi && error.isNotFound ? "RFQ not found" : "Couldn't load this RFQ"}
          message={isApi ? error.detail : undefined}
          onRetry={isApi && error.isNotFound ? undefined : () => void refetch()}
        />
        <div className="mt-5 text-center">
          <Link href="/supplier/rfqs" className="text-sm font-medium text-brand hover:underline">
            Back to browse
          </Link>
        </div>
      </div>
    );
  }

  return (
    <>
      <PageHeader
        title="Requirement details"
        breadcrumb={<BackLink href="/supplier/rfqs">Browse RFQs</BackLink>}
      />

      <RfqSummary rfq={rfq} showBuyer />

      <section className="mt-6">
        {justSubmitted ? (
          <Card className="animate-rise-in p-5 sm:p-6">
            <SuccessNote>
              Your quotation has been sent. The buyer can now see your price and lead time.
            </SuccessNote>
            <p className="mt-4 text-sm text-ink-muted">
              You can review it any time under{" "}
              <Link
                href="/supplier/quotations"
                className="font-medium text-brand hover:underline"
              >
                My quotations
              </Link>
              .
            </p>
          </Card>
        ) : rfq.hasQuoted ? (
          <NotAcceptingNotice
            icon={<CheckCircleIcon className="size-5" />}
            title="You have already quoted for this RFQ"
            description="Each supplier may submit one quotation per requirement. Yours is with the buyer — you can review it under My quotations."
          />
        ) : isRfqClosed(rfq.status) ? (
          <NotAcceptingNotice
            icon={<LockIcon className="size-5" />}
            title="This RFQ is closed"
            description="The buyer has stopped accepting quotations for this requirement."
          />
        ) : rfq.isExpired ? (
          <NotAcceptingNotice
            icon={<ClockIcon className="size-5" />}
            title="The deadline has passed"
            description="This requirement stopped accepting quotations on its deadline date."
          />
        ) : (
          <QuotationForm rfqId={rfq.id} onSubmitted={() => setJustSubmitted(true)} />
        )}
      </section>
    </>
  );
}
