import {
  Box,
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
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useDeferredValue, useEffect, useState } from "react";
import { api } from "../api/client";
import type { BrandDto, CategoryDto, StoreDto } from "../api/types";
import { renderCategoryMenuItems } from "../components/categories/CategorySelectItems";
import type { InventoryDto } from "../api/types";
import { GridPagination } from "../components/common/GridPagination";
import { usePagedList } from "../components/common/usePagedList";

function useInventory(
  q: string,
  brandId: string,
  categoryId: string,
  status: string,
  storeId: string,
) {
  const params = new URLSearchParams();
  if (q) params.set("q", q);
  if (brandId) params.set("brandId", brandId);
  if (categoryId) params.set("categoryId", categoryId);
  if (storeId) params.set("storeId", storeId);
  // Always sent (Active/Inactive/All) — unlike Products, "All" must be an explicit value the
  // backend can distinguish from "not provided" (which defaults to Active).
  params.set("status", status);
  return usePagedList<InventoryDto>("inventory", params);
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

function useStores() {
  return useQuery<StoreDto[]>({
    queryKey: ["stores", "Active"],
    queryFn: () => api.get("/api/stores?status=Active"),
    staleTime: 5 * 60_000,
  });
}

export function InventoryListPage() {
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState("");
  const [brandId, setBrandId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [status, setStatus] = useState("Active");
  // Defaults to the Warehouse Store's id once stores load (see effect below) — no separate
  // "default" placeholder option, so the dropdown shows Warehouse genuinely selected.
  const [storeId, setStoreId] = useState("");

  const deferredSearch = useDeferredValue(searchInput);

  const {
    items: inventory,
    pagination,
    isLoading,
    isError,
  } = useInventory(deferredSearch, brandId, categoryId, status, storeId);
  const { data: brands } = useBrands();
  const { data: categories } = useCategories();
  const { data: stores } = useStores();

  useEffect(() => {
    if (!storeId && stores) {
      const warehouse = stores.find((s) => s.type === "Warehouse");
      if (warehouse) setStoreId(warehouse.id);
    }
  }, [stores, storeId]);

  return (
    <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1 }}>
      <Toolbar disableGutters sx={{ mb: 3 }}>
        <Typography variant="h5">Inventory</Typography>
      </Toolbar>

      <Box sx={{ display: "flex", gap: 2, mb: 3, flexWrap: "wrap" }}>
        <TextField
          label="Search"
          size="small"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          placeholder="Name, code, SKU, barcode…"
          sx={{ minWidth: 220 }}
        />
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Store</InputLabel>
          <Select
            value={storeId}
            label="Store"
            onChange={(e) => setStoreId(e.target.value)}
          >
            {stores?.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Brand</InputLabel>
          <Select
            value={brandId}
            label="Brand"
            onChange={(e) => setBrandId(e.target.value)}
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
            onChange={(e) => setCategoryId(e.target.value)}
          >
            <MenuItem value="">All categories</MenuItem>
            {renderCategoryMenuItems(categories)}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 120 }}>
          <InputLabel>Status</InputLabel>
          <Select
            value={status}
            label="Status"
            onChange={(e) => setStatus(e.target.value)}
          >
            <MenuItem value="Active">Active</MenuItem>
            <MenuItem value="Inactive">Inactive</MenuItem>
            <MenuItem value="All">All</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {(isLoading || isError || inventory?.length === 0) && (
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
            <Typography color="error">Failed to load inventory.</Typography>
          ) : (
            <Typography color="text.secondary">
              No inventory items match the current filters.
            </Typography>
          )}
        </Box>
      )}

      {!isLoading && !isError && inventory && inventory.length > 0 && (
        <>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>SKU</TableCell>
                <TableCell>Barcode</TableCell>
                <TableCell>Product</TableCell>
                <TableCell>Colour</TableCell>
                <TableCell>Size</TableCell>
                <TableCell>Store</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Quantity</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {inventory.map((i) => (
                <TableRow
                  key={i.productVariantId}
                  hover
                  onClick={() =>
                    navigate(
                      `/inventory/${i.productVariantId}?storeId=${i.storeId}`,
                    )
                  }
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell>{i.sku}</TableCell>
                  <TableCell>{i.barcode ?? "—"}</TableCell>
                  <TableCell>{i.productName}</TableCell>
                  <TableCell>{i.colourName ?? "—"}</TableCell>
                  <TableCell>{i.sizeName ?? "—"}</TableCell>
                  <TableCell>{i.storeName}</TableCell>
                  <TableCell>
                    <Chip
                      label={i.status}
                      size="small"
                      color={i.status === "Active" ? "success" : "default"}
                    />
                  </TableCell>
                  <TableCell align="right">{i.quantity}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <GridPagination {...pagination} />
        </>
      )}
    </Box>
  );
}
