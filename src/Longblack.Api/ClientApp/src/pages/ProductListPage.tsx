import AddIcon from "@mui/icons-material/Add";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Toolbar,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useDeferredValue, useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { api } from "../api/client";
import type {
  BrandDto,
  CategoryDto,
  PagedResult,
  ProductDto,
} from "../api/types";
import { renderCategoryMenuItems } from "../components/categories/CategorySelectItems";
import { FilterPanel } from "../components/common/FilterPanel";
import { GridPagination } from "../components/common/GridPagination";
import {
  DEFAULT_PAGE_SIZE,
  PAGE_SIZE_OPTIONS,
} from "../components/common/pagination";
import { ProductFormDialog } from "../components/products/ProductFormDialog";
import { ProductThumbnail } from "../components/products/ProductThumbnail";
import { useAuth } from "../contexts/AuthContext";

function useProducts(
  q: string,
  brandId: string,
  categoryId: string,
  status: string,
  page: number,
  pageSize: number,
) {
  const params = new URLSearchParams();
  if (q) params.set("q", q);
  if (brandId) params.set("brandId", brandId);
  if (categoryId) params.set("categoryId", categoryId);
  if (status) params.set("status", status);
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));
  return useQuery<PagedResult<ProductDto>>({
    queryKey: ["products", q, brandId, categoryId, status, page, pageSize],
    queryFn: () => api.get(`/api/products?${params.toString()}`),
    placeholderData: (previous) => previous,
  });
}

function useBrands() {
  return useQuery<BrandDto[]>({
    queryKey: ["brands"],
    queryFn: () => api.get("/api/brands"),
    staleTime: 5 * 60_000,
  });
}

function useCategories() {
  return useQuery<CategoryDto[]>({
    queryKey: ["categories"],
    queryFn: () => api.get("/api/categories"),
    staleTime: 5 * 60_000,
  });
}

export function ProductListPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const canWrite =
    user?.roles.some((r) => r === "Manager" || r === "Admin") ?? false;

  const [searchParams, setSearchParams] = useSearchParams();
  // Status defaults to Active only when the parameter is absent; an explicit empty value means "All".
  const searchInput = searchParams.get("q") ?? "";
  const brandId = searchParams.get("brandId") ?? "";
  const categoryId = searchParams.get("categoryId") ?? "";
  const status = searchParams.get("status") ?? "Active";
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const requestedSize = Number(searchParams.get("pageSize"));
  const pageSize = PAGE_SIZE_OPTIONS.includes(requestedSize)
    ? requestedSize
    : DEFAULT_PAGE_SIZE;

  const [addOpen, setAddOpen] = useState(false);

  // Any change other than paging itself returns to page 1.
  function updateParams(patch: Record<string, string>, keepPage = false) {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        for (const [key, value] of Object.entries(patch)) {
          if (value === "" && key !== "status") next.delete(key);
          else next.set(key, value);
        }
        if (!keepPage) next.delete("page");
        return next;
      },
      { replace: true },
    );
  }

  const deferredSearch = useDeferredValue(searchInput);

  const {
    data,
    isLoading,
    isError,
  } = useProducts(deferredSearch, brandId, categoryId, status, page, pageSize);
  const products = data?.items;
  const total = data?.total ?? 0;
  const { data: brands } = useBrands();
  const { data: categories } = useCategories();

  // A stale URL or removed rows can leave the page past the end; step back to the last page.
  const lastPage = Math.max(1, Math.ceil(total / pageSize));
  useEffect(() => {
    if (data && page > lastPage) {
      updateParams({ page: String(lastPage) }, true);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data, page, lastPage]);

  return (
    <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1 }}>
      <Toolbar
        disableGutters
        sx={{ justifyContent: "space-between", gap: 1, mb: 3 }}
      >
        <Typography variant="h5">Products</Typography>
        {canWrite && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setAddOpen(true)}
          >
            Add Product
          </Button>
        )}
      </Toolbar>

      <ProductFormDialog open={addOpen} onClose={() => setAddOpen(false)} />

      <FilterPanel
        fields={[
          {
            key: "q",
            label: "Search",
            kind: "text",
            placeholder: "Name, code, SKU, barcode…",
          },
          {
            key: "brandId",
            label: "Brand",
            kind: "select",
            allLabel: "All brands",
            options: brands?.map((b) => ({ value: b.id, label: b.name })),
          },
          {
            key: "categoryId",
            label: "Category",
            kind: "select",
            allLabel: "All categories",
            renderOptions: () => renderCategoryMenuItems(categories),
            formatValue: (v) =>
              categories?.find((c) => c.id === v)?.name ?? v,
          },
          {
            key: "status",
            label: "Status",
            kind: "select",
            allLabel: "All",
            options: [
              { value: "Active", label: "Active" },
              { value: "Inactive", label: "Inactive" },
            ],
          },
        ]}
        values={{ q: searchInput, brandId, categoryId, status }}
        onApply={(v) => updateParams(v)}
      />

      {(isLoading || isError || products?.length === 0) && (
        <Box
          sx={{
            flexGrow: 1,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            textAlign: "center",
          }}
        >
          {isLoading ? (
            <CircularProgress />
          ) : isError ? (
            <Typography color="error">Failed to load products.</Typography>
          ) : (
            <Typography color="text.secondary">
              No products match the current filters.
            </Typography>
          )}
        </Box>
      )}

      {!isLoading &&
        !isError &&
        products &&
        products.length > 0 && (
          <>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ width: 56 }} />
                  <TableCell>Code</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell>Brand</TableCell>
                  <TableCell>Category</TableCell>
                  <TableCell>Status</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {products.map((p) => (
                  <TableRow
                    key={p.id}
                    hover
                    onClick={() => navigate(`/products/${p.id}`)}
                    sx={{ cursor: "pointer" }}
                  >
                    <TableCell>
                      <ProductThumbnail url={p.thumbnailUrl} alt={p.name} />
                    </TableCell>
                    <TableCell>{p.productCode}</TableCell>
                    <TableCell>{p.name}</TableCell>
                    <TableCell>{p.brandName ?? "—"}</TableCell>
                    <TableCell>{p.categoryName ?? "—"}</TableCell>
                    <TableCell>
                      <Chip
                        label={p.status}
                        size="small"
                        color={p.status === "Active" ? "success" : "default"}
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <GridPagination
              total={total}
              page={page}
              pageSize={pageSize}
              onPageChange={(p) => updateParams({ page: String(p) }, true)}
              onPageSizeChange={(s) => updateParams({ pageSize: String(s) })}
            />
          </>
        )}
    </Box>
  );
}
