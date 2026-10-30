export type DepartmentDto = {
  id: string;
  name: string;
  slug: string;
  path: string;
  parentId: string | null;
};

export type DepartmentListItemDto = {
  id: string;
  name: string;
  slug: string;
  path: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
};

export type DepartmentDescendantDto = {
  id: string;
  name: string;
  parentId: string | null;
};

export type TransferredDepartmentDto = DepartmentDto & {
  depth: number;
};

export type DepartmentNodeDto = {
  id: string;
  name: string;
  slug: string;
  path: string;
  depth: number;
  createdAt: string;
  hasChildren: boolean;
  childCount: number;
};

export type DepartmentBranchNodeDto = DepartmentNodeDto & {
  parentId: string | null;
};
