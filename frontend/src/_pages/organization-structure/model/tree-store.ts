import { create } from "zustand";

export type TreeState = {
  expandedIds: Set<string>;
  selectedId?: string;
};

export type TreeActions = {
  resetTree: () => void;

  updateSelectedId: (id?: string) => void;

  expandNode: (id: string) => void;
  collapseNode: (id: string) => void;
};

export const useTreeStore = create<TreeState & TreeActions>()((set) => ({
  expandedIds: new Set<string>(),
  selectedId: undefined,

  resetTree: () =>
    set({ expandedIds: new Set<string>(), selectedId: undefined }),

  updateSelectedId: (id?: string) => set({ selectedId: id }),

  expandNode: (id: string) =>
    set((state) => ({
      expandedIds: new Set<string>(state.expandedIds).add(id),
    })),
  collapseNode: (id: string) =>
    set((state) => {
      const expandedIds = new Set<string>(state.expandedIds);
      expandedIds.delete(id);

      return { expandedIds };
    }),
}));
