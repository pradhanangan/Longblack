import CheckIcon from '@mui/icons-material/Check'
import CloseIcon from '@mui/icons-material/Close'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import { IconButton, TableCell, TableRow, TextField } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../../api/client'
import type { SaleDto, SaleLineDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface Props {
  saleId: string
  line: SaleLineDto
  // Draft only — edit/remove controls are hidden once the Sale is no longer Draft.
  canEdit: boolean
}

export function SaleLineRow({ saleId, line, canEdit }: Props) {
  const { showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [editing, setEditing] = useState(false)
  const [quantity, setQuantity] = useState(String(line.quantity))
  const [discount, setDiscount] = useState(String(line.discount))

  function startEdit() {
    setQuantity(String(line.quantity))
    setDiscount(String(line.discount))
    setEditing(true)
  }

  const updateMutation = useMutation({
    mutationFn: () =>
      api.put<SaleDto>(`/api/sales/${saleId}/lines/${line.id}`, {
        quantity: Number(quantity),
        discount: Number(discount) || 0,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sales', saleId] })
      setEditing(false)
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to update line.'),
  })

  const removeMutation = useMutation({
    mutationFn: () => api.delete<SaleDto>(`/api/sales/${saleId}/lines/${line.id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['sales', saleId] }),
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to remove line.'),
  })

  const canSave = Number(quantity) > 0 && Number(discount) >= 0

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
            sx={{ width: 90 }}
          />
        ) : (
          line.quantity
        )}
      </TableCell>
      <TableCell align="right">${line.unitPrice.toFixed(2)}</TableCell>
      <TableCell align="right">
        {editing ? (
          <TextField
            size="small"
            type="number"
            value={discount}
            onChange={(e) => setDiscount(e.target.value)}
            slotProps={{ input: { inputProps: { min: 0, step: '0.01' } } }}
            sx={{ width: 100 }}
          />
        ) : (
          `$${line.discount.toFixed(2)}`
        )}
      </TableCell>
      <TableCell align="right">${line.lineTotal.toFixed(2)}</TableCell>
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
