import CheckIcon from '@mui/icons-material/Check'
import CloseIcon from '@mui/icons-material/Close'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import { IconButton, TableCell, TableRow, TextField } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { StockTransferDto, StockTransferLineDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface Props {
  stockTransferId: string
  line: StockTransferLineDto
  // Draft only — edit/remove controls are hidden once Dispatched/Received/Cancelled.
  canEdit: boolean
  // Received or later — shows the Received Qty / Variance columns instead of Actions.
  showReceivedColumns: boolean
}

export function StockTransferLineRow({ stockTransferId, line, canEdit, showReceivedColumns }: Props) {
  const { showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [editing, setEditing] = useState(false)
  const [quantity, setQuantity] = useState(String(line.dispatchedQuantity))

  function startEdit() {
    setQuantity(String(line.dispatchedQuantity))
    setEditing(true)
  }

  const updateMutation = useMutation({
    mutationFn: () =>
      api.put<StockTransferDto>(`/api/stock-transfers/${stockTransferId}/lines/${line.id}`, {
        quantity: Number(quantity),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stock-transfers', stockTransferId] })
      setEditing(false)
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to update line.'),
  })

  const removeMutation = useMutation({
    mutationFn: () => api.delete<StockTransferDto>(`/api/stock-transfers/${stockTransferId}/lines/${line.id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['stock-transfers', stockTransferId] }),
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to remove line.'),
  })

  const canSave = Number(quantity) > 0

  return (
    <TableRow>
      <TableCell>{line.sku}</TableCell>
      <TableCell>{line.barcode ?? '—'}</TableCell>
      <TableCell>{line.productName}</TableCell>
      <TableCell align="right">
        {editing ? (
          <TextField
            size="small"
            type="number"
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            slotProps={{ input: { inputProps: { min: 1, step: '1' } } }}
            sx={{ width: 100 }}
          />
        ) : (
          line.dispatchedQuantity
        )}
      </TableCell>
      {showReceivedColumns && (
        <>
          <TableCell align="right">{line.receivedQuantity ?? '—'}</TableCell>
          <TableCell
            align="right"
            sx={{
              color:
                line.variance && line.variance !== 0 ? 'error.main' : undefined,
            }}
          >
            {line.variance ?? '—'}
          </TableCell>
        </>
      )}
      {canEdit && (
        <TableCell align="right">
          {editing ? (
            <>
              <IconButton
                size="small"
                color="primary"
                disabled={!canSave || updateMutation.isPending}
                onClick={() => updateMutation.mutate()}
                aria-label="Save line"
              >
                <CheckIcon fontSize="small" />
              </IconButton>
              <IconButton size="small" onClick={() => setEditing(false)} aria-label="Cancel edit">
                <CloseIcon fontSize="small" />
              </IconButton>
            </>
          ) : (
            <>
              <IconButton size="small" onClick={startEdit} aria-label="Edit line">
                <EditIcon fontSize="small" />
              </IconButton>
              <IconButton
                size="small"
                color="error"
                disabled={removeMutation.isPending}
                onClick={() => removeMutation.mutate()}
                aria-label="Remove line"
              >
                <DeleteIcon fontSize="small" />
              </IconButton>
            </>
          )}
        </TableCell>
      )}
    </TableRow>
  )
}
