import clsx from "clsx";
import { Badge, Card } from "@/components/ui";
import { ClockIcon, TagIcon, TrendDownIcon, TruckIcon } from "@/components/ui/icons";
import { formatCurrency, formatDateTime } from "@/lib/format";
import type { Quotation } from "@/types";

/**
 * Quotations for one RFQ, cheapest first (the API orders them).
 *
 * The lowest price and the fastest delivery are marked, because those are the two axes a
 * buyer compares on and they are rarely the same supplier. Each row also carries a bar
 * showing its price relative to the range, so the spread is visible without arithmetic.
 */
export function QuotationList({ quotations }: { quotations: Quotation[] }) {
  const prices = quotations.map((q) => q.quotedPrice);
  const lowestPrice = Math.min(...prices);
  const highestPrice = Math.max(...prices);
  const fastest = Math.min(...quotations.map((q) => q.estimatedDeliveryDays));

  const comparable = quotations.length > 1;
  const spread = highestPrice - lowestPrice;

  return (
    <ul className="stagger space-y-3">
      {quotations.map((quotation) => {
        const isCheapest = quotation.quotedPrice === lowestPrice;
        const isFastest = quotation.estimatedDeliveryDays === fastest;

        // Relative position in the price range, floored so the cheapest bar is still visible.
        const fill =
          spread > 0 ? 12 + ((quotation.quotedPrice - lowestPrice) / spread) * 88 : 100;

        return (
          <li key={quotation.id}>
            <Card
              className={clsx(
                "overflow-hidden transition-[border-color] duration-[180ms]",
                isCheapest && comparable && "border-positive-line",
              )}
            >
              <div className="p-5">
                <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
                  <div className="min-w-0">
                    <h3 className="truncate text-base font-semibold text-ink">
                      {quotation.supplierCompanyName}
                    </h3>
                    <p className="mt-0.5 truncate text-sm text-ink-muted">
                      {quotation.supplierName} · Submitted {formatDateTime(quotation.createdAt)}
                    </p>
                  </div>

                  {comparable && (
                    <div className="flex flex-wrap gap-1.5">
                      {isCheapest && (
                        <Badge tone="positive" icon={<TrendDownIcon className="size-3" />}>
                          Lowest price
                        </Badge>
                      )}
                      {isFastest && (
                        <Badge tone="brand" icon={<ClockIcon className="size-3" />}>
                          Fastest delivery
                        </Badge>
                      )}
                    </div>
                  )}
                </div>

                <dl className="mt-5 flex flex-wrap gap-x-10 gap-y-4">
                  <div>
                    <dt className="flex items-center gap-1.5 text-xs text-ink-subtle">
                      <TagIcon className="size-3.5" />
                      Quoted price
                    </dt>
                    <dd className="mt-1 text-xl font-semibold tabular-nums text-ink">
                      {formatCurrency(quotation.quotedPrice)}
                    </dd>
                  </div>

                  <div>
                    <dt className="flex items-center gap-1.5 text-xs text-ink-subtle">
                      <TruckIcon className="size-3.5" />
                      Estimated delivery
                    </dt>
                    <dd className="mt-1 text-xl font-semibold tabular-nums text-ink">
                      {quotation.estimatedDeliveryDays}
                      <span className="ml-1 text-sm font-normal text-ink-muted">
                        {quotation.estimatedDeliveryDays === 1 ? "day" : "days"}
                      </span>
                    </dd>
                  </div>
                </dl>

                {quotation.message && (
                  <p className="mt-5 border-t border-line pt-4 text-sm leading-relaxed whitespace-pre-wrap text-ink-muted">
                    {quotation.message}
                  </p>
                )}
              </div>

              {/* Price position across the range. Decorative: the figure above is the real content. */}
              {comparable && spread > 0 && (
                <div className="h-1 w-full bg-canvas-sunken" aria-hidden="true">
                  <div
                    className={clsx(
                      "h-full rounded-r-full transition-[width] duration-500",
                      isCheapest ? "bg-positive" : "bg-brand/45",
                    )}
                    style={{ width: `${fill}%` }}
                  />
                </div>
              )}
            </Card>
          </li>
        );
      })}
    </ul>
  );
}
