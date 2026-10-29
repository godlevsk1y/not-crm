import type { LocationListItemDto } from "@/entities/location";

export type LocationSelectOption = Pick<LocationListItemDto, "id" | "name">;

export type LocationSelectProps = {
  id?: string;
  label?: string;
  placeholder?: string;
  disabled?: boolean;
  excludeIds?: string[];
} & (
  | {
      mode: "single";
      value: LocationSelectOption | null;
      onChange: (value: LocationSelectOption | null) => void;
    }
  | {
      mode: "multiple";
      value: LocationSelectOption[];
      onChange: (value: LocationSelectOption[]) => void;
    }
);
