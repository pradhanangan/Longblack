import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  Link,
  List,
  ListItem,
  MenuItem,
  Select,
  TextField,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../../api/client'
import type { GoodsReceiptDto, PurchaseOrderDto, SupplierDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface RowError {
  row: number
  message: string
}

interface Props {
  open: boolean
  onClose: () => void
}

const TEMPLATE = 'sku,barcode,quantity,unit_cost\nABC-001-BLK-M,,10,25.50\n'

function downloadTemplate() {
  const url = URL.createObjectURL(new Blob([TEMPLATE], { type: 'text/csv' }))
  const a = document.createElement('a')
  a.href = url
  a.download = 'goods-receipt-template.csv'
  a.click()
  URL.revokeObjectURL(url)
}

export function GoodsReceiptUploadDialog({ open, onClose }: Props) {
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const [supplierId, setSupplierId] = useState('')
  const [purchaseOrderId, setPurchaseOrderId] = useState('')
  const [receivedDate, setReceivedDate] = useState(new Date().toISOString().slice(0, 10))
  const [file, setFile] = useState<File | null>(null)
  const [rowErrors, setRowErrors] = useState<RowError[]>([])

  useEffect(() => {
    if (open) {
      setSupplierId('')
      setPurchaseOrderId('')
      setReceivedDate(new Date().toISOString().slice(0, 10))
      setFile(null)
      setRowErrors([])
    }
  }, [open])

  const { data: suppliers } = useQuery<SupplierDto[]>({
    queryKey: ['suppliers', 'Active'],
    queryFn: () => api.get('/api/suppliers?status=Active'),
    staleTime: 5 * 60_000,
  })

  const { data: purchaseOrders } = useQuery<PurchaseOrderDto[]>({
    queryKey: ['purchase-orders', 'receivable', supplierId],
    queryFn: () => api.get(`/api/purchase-orders?supplierId=${supplierId}`),
    enabled: open && !!supplierId,
  })
  const receivableOrders = (purchaseOrders ?? []).filter(
    (p) => p.status === 'Issued' || p.status === 'PartiallyReceived',
  )

  const mutation = useMutation({
    mutationFn: () => {
      const form = new FormData()
      form.append('file', file!)
      form.append('supplierId', supplierId)
      form.append('receivedDate', new Date(receivedDate).toISOString())
      if (purchaseOrderId) form.append('purchaseOrderId', purchaseOrderId)
      return api.postForm<GoodsReceiptDto>('/api/goods-receipts/upload', form)
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['goods-receipts'] })
      showSuccess('Draft goods receipt created from file.')
      onClose()
      navigate(`/goods-receipts/${data.id}`)
    },
    onError: (err: unknown) => {
      const error = err as Error & { errors?: RowError[] }
      if (Array.isArray(error.errors)) setRowErrors(error.errors)
      else showError(error.message ?? 'Failed to upload goods receipt.')
    },
  })

  const canSubmit = !!supplierId && !!file && !!receivedDate && !mutation.isPending

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Upload Goods Receipt (CSV)</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          Columns: sku, barcode (optional), quantity, unit_cost. Max 1,000 rows.{' '}
          <Link component="button" type="button" onClick={downloadTemplate}>
            Download template
          </Link>
        </Typography>
        <FormControl fullWidth margin="normal">
          <InputLabel>Supplier</InputLabel>
          <Select
            value={supplierId}
            label="Supplier"
            onChange={(e) => {
              setSupplierId(e.target.value)
              setPurchaseOrderId('')
            }}
          >
            {suppliers?.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl fullWidth margin="normal">
          <InputLabel>Purchase Order (optional)</InputLabel>
          <Select
            value={purchaseOrderId}
            label="Purchase Order (optional)"
            disabled={!supplierId}
            onChange={(e) => setPurchaseOrderId(e.target.value)}
          >
            <MenuItem value="">None (direct receiving)</MenuItem>
            {receivableOrders.map((p) => (
              <MenuItem key={p.id} value={p.id}>
                {p.poNumber} — {p.status}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <TextField
          label="Received Date"
          type="date"
          fullWidth
          margin="normal"
          value={receivedDate}
          onChange={(e) => setReceivedDate(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <Button component="label" variant="outlined" sx={{ mt: 1 }}>
          {file ? file.name : 'Choose CSV file'}
          <input
            hidden
            type="file"
            accept=".csv,text/csv"
            onChange={(e) => {
              setFile(e.target.files?.[0] ?? null)
              setRowErrors([])
            }}
          />
        </Button>
        {rowErrors.length > 0 && (
          <Alert severity="error" sx={{ mt: 2 }}>
            The file was rejected; nothing was created.
            <List dense disablePadding sx={{ maxHeight: 200, overflow: 'auto' }}>
              {rowErrors.map((e, i) => (
                <ListItem key={i} disableGutters sx={{ py: 0 }}>
                  {e.row > 0 ? `Row ${e.row}: ` : ''}
                  {e.message}
                </ListItem>
              ))}
            </List>
          </Alert>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={() => mutation.mutate()}
          disabled={!canSubmit}
          startIcon={mutation.isPending ? <CircularProgress size={16} /> : undefined}
        >
          Upload
        </Button>
      </DialogActions>
    </Dialog>
  )
}
