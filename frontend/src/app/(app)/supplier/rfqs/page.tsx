"use client";

import { useEffect, useState } from "react";
import {
  Button,
  Card,
  EmptyState,
  ErrorState,
  Input,
  PageHeader,
  Select,
  SkeletonRows,
  Stat,
} from "@/components/ui";
import { CheckCircleIcon, InboxIcon, SearchIcon, SlidersIcon } from "@/components/ui/icons";
import { Pagination } from "@/components/rfq/Pagination";
import { RfqCard } from "@/components/rfq/RfqCard";
import { ApiError } from "@/lib/api";
import { formatNumber } from "@/lib/format";
import { useBrowseRfqs, useDeliveryLocations, useSupplierDashboard } from "@/lib/queries";
import type { RfqQuery } from "@/types";

const PAGE_SIZE = 10;

export default function BrowseRfqsPage() {
  // `searchInput` is what the user is typing; `search` is what the API has been asked for.
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [location, setLocation] = useState("");
  const [sortBy, setSortBy] = useState("createdAt");
  const [includeExpired, setIncludeExpired] = useState(false);
  const [page, setPage] = useState(1);

  // Debounce so every keystroke does not become a request.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput);
      setPage(1);
    }, 350);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const query: RfqQuery = {
    search: search || undefined,
    location: location || undefined,
    includeExpired,
    sortBy,
    page,
    pageSize: PAGE_SIZE,
  };

  const dashboard = useSupplierDashboard();
  const locations = useDeliveryLocations();
  const { data, isPending, isError, error, refetch, isFetching } = useBrowseRfqs(query);

  const hasFilters = !!search || !!location || includeExpired;

  const resetFilters = () => {
    setSearchInput("");
    setSearch("");
    setLocation("");
    setIncludeExpired(false);
    setPage(1);
  };

  return (
    <>
      <PageHeader
        title="Browse RFQs"
        description="Open requirements from buyers. Submit a quotation for anything you can fulfil."
      />

      <div className="mb-6 grid gap-3 sm:grid-cols-3">
        <Stat
          label="Open requirements"
          value={formatNumber(dashboard.data?.availableRfqs ?? 0)}
          loading={dashboard.isPending}
          icon={<SearchIcon className="size-4.5" />}
        />
        <Stat
          label="Awaiting your quote"
          value={formatNumber(dashboard.data?.openRfqsNotQuoted ?? 0)}
          loading={dashboard.isPending}
          tone="warning"
          icon={<InboxIcon className="size-4.5" />}
        />
        <Stat
          label="Quotations submitted"
          value={formatNumber(dashboard.data?.quotationsSubmitted ?? 0)}
          loading={dashboard.isPending}
          tone="positive"
          icon={<CheckCircleIcon className="size-4.5" />}
        />
      </div>

      <Card className="mb-5 p-4">
        <div className="grid gap-3 sm:grid-cols-[1fr_auto_auto]">
          <div>
            <label htmlFor="search" className="sr-only">
              Search requirements
            </label>
            <Input
              id="search"
              type="search"
              placeholder="Search by product or description…"
              value={searchInput}
              leadingIcon={<SearchIcon className="size-4" />}
              onChange={(event) => setSearchInput(event.target.value)}
            />
          </div>

          <div>
            <label htmlFor="location" className="sr-only">
              Delivery location
            </label>
            <Select
              id="location"
              value={location}
              onChange={(event) => {
                setLocation(event.target.value);
                setPage(1);
              }}
              className="sm:w-56"
            >
              <option value="">All locations</option>
              {locations.data?.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </Select>
          </div>

          <div>
            <label htmlFor="sort" className="sr-only">
              Sort by
            </label>
            <Select
              id="sort"
              value={sortBy}
              onChange={(event) => {
                setSortBy(event.target.value);
                setPage(1);
              }}
              className="sm:w-48"
            >
              <option value="createdAt">Newest first</option>
              <option value="deadline_asc">Deadline soonest</option>
              <option value="quantity_desc">Largest quantity</option>
            </Select>
          </div>
        </div>

        <div className="mt-3.5 flex flex-wrap items-center gap-x-4 gap-y-2 border-t border-line pt-3.5">
          <label className="flex cursor-pointer items-center gap-2 text-sm text-ink-muted transition-colors hover:text-ink">
            <input
              type="checkbox"
              checked={includeExpired}
              onChange={(event) => {
                setIncludeExpired(event.target.checked);
                setPage(1);
              }}
              className="size-4 cursor-pointer rounded border-line-strong accent-brand"
            />
            Include RFQs past their deadline
          </label>

          {hasFilters && (
            <Button
              variant="ghost"
              size="sm"
              onClick={resetFilters}
              icon={<SlidersIcon className="size-4" />}
            >
              Clear filters
            </Button>
          )}

          {isFetching && !isPending && (
            <span className="ml-auto flex items-center gap-1.5 text-sm text-ink-subtle">
              <span className="size-1.5 animate-pulse rounded-full bg-brand" />
              Searching
            </span>
          )}
        </div>
      </Card>

      {isPending ? (
        <SkeletonRows rows={4} />
      ) : isError ? (
        <ErrorState
          title="Couldn't load requirements"
          message={error instanceof ApiError ? error.detail : undefined}
          onRetry={() => void refetch()}
        />
      ) : data.items.length === 0 ? (
        <EmptyState
          icon={<SearchIcon className="size-6" />}
          title={hasFilters ? "No RFQs match your filters" : "No open RFQs right now"}
          description={
            hasFilters
              ? "Try a broader search term, a different location, or clear the filters."
              : "Buyers haven't posted any open requirements yet. Check back shortly."
          }
          action={
            hasFilters && (
              <Button variant="secondary" onClick={resetFilters}>
                Clear filters
              </Button>
            )
          }
        />
      ) : (
        <div className="space-y-4">
          <div className="stagger space-y-3">
            {data.items.map((rfq) => (
              <RfqCard key={rfq.id} rfq={rfq} href={`/supplier/rfqs/${rfq.id}`} variant="supplier" />
            ))}
          </div>
          <Pagination page={data} onPageChange={setPage} />
        </div>
      )}
    </>
  );
}
