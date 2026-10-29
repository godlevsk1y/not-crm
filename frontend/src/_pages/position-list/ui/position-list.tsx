import { RestorePositionButton } from "@/features/position-actions";
import {
  PositionListItemCard,
  type PositionListItemDto,
} from "@/entities/position";

type PositionListProps = {
  positions: PositionListItemDto[];
  isActive: boolean;
  onCardUpdate: (position: PositionListItemDto) => void;
  onCardDelete: (position: PositionListItemDto) => void;
};

export function PositionList({
  positions,
  isActive,
  onCardUpdate,
  onCardDelete,
}: PositionListProps) {
  return (
    <ul className="grid gap-5 grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
      {positions.map((position) => (
        <li key={position.id} className="min-w-0">
          <PositionListItemCard
            position={position}
            restoreAction={
              !isActive ? <RestorePositionButton id={position.id} /> : undefined
            }
            onUpdate={isActive ? () => onCardUpdate(position) : undefined}
            onDelete={isActive ? () => onCardDelete(position) : undefined}
          />
        </li>
      ))}
    </ul>
  );
}
