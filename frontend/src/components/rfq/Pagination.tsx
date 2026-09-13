"use client";

import { Button } from "@/components/ui";
import { ChevronLeftIcon, ChevronRightIcon } from "@/components/ui/icons";
import { formatNumber } from "@/lib/format";
import type { PagedResult } from "@/types";

export function Pagination<T>({
  page: result,
  onPageChange,
}: {
  page: PagedResult<T>;
  onPageChange: (page: number) => void;
}) {
  if (result.totalPages <= 1) return null;

  const first = (result.page - 1) * result.pageSize + 1;
  const last = Math.min(result.page * result.pageSize, result.totalCount);

  return (
    <nav
      className="flex flex-col items-center justify-between gap-3 border-t border-line pt-4 sm:flex-row"
      aria-label="Pagination"
    >
      <p className="text-sm text-ink-muted">
        Showing <span className="font-medium tabular-nums text-ink">{formatNumber(first)}</span>–
        <span className="font-medium tabular-nums text-ink">{formatNumber(last)}</span> of{" "}
        <span className="font-medium tabular-nums text-ink">{formatNumber(result.totalCount)}</span>
      </p>

      <div className="flex items-center gap-2">
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onPageChange(result.page - 1)}
          disabled={!result.hasPreviousPage}
          icon={<ChevronLeftIcon className="size-4" />}
        >
          Previous
        </Button>

        <span className="px-2 text-sm tabular-nums text-ink-muted">
          {result.page} / {result.totalPages}
        </span>

        <Button
          variant="secondary"
          size="sm"
          onClick={() => onPageChange(result.page + 1)}
          disabled={!result.hasNextPage}
        >
          Next
          <ChevronRightIcon className="size-4" />
        </Button>
      </div>
    </nav>
  );
}
