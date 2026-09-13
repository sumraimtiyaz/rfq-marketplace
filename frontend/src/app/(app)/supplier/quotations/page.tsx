"use client";

import Link from "next/link";
import {
  Badge,
  Card,
  EmptyState,
  ErrorState,
  LinkButton,
  PageHeader,
  SkeletonRows,
} from "@/components/ui";
import {
  CalendarIcon,
  ClockIcon,
  InboxIcon,
  SearchIcon,
  TagIcon,
  TruckIcon,
} from "@/components/ui/icons";
import { ApiError } from "@/lib/api";
import { formatCurrency, formatDate, formatDateTime, pluralise } from "@/lib/format";
import { useMyQuotations } from "@/lib/queries";
import { isRfqClosed, rfqStatusLabel } from "@/lib/rfq-status";

export default function MyQuotationsPage() {
  const { data, isPending, isError, error, refetch } = useMyQuotations();

  return (
    <>
      <PageHeader
        title="My quotations"
        description="Every quotation you have submitted, newest first."
        actions={
          <LinkButton
            href="/supplier/rfqs"
            variant="secondary"
            size="sm"
            icon={<SearchIcon className="size-4" />}
          >
            Browse RFQs
          </LinkButton>
        }
      />

      {isPending ? (
        <SkeletonRows rows={3} />
      ) : isError ? (
        <ErrorState
          title="Couldn't load your quotations"
          message={error instanceof ApiError ? error.detail : undefined}
          onRetry={() => void refetch()}
        />
      ) : data.length === 0 ? (
        <EmptyState
          icon={<InboxIcon className="size-6" />}
          title="You haven't quoted yet"
          description="Find an open requirement you can fulfil and send the buyer your price."
          action={
            <LinkButton href="/supplier/rfqs" icon={<SearchIcon className="size-4" />}>
              Browse open RFQs
            </LinkButton>
          }
        />
      ) : (
        <>
          <p className="mb-4 text-sm text-ink-muted">{pluralise(data.length, "quotation")} submitted</p>

          <ul className="stagger space-y-3">
            {data.map((quotation) => (
              <li key={quotation.id}>
                <Card interactive>
                  <Link href={`/supplier/rfqs/${quotation.rfqId}`} className="block rounded-card p-5">
                    <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
                      <div className="min-w-0">
                        <h2 className="truncate text-base font-semibold text-ink">
                          {quotation.rfqProductName}
                        </h2>
                        <p className="mt-0.5 truncate text-sm text-ink-muted">
                          {quotation.buyerCompanyName} · {quotation.rfqDeliveryLocation}
                        </p>
                      </div>

                      <Badge tone={isRfqClosed(quotation.rfqStatus) ? "neutral" : "positive"} dot>
                        RFQ {rfqStatusLabel(quotation.rfqStatus).toLowerCase()}
                      </Badge>
                    </div>

                    <dl className="mt-5 grid grid-cols-2 gap-x-4 gap-y-4 sm:grid-cols-4">
                      <div>
                        <dt className="flex items-center gap-1.5 text-xs text-ink-subtle">
                          <TagIcon className="size-3.5" />
                          Your price
                        </dt>
                        <dd className="mt-1 text-sm font-semibold tabular-nums text-ink">
                          {formatCurrency(quotation.quotedPrice)}
                        </dd>
                      </div>

                      <div>
                        <dt className="flex items-center gap-1.5 text-xs text-ink-subtle">
                          <TruckIcon className="size-3.5" />
                          Delivery
                        </dt>
                        <dd className="mt-1 text-sm font-medium tabular-nums text-ink">
                          {quotation.estimatedDeliveryDays}{" "}
                          {quotation.estimatedDeliveryDays === 1 ? "day" : "days"}
                        </dd>
                      </div>

                      <div>
                        <dt className="flex items-center gap-1.5 text-xs text-ink-subtle">
                          <CalendarIcon className="size-3.5" />
                          RFQ deadline
                        </dt>
                        <dd className="mt-1 text-sm font-medium text-ink">
                          {formatDate(quotation.rfqDeadline)}
                        </dd>
                      </div>

                      <div>
                        <dt className="flex items-center gap-1.5 text-xs text-ink-subtle">
                          <ClockIcon className="size-3.5" />
                          Submitted
                        </dt>
                        <dd className="mt-1 text-sm font-medium text-ink">
                          {formatDateTime(quotation.createdAt)}
                        </dd>
                      </div>
                    </dl>

                    {quotation.message && (
                      <p className="mt-4 line-clamp-2 border-t border-line pt-3.5 text-sm text-ink-muted">
                        {quotation.message}
                      </p>
                    )}
                  </Link>
                </Card>
              </li>
            ))}
          </ul>
        </>
      )}
    </>
  );
}
