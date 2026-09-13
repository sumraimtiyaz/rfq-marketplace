"use client";

import { useState } from "react";
import {
  EmptyState,
  ErrorState,
  LinkButton,
  PageHeader,
  Select,
  SkeletonRows,
  Stat,
} from "@/components/ui";
import { CheckCircleIcon, DocumentIcon, InboxIcon, PlusIcon } from "@/components/ui/icons";
import { Pagination } from "@/components/rfq/Pagination";
import { RfqCard } from "@/components/rfq/RfqCard";
import { useBuyerDashboard, useMyRfqs } from "@/lib/queries";
import { ApiError } from "@/lib/api";
import { formatNumber } from "@/lib/format";
import type { RfqQuery, RfqStatus } from "@/types";

const PAGE_SIZE = 10;

export default function BuyerRfqsPage() {
  const [statusFilter, setStatusFilter] = useState<RfqStatus | "">("");
  const [page, setPage] = useState(1);

  const query: RfqQuery = {
    page,
    pageSize: PAGE_SIZE,
    status: statusFilter || undefined,
    sortBy: "createdAt",
  };

  const dashboard = useBuyerDashboard();
  const { data, isPending, isError, error, refetch, isFetching } = useMyRfqs(query);

  const changeStatus = (value: string) => {
    setStatusFilter(value as RfqStatus | "");
    setPage(1); // a filter change invalidates the current page number
  };

  return (
    <>
      <PageHeader
        title="My RFQs"
        description="Requirements you have published, and the quotations they have attracted."
        actions={
          <LinkButton href="/buyer/rfqs/new" icon={<PlusIcon className="size-4" />}>
            Create RFQ
          </LinkButton>
        }
      />

      <div className="mb-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Stat
          label="Total RFQs"
          value={formatNumber(dashboard.data?.totalRfqs ?? 0)}
          loading={dashboard.isPending}
          icon={<DocumentIcon className="size-4.5" />}
        />
        <Stat
          label="Open"
          value={formatNumber(dashboard.data?.openRfqs ?? 0)}
          loading={dashboard.isPending}
          tone="positive"
          icon={<CheckCircleIcon className="size-4.5" />}
        />
        <Stat
          label="Closed"
          value={formatNumber(dashboard.data?.closedRfqs ?? 0)}
          loading={dashboard.isPending}
          icon={<DocumentIcon className="size-4.5" />}
        />
        <Stat
          label="Quotations received"
          value={formatNumber(dashboard.data?.quotationsReceived ?? 0)}
          hint="Across all your RFQs"
          loading={dashboard.isPending}
          tone="brand"
          icon={<InboxIcon className="size-4.5" />}
        />
      </div>

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <label htmlFor="status" className="text-sm text-ink-muted">
          Status
        </label>
        <Select
          id="status"
          value={statusFilter}
          onChange={(event) => changeStatus(event.target.value)}
          className="w-40"
        >
          <option value="">All</option>
          <option value="Open">Open</option>
          <option value="Closed">Closed</option>
        </Select>

        {/* Only while refetching an already-visible list; the first load shows skeletons instead. */}
        {isFetching && !isPending && (
          <span className="flex items-center gap-1.5 text-sm text-ink-subtle">
            <span className="size-1.5 animate-pulse rounded-full bg-brand" />
            Updating
          </span>
        )}
      </div>

      {isPending ? (
        <SkeletonRows rows={3} />
      ) : isError ? (
        <ErrorState
          title="Couldn't load your RFQs"
          message={error instanceof ApiError ? error.detail : undefined}
          onRetry={() => void refetch()}
        />
      ) : data.items.length === 0 ? (
        <EmptyState
          icon={<DocumentIcon className="size-6" />}
          title={statusFilter ? `No ${statusFilter.toLowerCase()} RFQs` : "You haven't posted an RFQ yet"}
          description={
            statusFilter
              ? "Try a different status filter to see more."
              : "Publish a requirement and suppliers will start sending you quotations."
          }
          action={
            !statusFilter && (
              <LinkButton href="/buyer/rfqs/new" icon={<PlusIcon className="size-4" />}>
                Create your first RFQ
              </LinkButton>
            )
          }
        />
      ) : (
        <div className="space-y-4">
          <div className="stagger space-y-3">
            {data.items.map((rfq) => (
              <RfqCard key={rfq.id} rfq={rfq} href={`/buyer/rfqs/${rfq.id}`} variant="buyer" />
            ))}
          </div>
          <Pagination page={data} onPageChange={setPage} />
        </div>
      )}
    </>
  );
}
