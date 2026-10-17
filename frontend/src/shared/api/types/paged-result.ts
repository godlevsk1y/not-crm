export type PagedResult<T> = {
  results: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};
