import Link from "next/link";
import type { ReactNode } from "react";
import clsx from "clsx";
import { Badge, Card } from "@/components/ui";
import { CalendarIcon, CheckIcon, ClockIcon, MapPinIcon, PackageIcon } from "@/components/ui/icons";
import { describeDeadline, formatDate, formatNumber, pluralise } from "@/lib/format";
import { isRfqClosed } from "@/lib/rfq-status";
import type { RfqListItem, RfqStatusValue } from "@/types";

export function StatusBadge({ status, isExpired }: { status: RfqStatusValue; isExpired: boolean }) {
  if (isRfqClosed(status)) {
    return (
      <Badge tone="neutral" dot>
        Closed
      </Badge>
    );
  }

  if (isExpired) {
    return (
      <Badge tone="warning" dot>
        Deadline passed
      </Badge>
    );
  }

  return (
    <Badge tone="positive" dot>
      Open
    </Badge>
  );
}

/** One labelled fact, with an icon so the eye can find it without reading the label. */
function DetailItem({
  icon,
  label,
  value,
  valueClassName,
}: {
  icon: ReactNode;
  label: string;
  value: string;
  valueClassName?: string;
}) {
  return (
    <div className="min-w-0">
      <dt className="flex items-center gap-1.5 text-xs text-ink-subtle">
        {icon}
        {label}
      </dt>
      <dd className={clsx("mt-1 truncate text-sm font-medium text-ink", valueClassName)}>{value}</dd>
    </div>
  );
}

/**
 * One RFQ in a list. `variant` decides which side of the marketplace is looking: buyers see
 * how many quotations arrived, suppliers see whether they have already responded.
 */
export function RfqCard({
  rfq,
  href,
  variant,
}: {
  rfq: RfqListItem;
  href: string;
  variant: "buyer" | "supplier";
}) {
  const deadline = describeDeadline(rfq.deadline);

  return (
    <Card interactive>
      <Link href={href} className="block rounded-card p-5">
        <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
          <div className="min-w-0 flex-1">
            <h3 className="truncate text-base font-semibold text-ink">{rfq.productName}</h3>
            {variant === "supplier" && (
              <p className="mt-0.5 truncate text-sm text-ink-muted">{rfq.buyerCompanyName}</p>
            )}
          </div>

          <div className="flex flex-wrap items-center gap-1.5">
            {variant === "supplier" && rfq.hasQuoted && (
              <Badge tone="brand" icon={<CheckIcon className="size-3" />}>
                Quoted
              </Badge>
            )}
            {variant === "buyer" && rfq.quotationCount > 0 && (
              <Badge tone="brand">{pluralise(rfq.quotationCount, "quotation")}</Badge>
            )}
            <StatusBadge status={rfq.status} isExpired={rfq.isExpired} />
          </div>
        </div>

        <dl className="mt-5 grid grid-cols-2 gap-x-4 gap-y-4 sm:grid-cols-4">
          <DetailItem
            icon={<PackageIcon className="size-3.5" />}
            label="Quantity"
            value={formatNumber(rfq.quantity)}
          />
          <DetailItem
            icon={<MapPinIcon className="size-3.5" />}
            label="Delivery to"
            value={rfq.deliveryLocation}
          />
          <DetailItem
            icon={<CalendarIcon className="size-3.5" />}
            label="Deadline"
            value={formatDate(rfq.deadline)}
          />
          <DetailItem
            icon={<ClockIcon className="size-3.5" />}
            label="Time left"
            value={deadline.label}
            valueClassName={
              deadline.past ? "text-ink-subtle" : deadline.urgent ? "text-warning" : undefined
            }
          />
        </dl>

        {variant === "buyer" && rfq.quotationCount === 0 && !isRfqClosed(rfq.status) && !rfq.isExpired && (
          <p className="mt-4 border-t border-line pt-3.5 text-sm text-ink-subtle">
            No quotations yet — suppliers can see this requirement.
          </p>
        )}
      </Link>
    </Card>
  );
}
