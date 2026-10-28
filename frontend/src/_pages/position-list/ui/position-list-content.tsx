import type { PositionListItemDto } from "@/entities/position";

import { PositionList } from "./position-list";
import {
  PositionListEmpty,
  PositionListError,
  PositionListLoading,
} from "./position-list-states";
import { Spinner } from "@/shared/ui/spinner";
import { RefCallback } from "react";

interface PositionListContentProps {
  positions: PositionListItemDto[] | undefined;
  isError: boolean;
  isPending: boolean;
  isActive: boolean;
  pageSize: number;
  message?: string;
  isFetchingNextPage: boolean;
  cursorRef: RefCallback<HTMLDivElement>;
  onRetry: () => void;
}

export function PositionListContent({
  positions,
  isError,
  isPending,
  isActive,
  pageSize,
  isFetchingNextPage,
  cursorRef,
  message = "Не удалось загрузить должности",
  onRetry,
}: PositionListContentProps) {
  if (isError) {
    return <PositionListError message={message} onRetry={onRetry} />;
  }

  if (isPending) {
    return <PositionListLoading pageSize={pageSize} />;
  }

  if (!positions?.length) {
    return <PositionListEmpty isActive={isActive} />;
  }

  return (
    <div>
      <PositionList positions={positions ?? []} />
      <div ref={cursorRef} className="flex justify-center py-8">
        {isFetchingNextPage && <Spinner />}
      </div>
    </div>
  );
}
