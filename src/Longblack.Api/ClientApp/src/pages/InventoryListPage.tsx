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
  Typography,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useDeferredValue, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { BrandDto, CategoryDto } from '../api/types'
import type { InventoryDto } from '../api/types'

function useInventory(q: string, brandId: string, categoryId: string, status: string) {
  const params = new URLSearchParams()
  if (q) params.set('q', q)
  if (brandId) params.set('brandId', brandId)
  if (categoryId) params.set('categoryId', categoryId)
  // Always sent (Active/Inactive/All) — unlike Products, "All" must be an explicit value the
  // backend can distinguish from "not provided" (which defaults to Active).
  params.set('status', status)
  return useQuery<InventoryDto[]>({
    queryKey: ['inventory', q, brandId, categoryId, status],
    queryFn: () => api.get(`/api/inventory?${params.toString()}`),
  })
}

function useBrands() {
  return useQuery<BrandDto[]>({
    queryKey: ['brands'],
    queryFn: () => api.get('/api/brands'),
    staleTime: 5 * 60_000,
  })
}

function useCategories() {
  return useQuery<CategoryDto[]>({
    queryKey: ['categories'],
    queryFn: () => api.get('/api/categories'),
    staleTime: 5 * 60_000,
  })
}

export function InventoryListPage() {
  const [searchInput, setSearchInput] = useState('')
  const [brandId, setBrandId] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [status, setStatus] = useState('Active')

  const deferredSearch = useDeferredValue(searchInput)

  const { data: inventory, isLoading, isError } = useInventory(deferredSearch, brandId, categoryId, status)
  const { data: brands } = useBrands()
  const { data: categories } = useCategories()

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>Inventory</Typography>

      <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
        <TextField
          label="Search"
          size="small"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          placeholder="Name, code, SKU, barcode…"
          sx={{ minWidth: 220 }}
        />
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Brand</InputLabel>
          <Select value={brandId} label="Brand" onChange={(e) => setBrandId(e.target.value)}>
            <MenuItem value="">All brands</MenuItem>
            {brands?.map((b) => <MenuItem key={b.id} value={b.id}>{b.name}</MenuItem>)}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Category</InputLabel>
          <Select value={categoryId} label="Category" onChange={(e) => setCategoryId(e.target.value)}>
            <MenuItem value="">All categories</MenuItem>
            {categories?.map((c) => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 120 }}>
          <InputLabel>Status</InputLabel>
          <Select value={status} label="Status" onChange={(e) => setStatus(e.target.value)}>
            <MenuItem value="Active">Active</MenuItem>
            <MenuItem value="Inactive">Inactive</MenuItem>
            <MenuItem value="All">All</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {isLoading && <CircularProgress />}
      {isError && <Typography color="error">Failed to load inventory.</Typography>}

      {!isLoading && !isError && inventory && (
        inventory.length === 0 ? (
          <Typography color="text.secondary">No variants match the current filters.</Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>SKU</TableCell>
                <TableCell>Barcode</TableCell>
                <TableCell>Product</TableCell>
                <TableCell>Colour</TableCell>
                <TableCell>Size</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Quantity</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {inventory.map((i) => (
                <TableRow key={i.productVariantId} hover>
                  <TableCell>
                    <Link
                      to={`/inventory/${i.productVariantId}`}
                      style={{ textDecoration: 'none', color: 'inherit', fontWeight: 500 }}
                    >
                      {i.sku}
                    </Link>
                  </TableCell>
                  <TableCell>{i.barcode ?? '—'}</TableCell>
                  <TableCell>{i.productName}</TableCell>
                  <TableCell>{i.colourName ?? '—'}</TableCell>
                  <TableCell>{i.sizeName ?? '—'}</TableCell>
                  <TableCell>
                    <Chip label={i.status} size="small" color={i.status === 'Active' ? 'success' : 'default'} />
                  </TableCell>
                  <TableCell align="right">{i.quantity}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )
      )}
    </Box>
  )
}
