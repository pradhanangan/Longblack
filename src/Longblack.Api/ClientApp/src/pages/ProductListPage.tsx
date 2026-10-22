import AddIcon from "@mui/icons-material/Add";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
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
import { GridPagination } from "../components/common/GridPagination";
import {
  DEFAULT_PAGE_SIZE,
  PAGE_SIZE_OPTIONS,
} from "../components/common/pagination";
import { ProductFormDialog } from "../components/products/ProductFormDialog";
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
    <Box>
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

      {/* Filter bar */}
      <Box sx={{ display: "flex", gap: 2, mb: 3, flexWrap: "wrap" }}>
        <TextField
          label="Search"
          value={searchInput}
          onChange={(e) => updateParams({ q: e.target.value })}
          placeholder="Name, code, SKU, barcode…"
          sx={{ minWidth: 220 }}
        />
        <FormControl sx={{ minWidth: 160 }}>
          <InputLabel>Brand</InputLabel>
          <Select
            value={brandId}
            label="Brand"
            onChange={(e) => updateParams({ brandId: e.target.value })}
          >
            <MenuItem value="">All brands</MenuItem>
            {brands?.map((b) => (
              <MenuItem key={b.id} value={b.id}>
                {b.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl sx={{ minWidth: 160 }}>
          <InputLabel>Category</InputLabel>
          <Select
            value={categoryId}
            label="Category"
            onChange={(e) => updateParams({ categoryId: e.target.value })}
          >
            <MenuItem value="">All categories</MenuItem>
            {renderCategoryMenuItems(categories)}
          </Select>
        </FormControl>
        <FormControl sx={{ minWidth: 120 }}>
          <InputLabel>Status</InputLabel>
          <Select
            value={status}
            label="Status"
            onChange={(e) => updateParams({ status: e.target.value })}
          >
            <MenuItem value="Active">Active</MenuItem>
            <MenuItem value="Inactive">Inactive</MenuItem>
            <MenuItem value="">All</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {isLoading && <CircularProgress />}
      {isError && (
        <Typography color="error">Failed to load products.</Typography>
      )}

      {!isLoading &&
        !isError &&
        products &&
        (products.length === 0 ? (
          <Typography color="text.secondary">
            No products match the current filters.
          </Typography>
        ) : (
          <>
            <Table size="small">
              <TableHead>
                <TableRow>
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
        ))}
    </Box>
  );
}
