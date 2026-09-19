import HistoryIcon from '@mui/icons-material/History'
import DeleteIcon from '@mui/icons-material/Delete'
import { Button, Chip, IconButton, TableCell, TableRow, TextField } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { StockTakeDto, StockTakeItemDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'
import { ConfirmDialog } from '../products/ConfirmDialog'
import { CountHistoryDialog } from './CountHistoryDialog'

interface Props {
  stockTakeId: string
  item: StockTakeItemDto
  // Recount/Remove only make sense while the Stock Take is InProgress; read-only otherwise.
  canEdit: boolean
}

export function StockTakeItemRow({ stockTakeId, item, canEdit }: Props) {
  const { showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [recounting, setRecounting] = useState(false)
  const [recountValue, setRecountValue] = useState('')
  const [removeConfirmOpen, setRemoveConfirmOpen] = useState(false)
  const [historyOpen, setHistoryOpen] = useState(false)

  function startRecount() {
    setRecountValue(String(item.countedQuantity ?? ''))
    setRecounting(true)
  }

  const recountMutation = useMutation({
    mutationFn: () =>
      api.post<StockTakeDto>(`/api/stock-takes/${stockTakeId}/items/${item.id}/counts`, {
        quantity: Number(recountValue),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stock-takes', stockTakeId] })
      setRecounting(false)
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to save recount.'),
  })

  const removeMutation = useMutation({
    mutationFn: () => api.delete<StockTakeDto>(`/api/stock-takes/${stockTakeId}/items/${item.id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['stock-takes', stockTakeId] }),
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to remove item.'),
  })

  const canSaveRecount = recountValue.trim() !== '' && Number(recountValue) >= 0 && !recountMutation.isPending

  return (
    <TableRow sx={{ opacity: item.status === 'Pending' ? 0.7 : 1 }}>
      <TableCell>{item.sku}</TableCell>
      <TableCell>{item.barcode ?? '—'}</TableCell>
      <TableCell>{item.productName}</TableCell>
      <TableCell>{item.colourName ?? '—'}</TableCell>
      <TableCell>{item.sizeName ?? '—'}</TableCell>
      <TableCell align="right">{item.expectedQuantity}</TableCell>
      <TableCell align="right">
        {item.countedQuantity ?? '—'}
        {item.countedQuantity !== null && (
          <IconButton size="small" onClick={() => setHistoryOpen(true)} aria-label="View count history">
            <HistoryIcon fontSize="inherit" />
          </IconButton>
        )}
      </TableCell>
      <TableCell
        align="right"
        sx={{ color: item.variance == null ? undefined : item.variance < 0 ? 'error.main' : item.variance > 0 ? 'success.main' : undefined }}
      >
        {item.variance == null ? '—' : item.variance > 0 ? `+${item.variance}` : item.variance}
      </TableCell>
      <TableCell>
        <Chip label={item.status} size="small" color={item.status === 'Counted' ? 'success' : 'default'} />
      </TableCell>
      {canEdit && (
        <TableCell align="right">
          {recounting ? (
            <>
              <TextField
                size="small"
                type="number"
                value={recountValue}
                onChange={(e) => setRecountValue(e.target.value)}
                slotProps={{ input: { inputProps: { min: 0, step: '1' } } }}
                sx={{ width: 90, mr: 1 }}
              />
              <Button
                size="small"
                disabled={!canSaveRecount}
                onClick={() => recountMutation.mutate()}
                sx={{ mr: 1 }}
              >
                Save
              </Button>
              <Button size="small" onClick={() => setRecounting(false)}>
                Cancel
              </Button>
            </>
          ) : item.status === 'Counted' ? (
            <Button size="small" onClick={startRecount}>
              Recount
            </Button>
          ) : (
            <IconButton
              size="small"
              color="error"
              onClick={() => setRemoveConfirmOpen(true)}
              aria-label="Remove item"
            >
              <DeleteIcon fontSize="small" />
            </IconButton>
          )}
        </TableCell>
      )}

      <CountHistoryDialog
        open={historyOpen}
        onClose={() => setHistoryOpen(false)}
        stockTakeId={stockTakeId}
        itemId={item.id}
        sku={item.sku}
      />
      <ConfirmDialog
        open={removeConfirmOpen}
        onClose={() => setRemoveConfirmOpen(false)}
        title="Remove Item"
        message={`Remove SKU "${item.sku}" from this Stock Take? It cannot be added back — you would need to start a new Stock Take to include it again.`}
        confirmLabel="Remove"
        onConfirm={() => removeMutation.mutate()}
      />
    </TableRow>
  )
}
