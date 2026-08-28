import AddIcon from '@mui/icons-material/Add'
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
  Typography,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useDeferredValue, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { BrandDto, CategoryDto, ProductDto } from '../api/types'
import { ProductFormDialog } from '../components/products/ProductFormDialog'
import { useAuth } from '../contexts/AuthContext'

function useProducts(q: string, brandId: string, categoryId: string, status: string) {
  const params = new URLSearchParams()
  if (q) params.set('q', q)
  if (brandId) params.set('brandId', brandId)
  if (categoryId) params.set('categoryId', categoryId)
  if (status) params.set('status', status)
  const qs = params.toString()
  return useQuery<ProductDto[]>({
    queryKey: ['products', q, brandId, categoryId, status],
    queryFn: () => api.get(`/api/products${qs ? `?${qs}` : ''}`),
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

export function ProductListPage() {
  const { user } = useAuth()
  const canWrite = user?.roles.some((r) => r === 'Manager' || r === 'Admin') ?? false

  const [searchInput, setSearchInput] = useState('')
  const [brandId, setBrandId] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [status, setStatus] = useState('Active')

  const [addOpen, setAddOpen] = useState(false)

  const deferredSearch = useDeferredValue(searchInput)

  const { data: products, isLoading, isError } = useProducts(deferredSearch, brandId, categoryId, status)
  const { data: brands } = useBrands()
  const { data: categories } = useCategories()

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Products</Typography>
        {canWrite && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setAddOpen(true)}>
            Add Product
          </Button>
        )}
      </Box>

      <ProductFormDialog open={addOpen} onClose={() => setAddOpen(false)} />

      {/* Filter bar */}
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
            <MenuItem value="">All</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {isLoading && <CircularProgress />}
      {isError && <Typography color="error">Failed to load products.</Typography>}

      {!isLoading && !isError && products && (
        products.length === 0 ? (
          <Typography color="text.secondary">No products match the current filters.</Typography>
        ) : (
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
                <TableRow key={p.id} hover>
                  <TableCell>
                    <Link to={`/products/${p.id}`} style={{ textDecoration: 'none', color: 'inherit', fontWeight: 500 }}>
                      {p.productCode}
                    </Link>
                  </TableCell>
                  <TableCell>{p.name}</TableCell>
                  <TableCell>{p.brandName ?? '—'}</TableCell>
                  <TableCell>{p.categoryName ?? '—'}</TableCell>
                  <TableCell>
                    <Chip
                      label={p.status}
                      size="small"
                      color={p.status === 'Active' ? 'success' : 'default'}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )
      )}
    </Box>
  )
}

