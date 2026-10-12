export type LocationListItemDto = {
  id: string;
  name: string;
  address: AddressDto;
  createdAt: string;
  departmentCount: number;
};

export type AddressDto = {
  country: string;
  region?: string;
  city: string;
  district?: string;
  street: string;
  houseNumber: string;
  postalCode?: string;
};
