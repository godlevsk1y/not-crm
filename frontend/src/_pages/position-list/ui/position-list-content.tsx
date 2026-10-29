import type { PositionListItemDto } from "@/entities/position";

import { PositionList } from "./position-list";
import {
  PositionListEmpty,
  PositionListError,
  PositionListLoading,
} from "./position-list-states";
import { Spinner } from "@/shared/ui/spinner";
import { type RefCallback, useState } from "react";
import {
  UpdatePositionDialog,
  DeletePositionDialog,
} from "@/features/position-actions";

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
  const [updatingPosition, setUpdatingPosition] =
    useState<PositionListItemDto | null>(null);
  const [deletingPosition, setDeletingPosition] =
    useState<PositionListItemDto | null>(null);

  let content;
  if (isError) {
    content = <PositionListError message={message} onRetry={onRetry} />;
  } else if (isPending) {
    content = <PositionListLoading pageSize={pageSize} />;
  } else if (!positions?.length) {
    content = <PositionListEmpty isActive={isActive} />;
  } else {
    content = (
      <div>
        <PositionList
          positions={positions}
          onCardUpdate={setUpdatingPosition}
          onCardDelete={setDeletingPosition}
        />
        <div ref={cursorRef} className="flex justify-center py-8">
          {isFetchingNextPage && <Spinner />}
        </div>
      </div>
    );
  }

  return (
    <>
      {content}
      {updatingPosition && (
        <UpdatePositionDialog
          position={updatingPosition}
          onClose={() => setUpdatingPosition(null)}
        />
      )}
      {deletingPosition && (
        <DeletePositionDialog
          position={deletingPosition}
          onClose={() => setDeletingPosition(null)}
        />
      )}
    </>
  );
}
