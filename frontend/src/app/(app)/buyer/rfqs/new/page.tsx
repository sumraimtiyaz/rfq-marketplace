"use client";

import { useRouter } from "next/navigation";
import { BackLink, PageHeader, useToast } from "@/components/ui";
import { RfqForm } from "@/components/rfq/RfqForm";
import { useCreateRfq } from "@/lib/queries";
import type { RfqValues } from "@/lib/validations";

export default function CreateRfqPage() {
  const router = useRouter();
  const createRfq = useCreateRfq();
  const { toast } = useToast();

  // The form navigates to the new RFQ on success, so the confirmation has to be a toast:
  // anything rendered here would unmount during that navigation.
  const handleSubmit = async (values: RfqValues) => {
    const rfq = await createRfq.mutateAsync(values);
    toast({
      tone: "success",
      title: "RFQ published",
      description: `Suppliers can now see "${rfq.productName}" and send you quotations.`,
    });
    return rfq;
  };

  return (
    <div className="mx-auto max-w-3xl">
      <PageHeader
        title="Create an RFQ"
        description="Describe what you need. Suppliers will see it and respond with prices and lead times."
        breadcrumb={<BackLink href="/buyer/rfqs">My RFQs</BackLink>}
      />
      <RfqForm
        submitLabel="Publish RFQ"
        onSubmit={handleSubmit}
        onCancel={() => router.push("/buyer/rfqs")}
      />
    </div>
  );
}
