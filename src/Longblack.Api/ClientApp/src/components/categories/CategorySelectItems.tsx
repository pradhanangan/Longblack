import { MenuItem } from "@mui/material";
import type { CategoryDto } from "../../api/types";

/**
 * Orders categories as parent → its children (alphabetically within each group).
 * Parents that have at least one child become non-selectable group headers, so
 * only leaf categories can actually be chosen from the dropdown.
 */
export function orderCategoriesForDropdown(
  categories: CategoryDto[] | undefined,
) {
  const list = categories ?? [];
  const childrenByParent = new Map<string, CategoryDto[]>();
  for (const c of list) {
    if (c.parentCategoryId) {
      const siblings = childrenByParent.get(c.parentCategoryId) ?? [];
      siblings.push(c);
      childrenByParent.set(c.parentCategoryId, siblings);
    }
  }
  for (const siblings of childrenByParent.values()) {
    siblings.sort((a, b) => a.name.localeCompare(b.name));
  }

  const parents = list
    .filter((c) => !c.parentCategoryId)
    .sort((a, b) => a.name.localeCompare(b.name));

  return parents.flatMap((parent) => {
    const children = childrenByParent.get(parent.id) ?? [];
    return [
      { category: parent, isGroupHeader: children.length > 0 },
      ...children.map((child) => ({ category: child, isGroupHeader: false })),
    ];
  });
}

/** Renders <MenuItem> entries for a Category <Select>, indenting children under their parent. */
export function renderCategoryMenuItems(
  categories: CategoryDto[] | undefined,
) {
  return orderCategoriesForDropdown(categories).map(
    ({ category, isGroupHeader }) =>
      isGroupHeader ? (
        <MenuItem
          key={category.id}
          value={category.id}
          disabled
          sx={{ fontWeight: 600, opacity: 1, "&.Mui-disabled": { opacity: 1 } }}
        >
          {category.name}
        </MenuItem>
      ) : (
        <MenuItem
          key={category.id}
          value={category.id}
          sx={{ pl: category.parentCategoryId ? 4 : 2 }}
        >
          {category.name}
        </MenuItem>
      ),
  );
}
