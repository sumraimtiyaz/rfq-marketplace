import clsx from "clsx";
import type { ReactNode } from "react";
import { Card } from "@/components/ui";
import { StatusBadge } from "@/components/rfq/RfqCard";
import { BuildingIcon, CalendarIcon, MapPinIcon, PackageIcon } from "@/components/ui/icons";
import { describeDeadline, formatDate, formatNumber } from "@/lib/format";
import type { RfqDetail } from "@/types";

function Fact({
  icon,
  label,
  children,
}: {
  icon: ReactNode;
  label: string;
  children: ReactNode;
}) {
  return (
    <div className="min-w-0">
      <dt className="flex items-center gap-1.5 text-xs text-ink-subtle">
        {icon}
        {label}
      </dt>
      <dd className="mt-1.5 text-base font-medium text-ink">{children}</dd>
    </div>
  );
}

/** The full requirement, laid out the same way for both buyers and suppliers. */
export function RfqSummary({ rfq, showBuyer }: { rfq: RfqDetail; showBuyer: boolean }) {
  const deadline = describeDeadline(rfq.deadline);

  return (
    <Card className="animate-rise-in overflow-hidden">
      <div className="border-b border-line bg-gradient-to-b from-canvas-sunken/60 to-transparent p-5 sm:p-6">
        <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
          <div className="min-w-0">
            <h2 className="text-xl font-semibold text-ink">{rfq.productName}</h2>
            {showBuyer && (
              <p className="mt-1.5 flex items-center gap-1.5 text-sm text-ink-muted">
                <BuildingIcon className="size-4 shrink-0" />
                {rfq.buyerCompanyName}
              </p>
            )}
          </div>
          <StatusBadge status={rfq.status} isExpired={rfq.isExpired} />
        </div>
      </div>

      <div className="p-5 sm:p-6">
        <dl className="grid gap-5 sm:grid-cols-3">
          <Fact icon={<PackageIcon className="size-3.5" />} label="Quantity">
            <span className="tabular-nums">{formatNumber(rfq.quantity)}</span>
          </Fact>

          <Fact icon={<MapPinIcon className="size-3.5" />} label="Delivery location">
            {rfq.deliveryLocation}
          </Fact>

          <Fact icon={<CalendarIcon className="size-3.5" />} label="Deadline">
            {formatDate(rfq.deadline)}
            <span
              className={clsx(
                "ml-2 text-sm font-normal",
                deadline.past ? "text-ink-subtle" : deadline.urgent ? "text-warning" : "text-ink-muted",
              )}
            >
              {deadline.label}
            </span>
          </Fact>
        </dl>

        <div className="mt-6 border-t border-line pt-5">
          <h3 className="text-xs text-ink-subtle">Description</h3>
          <p className="mt-2 text-[0.9375rem] leading-relaxed whitespace-pre-wrap text-ink">
            {rfq.description}
          </p>
        </div>
      </div>
    </Card>
  );
}
