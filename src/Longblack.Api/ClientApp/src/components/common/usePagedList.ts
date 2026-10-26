import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../../api/client";
import type { PagedResult } from "../../api/types";
import { DEFAULT_PAGE_SIZE } from "./pagination";

export function usePagedList<T>(resource: string, filters: URLSearchParams) {
  const filterKey = filters.toString();
  const [paging, setPaging] = useState({
    filterKey,
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const page = paging.filterKey === filterKey ? paging.page : 1;
  const { pageSize } = paging;

  if (paging.filterKey !== filterKey) {
    setPaging({ filterKey, page: 1, pageSize });
  }

  const params = new URLSearchParams(filters);
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));
  const query = useQuery<PagedResult<T>>({
    queryKey: [resource, "paged", filterKey, page, pageSize],
    queryFn: () => api.get(`/api/${resource}?${params.toString()}`),
  });

  if (query.data) {
    const lastPage = Math.max(1, Math.ceil(query.data.total / pageSize));
    if (page > lastPage) {
      setPaging({ filterKey, page: lastPage, pageSize });
    }
  }

  return {
    ...query,
    items: query.data?.items,
    pagination: {
      total: query.data?.total ?? 0,
      page,
      pageSize,
      onPageChange: (nextPage: number) =>
        setPaging({ filterKey, page: nextPage, pageSize }),
      onPageSizeChange: (nextPageSize: number) =>
        setPaging({ filterKey, page: 1, pageSize: nextPageSize }),
    },
  };
}
