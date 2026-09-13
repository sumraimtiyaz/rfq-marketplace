"use client";

import { useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import {
  BackLink,
  Button,
  ConfirmDialog,
  EmptyState,
  ErrorState,
  FormError,
  LinkButton,
  LoadingState,
  Notice,
  PageHeader,
  SkeletonRows,
  useToast,
} from "@/components/ui";
import { InboxIcon, PencilIcon, TrashIcon } from "@/components/ui/icons";
import { RfqSummary } from "@/components/rfq/RfqSummary";
import { QuotationList } from "@/components/quotation/QuotationList";
import { ApiError } from "@/lib/api";
import { pluralise } from "@/lib/format";
import { isRfqClosed } from "@/lib/rfq-status";
import { useDeleteRfq, useRfq, useRfqQuotations, useSetRfqStatus } from "@/lib/queries";

export default function BuyerRfqDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { toast } = useToast();

  const rfqQuery = useRfq(id);
  const quotationsQuery = useRfqQuotations(id, rfqQuery.isSuccess);
  const setStatus = useSetRfqStatus(id);
  const deleteRfq = useDeleteRfq();

  const [confirmDelete, setConfirmDelete] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  if (rfqQuery.isPending) {
    return <LoadingState label="Loading RFQ…" />;
  }

  if (rfqQuery.isError) {
    const error = rfqQuery.error;
    const isApi = error instanceof ApiError;
    const isTerminal = isApi && (error.isNotFound || error.isForbidden);

    return (
      <div className="mx-auto max-w-lg">
        <ErrorState
          title={
            isApi && error.isNotFound
              ? "RFQ not found"
              : isApi && error.isForbidden
                ? "You don't have access to this RFQ"
                : "Couldn't load this RFQ"
          }
          message={isApi ? error.detail : undefined}
          onRetry={isTerminal ? undefined : () => void rfqQuery.refetch()}
        />
        <div className="mt-5 text-center">
          <Link href="/buyer/rfqs" className="text-sm font-medium text-brand hover:underline">
            Back to my RFQs
          </Link>
        </div>
      </div>
    );
  }

  const rfq = rfqQuery.data;
  const isClosed = isRfqClosed(rfq.status);

  const runStatusChange = async (action: "close" | "reopen") => {
    setActionError(null);
    try {
      await setStatus.mutateAsync(action);
      toast({
        tone: "success",
        title: action === "close" ? "RFQ closed" : "RFQ reopened",
        description:
          action === "close"
            ? "It no longer appears to suppliers and cannot receive new quotations."
            : "Suppliers can see it again and submit quotations.",
      });
    } catch (error) {
      setActionError(
        error instanceof ApiError ? error.detail : "Could not update this RFQ. Please try again.",
      );
    }
  };

  const runDelete = async () => {
    setActionError(null);
    try {
      await deleteRfq.mutateAsync(id);
      toast({
        tone: "success",
        title: "RFQ deleted",
        description: `"${rfq.productName}" and its quotations have been removed.`,
      });
      router.push("/buyer/rfqs");
    } catch (error) {
      setConfirmDelete(false);
      setActionError(
        error instanceof ApiError ? error.detail : "Could not delete this RFQ. Please try again.",
      );
    }
  };

  return (
    <>
      <PageHeader
        title="RFQ details"
        breadcrumb={<BackLink href="/buyer/rfqs">My RFQs</BackLink>}
        actions={
          <>
            {!isClosed && (
              <LinkButton
                href={`/buyer/rfqs/${id}/edit`}
                variant="secondary"
                size="sm"
                icon={<PencilIcon className="size-4" />}
              >
                Edit
              </LinkButton>
            )}
            <Button
              variant="secondary"
              size="sm"
              onClick={() => void runStatusChange(isClosed ? "reopen" : "close")}
              loading={setStatus.isPending}
            >
              {isClosed ? "Reopen" : "Close"}
            </Button>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setConfirmDelete(true)}
              icon={<TrashIcon className="size-4" />}
              className="text-danger hover:border-danger-line hover:bg-danger-soft"
            >
              Delete
            </Button>
          </>
        }
      />

      {actionError && (
        <div className="mb-4">
          <FormError message={actionError} />
        </div>
      )}

      <RfqSummary rfq={rfq} showBuyer={false} />

      {isClosed && (
        <div className="mt-3">
          <Notice tone="info" title="This RFQ is closed">
            It no longer appears to suppliers and cannot receive new quotations. Reopen it to start
            accepting quotations again.
          </Notice>
        </div>
      )}

      <section className="mt-8">
        <div className="mb-4 flex items-baseline gap-2.5">
          <h2 className="text-lg font-semibold text-ink">Quotations received</h2>
          {quotationsQuery.isSuccess && quotationsQuery.data.length > 0 && (
            <span className="text-sm text-ink-muted">
              {pluralise(quotationsQuery.data.length, "quotation")}
            </span>
          )}
        </div>

        {quotationsQuery.isPending ? (
          <SkeletonRows rows={2} />
        ) : quotationsQuery.isError ? (
          <ErrorState
            title="Couldn't load quotations"
            message={
              quotationsQuery.error instanceof ApiError ? quotationsQuery.error.detail : undefined
            }
            onRetry={() => void quotationsQuery.refetch()}
          />
        ) : quotationsQuery.data.length === 0 ? (
          <EmptyState
            icon={<InboxIcon className="size-6" />}
            title="No quotations yet"
            description={
              rfq.isExpired
                ? "The deadline passed without any supplier responding."
                : isClosed
                  ? "This RFQ was closed before any supplier responded."
                  : "Suppliers can see this requirement. Quotations will appear here as they arrive."
            }
          />
        ) : (
          <QuotationList quotations={quotationsQuery.data} />
        )}
      </section>

      <ConfirmDialog
        open={confirmDelete}
        title="Delete this RFQ?"
        description="This permanently removes the RFQ and every quotation suppliers have submitted for it. This cannot be undone."
        confirmLabel="Delete RFQ"
        destructive
        loading={deleteRfq.isPending}
        onConfirm={() => void runDelete()}
        onCancel={() => setConfirmDelete(false)}
      />
    </>
  );
}
