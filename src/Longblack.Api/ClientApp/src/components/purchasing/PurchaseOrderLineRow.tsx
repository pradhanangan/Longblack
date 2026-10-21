import CheckIcon from '@mui/icons-material/Check'
import CloseIcon from '@mui/icons-material/Close'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import { IconButton, TableCell, TableRow, TextField } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { PurchaseOrderDto, PurchaseOrderLineDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface Props {
  purchaseOrderId: string
  line: PurchaseOrderLineDto
  canEdit: boolean
}

export function PurchaseOrderLineRow({ purchaseOrderId, line, canEdit }: Props) {
  const { showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [editing, setEditing] = useState(false)
  const [quantity, setQuantity] = useState(String(line.quantity))
  const [unitCost, setUnitCost] = useState(String(line.unitCost))

  function startEdit() {
    setQuantity(String(line.quantity))
    setUnitCost(String(line.unitCost))
    setEditing(true)
  }

  const updateMutation = useMutation({
    mutationFn: () =>
      api.put<PurchaseOrderDto>(`/api/purchase-orders/${purchaseOrderId}/lines/${line.id}`, {
        quantity: Number(quantity),
        unitCost: Number(unitCost),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchase-orders', purchaseOrderId] })
      setEditing(false)
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to update line.'),
  })

  const removeMutation = useMutation({
    mutationFn: () => api.delete<PurchaseOrderDto>(`/api/purchase-orders/${purchaseOrderId}/lines/${line.id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['purchase-orders', purchaseOrderId] }),
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to remove line.'),
  })

  const canSave = Number(quantity) > 0 && unitCost.trim() !== '' && Number(unitCost) >= 0

  return (
    <TableRow>
      <TableCell>{line.sku}</TableCell>
      <TableCell>{line.barcode ?? '—'}</TableCell>
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
          line.quantity
        )}
      </TableCell>
      <TableCell align="right">
        {editing ? (
          <TextField
            size="small"
            type="number"
            value={unitCost}
            onChange={(e) => setUnitCost(e.target.value)}
            slotProps={{ input: { inputProps: { min: 0, step: '0.01' } } }}
            sx={{ width: 130 }}
          />
        ) : (
          `$${line.unitCost.toFixed(2)}`
        )}
      </TableCell>
      <TableCell align="right">${(line.quantity * line.unitCost).toFixed(2)}</TableCell>
      <TableCell align="right">
        {line.receivedQuantity} / {line.outstandingQuantity} left
      </TableCell>
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
