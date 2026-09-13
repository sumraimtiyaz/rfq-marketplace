"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { BackLink, ErrorState, LoadingState, PageHeader, useToast } from "@/components/ui";
import { RfqForm } from "@/components/rfq/RfqForm";
import { ApiError } from "@/lib/api";
import { useRfq, useUpdateRfq } from "@/lib/queries";
import { isRfqClosed } from "@/lib/rfq-status";
import type { RfqValues } from "@/lib/validations";

export default function EditRfqPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { toast } = useToast();

  const { data: rfq, isPending, isError, error, refetch } = useRfq(id);
  const updateRfq = useUpdateRfq(id);

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
      </div>
    );
  }

  // Closing an RFQ freezes its terms, so suppliers who already quoted are not quoted against
  // a moving target. The API enforces this too.
  if (isRfqClosed(rfq.status)) {
    return (
      <div className="mx-auto max-w-lg">
        <ErrorState
          title="This RFQ is closed"
          message="Reopen it before making changes, so suppliers are always quoting against current terms."
        />
        <div className="mt-5 text-center">
          <Link href={`/buyer/rfqs/${id}`} className="text-sm font-medium text-brand hover:underline">
            Back to RFQ
          </Link>
        </div>
      </div>
    );
  }

  const handleSubmit = async (values: RfqValues) => {
    const updated = await updateRfq.mutateAsync(values);
    toast({
      tone: "success",
      title: "Changes saved",
      description: "Suppliers now see the updated requirement.",
    });
    return updated;
  };

  return (
    <div className="mx-auto max-w-3xl">
      <PageHeader
        title="Edit RFQ"
        description="Suppliers who have already quoted will see the updated requirement."
        breadcrumb={<BackLink href={`/buyer/rfqs/${id}`}>Back to RFQ</BackLink>}
      />

      <RfqForm
        initial={rfq}
        submitLabel="Save changes"
        onSubmit={handleSubmit}
        onCancel={() => router.push(`/buyer/rfqs/${id}`)}
      />
    </div>
  );
}
