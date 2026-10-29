import { RestoreLocationButton } from "@/features/location-actions";
import { LocationListItemCard, LocationListItemDto } from "@/entities/location";

type LocationListProps = {
  locations: LocationListItemDto[];
  isActive: boolean;
  onCardUpdate: (location: LocationListItemDto) => void;
  onCardDelete: (location: LocationListItemDto) => void;
};

export function LocationList({
  locations,
  isActive,
  onCardUpdate,
  onCardDelete,
}: LocationListProps) {
  return (
    <ul className="grid gap-5 grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
      {locations.map((location) => (
        <li key={location.id} className="min-w-0">
          <LocationListItemCard
            location={location}
            restoreAction={
              !isActive ? <RestoreLocationButton id={location.id} /> : undefined
            }
            onUpdate={isActive ? () => onCardUpdate(location) : undefined}
            onDelete={isActive ? () => onCardDelete(location) : undefined}
          />
        </li>
      ))}
    </ul>
  );
}
