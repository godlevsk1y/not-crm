import {
  PositionListItemCard,
  type PositionListItemDto,
} from "@/entities/position";

type PositionListProps = {
  positions: PositionListItemDto[];
  onCardUpdate: (position: PositionListItemDto) => void;
  onCardDelete: (position: PositionListItemDto) => void;
};

export function PositionList({
  positions,
  onCardUpdate,
  onCardDelete,
}: PositionListProps) {
  return (
    <ul className="grid gap-5 grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
      {positions.map((position) => (
        <li key={position.id} className="min-w-0">
          <PositionListItemCard
            position={position}
            onUpdate={() => onCardUpdate(position)}
            onDelete={() => onCardDelete(position)}
          />
        </li>
      ))}
    </ul>
  );
}
