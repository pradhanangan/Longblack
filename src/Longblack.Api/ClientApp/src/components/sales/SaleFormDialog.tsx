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
import type { SaleDto, StoreDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface Props {
  open: boolean
  onClose: () => void
}

export function SaleFormDialog({ open, onClose }: Props) {
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const [storeId, setStoreId] = useState('')

  const { data: stores } = useQuery<StoreDto[]>({
    queryKey: ['stores', 'Active'],
    queryFn: () => api.get('/api/stores?status=Active'),
    staleTime: 5 * 60_000,
  })

  const mutation = useMutation({
    mutationFn: () => api.post<SaleDto>('/api/sales', { storeId }),
    onSuccess: (data: SaleDto) => {
      queryClient.invalidateQueries({ queryKey: ['sales'] })
      showSuccess('Sale created.')
      setStoreId('')
      onClose()
      navigate(`/sales/${data.id}`)
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to create Sale.'),
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>New Sale</DialogTitle>
      <DialogContent>
        <FormControl fullWidth margin="normal" required>
          <InputLabel>Store</InputLabel>
          <Select value={storeId} label="Store" onChange={(e) => setStoreId(e.target.value)}>
            {stores?.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={() => mutation.mutate()}
          disabled={!storeId || mutation.isPending}
          startIcon={mutation.isPending ? <CircularProgress size={16} /> : undefined}
        >
          Create
        </Button>
      </DialogActions>
    </Dialog>
  )
}
