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
import type { StockTransferDto, StoreDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface Props {
  open: boolean
  onClose: () => void
}

// Hub-and-spoke is enforced server-side (one side must be the Warehouse, and they must differ);
// both selects list every Active store so either direction (Warehouse -> store, or store ->
// Warehouse) can be picked from the same two dropdowns.
export function StockTransferFormDialog({ open, onClose }: Props) {
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const [sourceStoreId, setSourceStoreId] = useState('')
  const [destinationStoreId, setDestinationStoreId] = useState('')

  const { data: stores } = useQuery<StoreDto[]>({
    queryKey: ['stores', 'Active'],
    queryFn: () => api.get('/api/stores?status=Active'),
    staleTime: 5 * 60_000,
  })

  const mutation = useMutation({
    mutationFn: () =>
      api.post<StockTransferDto>('/api/stock-transfers', {
        sourceStoreId,
        destinationStoreId,
      }),
    onSuccess: (data: StockTransferDto) => {
      queryClient.invalidateQueries({ queryKey: ['stock-transfers'] })
      showSuccess('Stock Transfer created.')
      setSourceStoreId('')
      setDestinationStoreId('')
      onClose()
      navigate(`/stock-transfers/${data.id}`)
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to create Stock Transfer.'),
  })

  const canCreate = !!sourceStoreId && !!destinationStoreId && sourceStoreId !== destinationStoreId

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>New Stock Transfer</DialogTitle>
      <DialogContent>
        <FormControl fullWidth margin="normal" required>
          <InputLabel>Source Store</InputLabel>
          <Select
            value={sourceStoreId}
            label="Source Store"
            onChange={(e) => setSourceStoreId(e.target.value)}
          >
            {stores?.map((s) => <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>)}
          </Select>
        </FormControl>
        <FormControl fullWidth margin="normal" required>
          <InputLabel>Destination Store</InputLabel>
          <Select
            value={destinationStoreId}
            label="Destination Store"
            onChange={(e) => setDestinationStoreId(e.target.value)}
          >
            {stores?.map((s) => <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>)}
          </Select>
        </FormControl>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={() => mutation.mutate()}
          disabled={!canCreate || mutation.isPending}
          startIcon={mutation.isPending ? <CircularProgress size={16} /> : undefined}
        >
          Create
        </Button>
      </DialogActions>
    </Dialog>
  )
}
