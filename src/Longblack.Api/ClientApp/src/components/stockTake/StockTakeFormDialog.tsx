import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../../api/client'
import type { BrandDto, CategoryDto, StockTakeDto, StoreDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'
import { renderCategoryMenuItems } from '../categories/CategorySelectItems'

interface Props {
  open: boolean
  onClose: () => void
}

// Scope (Brand/Category) is fixed at creation — no edit dialog exists; changing your mind
// means Cancel + create a new Stock Take (see grill session for Stock Take frontend).
export function StockTakeFormDialog({ open, onClose }: Props) {
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const [storeId, setStoreId] = useState('')
  const [brandId, setBrandId] = useState('')
  const [categoryId, setCategoryId] = useState('')

  const { data: stores } = useQuery<StoreDto[]>({
    queryKey: ['stores', 'Active'],
    queryFn: () => api.get('/api/stores?status=Active'),
    staleTime: 5 * 60_000,
  })

  const { data: brands } = useQuery<BrandDto[]>({
    queryKey: ['brands'],
    queryFn: () => api.get('/api/brands'),
    staleTime: 5 * 60_000,
  })

  const { data: categories } = useQuery<CategoryDto[]>({
    queryKey: ['categories'],
    queryFn: () => api.get('/api/categories'),
    staleTime: 5 * 60_000,
  })

  const mutation = useMutation({
    mutationFn: () =>
      api.post<StockTakeDto>('/api/stock-takes', {
        storeId,
        brandId: brandId || null,
        categoryId: categoryId || null,
      }),
    onSuccess: (data: StockTakeDto) => {
      queryClient.invalidateQueries({ queryKey: ['stock-takes'] })
      showSuccess('Stock Take created.')
      setStoreId('')
      setBrandId('')
      setCategoryId('')
      onClose()
      navigate(`/stock-takes/${data.id}`)
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to create Stock Take.'),
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>New Stock Take</DialogTitle>
      <DialogContent>
        <FormControl fullWidth margin="normal" required>
          <InputLabel>Store</InputLabel>
          <Select value={storeId} label="Store" onChange={(e) => setStoreId(e.target.value)}>
            {stores?.map((s) => <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>)}
          </Select>
        </FormControl>
        <FormControl fullWidth margin="normal">
          <InputLabel>Brand</InputLabel>
          <Select value={brandId} label="Brand" onChange={(e) => setBrandId(e.target.value)}>
            <MenuItem value="">Any brand</MenuItem>
            {brands?.map((b) => <MenuItem key={b.id} value={b.id}>{b.name}</MenuItem>)}
          </Select>
        </FormControl>
        <FormControl fullWidth margin="normal">
          <InputLabel>Category</InputLabel>
          <Select value={categoryId} label="Category" onChange={(e) => setCategoryId(e.target.value)}>
            <MenuItem value="">Any category</MenuItem>
            {renderCategoryMenuItems(categories)}
          </Select>
        </FormControl>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending || !storeId}
          startIcon={mutation.isPending ? <CircularProgress size={16} /> : undefined}
        >
          Create
        </Button>
      </DialogActions>
    </Dialog>
  )
}
