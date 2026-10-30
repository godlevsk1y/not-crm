import { create } from "zustand";

export type TreeState = {
  expandedIds: Set<string>;
};

export type TreeActions = {
  resetTree: () => void;

  expandNodes: (ids: string[]) => void;
  expandNode: (id: string) => void;
  collapseNode: (id: string) => void;
};

export const useTreeStore = create<TreeState & TreeActions>()((set) => ({
  expandedIds: new Set<string>(),

  resetTree: () => set({ expandedIds: new Set<string>() }),

  expandNodes: (ids) =>
    set((state) => ({
      expandedIds: new Set([...state.expandedIds, ...ids]),
    })),

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
