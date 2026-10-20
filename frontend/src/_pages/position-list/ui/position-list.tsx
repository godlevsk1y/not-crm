import {
  PositionListItemCard,
  type PositionListItemDto,
} from "@/entities/position";

type PositionListProps = {
  positions: PositionListItemDto[];
};

export function PositionList({ positions }: PositionListProps) {
  return (
    <ul className="grid gap-5 grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
      {positions.map((position) => (
        <li key={position.id} className="min-w-0">
          <PositionListItemCard position={position} />
        </li>
      ))}
    </ul>
  );
}
