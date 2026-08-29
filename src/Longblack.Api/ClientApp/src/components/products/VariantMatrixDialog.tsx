import AddIcon from '@mui/icons-material/Add'
import DeleteIcon from '@mui/icons-material/Delete'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  FormGroup,
  IconButton,
  InputAdornment,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { ColourDto, ProductVariantDto, SizeDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'

interface MatrixRow {
  localId: string
  colourId: string
  colourName: string
  colourCode: string
  sizeId: string
  sizeName: string
  sizeCode: string
  sku: string
  barcode: string
  sellingPrice: string
  conflict?: boolean
}

interface Props {
  open: boolean
  onClose: () => void
  productId: string
  brandCode?: string
  categoryCode?: string
  existingVariants?: ProductVariantDto[]
}

function buildSku(brandCode: string, categoryCode: string, colourCode: string, sizeCode: string) {
  return `${brandCode}-${categoryCode}-${colourCode}-${sizeCode}`.toUpperCase()
}

export function VariantMatrixDialog({
  open,
  onClose,
  productId,
  brandCode = '',
  categoryCode = '',
  existingVariants = [],
}: Props) {
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()

  const [step, setStep] = useState<1 | 2>(1)
  const [selectedColourIds, setSelectedColourIds] = useState<string[]>([])
  const [selectedSizeIds, setSelectedSizeIds] = useState<string[]>([])
  const [rows, setRows] = useState<MatrixRow[]>([])
  const [conflictSkus, setConflictSkus] = useState<string[]>([])

  const { data: colours } = useQuery<ColourDto[]>({
    queryKey: ['colours'],
    queryFn: () => api.get('/api/colours'),
    staleTime: 5 * 60_000,
  })

  const { data: sizes } = useQuery<SizeDto[]>({
    queryKey: ['sizes'],
    queryFn: () => api.get('/api/sizes'),
    staleTime: 5 * 60_000,
  })

  // Reset when dialog opens
  useEffect(() => {
    if (open) {
      setStep(1)
      setSelectedColourIds([])
      setSelectedSizeIds([])
      setRows([])
      setConflictSkus([])
    }
  }, [open])

  const existingCombinations = new Set(
    existingVariants
      .filter((v) => v.status === 'Active')
      .map((v) => `${v.colourId}:${v.sizeId}`)
  )

  function toggleColour(id: string) {
    setSelectedColourIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    )
  }

  function toggleSize(id: string) {
    setSelectedSizeIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    )
  }

  function goToStep2() {
    if (!colours || !sizes) return
    const generated: MatrixRow[] = []
    let skipped = 0

    for (const colourId of selectedColourIds) {
      const colour = colours.find((c) => c.id === colourId)!
      for (const sizeId of selectedSizeIds) {
        const size = sizes.find((s) => s.id === sizeId)!
        const key = `${colourId}:${sizeId}`
        if (existingCombinations.has(key)) { skipped++; continue }
        generated.push({
          localId: key,
          colourId,
          colourName: colour.name,
          colourCode: colour.code,
          sizeId,
          sizeName: size.name,
          sizeCode: size.code,
          sku: brandCode && categoryCode
            ? buildSku(brandCode, categoryCode, colour.code, size.code)
            : '',
          barcode: '',
          sellingPrice: '',
        })
      }
    }

    if (generated.length === 0 && skipped > 0) {
      showError('All selected combinations already exist as active variants.')
      return
    }
    setRows(generated)
    setConflictSkus([])
    setStep(2)
  }

  function updateRow(localId: string, field: keyof MatrixRow, value: string) {
    setRows((prev) =>
      prev.map((r) => r.localId === localId ? { ...r, [field]: value, conflict: field === 'sku' ? false : r.conflict } : r)
    )
  }

  function removeRow(localId: string) {
    setRows((prev) => prev.filter((r) => r.localId !== localId))
  }

  const mutation = useMutation({
    mutationFn: () => {
      const body = rows.map((r) => ({
        sku: r.sku,
        barcode: r.barcode || null,
        colourId: r.colourId,
        sizeId: r.sizeId,
        sellingPrice: parseFloat(r.sellingPrice),
      }))
      return api.post(`/api/products/${productId}/variants/batch`, body)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products', productId, 'variants'] })
      showSuccess(`${rows.length} variant(s) created.`)
      onClose()
    },
    onError: (err: unknown) => {
      const error = err as Error & { status?: number }
      if (error.status === 409) {
        try {
          const body = JSON.parse(error.message.includes('{') ? error.message : '{}') as { conflictingSkus?: string[] }
          const skus: string[] = body.conflictingSkus ?? []
          setConflictSkus(skus)
          setRows((prev) => prev.map((r) => ({ ...r, conflict: skus.includes(r.sku) })))
        } catch {
          showError('Some SKUs already exist. Please review and fix conflicts.')
        }
      } else {
        showError(error.message ?? 'Failed to create variants.')
      }
    },
  })

  const canSubmit = rows.length > 0 &&
    rows.every((r) => r.sku.trim() && r.sellingPrice && parseFloat(r.sellingPrice) > 0)

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        Generate Variants — Step {step} of 2
      </DialogTitle>
      <DialogContent>
        {step === 1 && (
          <Box sx={{ display: 'flex', gap: 4, mt: 1 }}>
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Colours</Typography>
              <FormGroup>
                {colours?.map((c) => (
                  <FormControlLabel
                    key={c.id}
                    control={
                      <Checkbox
                        checked={selectedColourIds.includes(c.id)}
                        onChange={() => toggleColour(c.id)}
                        size="small"
                      />
                    }
                    label={`${c.name} (${c.code})`}
                  />
                ))}
              </FormGroup>
            </Box>
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Sizes</Typography>
              <FormGroup>
                {sizes?.map((s) => (
                  <FormControlLabel
                    key={s.id}
                    control={
                      <Checkbox
                        checked={selectedSizeIds.includes(s.id)}
                        onChange={() => toggleSize(s.id)}
                        size="small"
                      />
                    }
                    label={`${s.name} (${s.code})`}
                  />
                ))}
              </FormGroup>
            </Box>
          </Box>
        )}

        {step === 2 && (
          <Box>
            {conflictSkus.length > 0 && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {conflictSkus.length} SKU conflict(s) found. Edit the highlighted SKUs and try again.
              </Alert>
            )}
            {rows.length === 0 ? (
              <Typography color="text.secondary">No combinations to generate.</Typography>
            ) : (
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Colour</TableCell>
                    <TableCell>Size</TableCell>
                    <TableCell>SKU *</TableCell>
                    <TableCell>Barcode</TableCell>
                    <TableCell>Selling Price *</TableCell>
                    <TableCell />
                  </TableRow>
                </TableHead>
                <TableBody>
                  {rows.map((row) => (
                    <TableRow key={row.localId} sx={{ bgcolor: row.conflict ? 'error.lighter' : undefined }}>
                      <TableCell>{row.colourName}</TableCell>
                      <TableCell>{row.sizeName}</TableCell>
                      <TableCell>
                        <TextField
                          size="small"
                          value={row.sku}
                          onChange={(e) => updateRow(row.localId, 'sku', e.target.value)}
                          error={row.conflict || !row.sku.trim()}
                          helperText={row.conflict ? 'SKU already exists' : undefined}
                          sx={{ width: 180 }}
                        />
                      </TableCell>
                      <TableCell>
                        <TextField
                          size="small"
                          value={row.barcode}
                          onChange={(e) => updateRow(row.localId, 'barcode', e.target.value)}
                          sx={{ width: 130 }}
                        />
                      </TableCell>
                      <TableCell>
                        <TextField
                          size="small"
                          type="number"
                          value={row.sellingPrice}
                          onChange={(e) => updateRow(row.localId, 'sellingPrice', e.target.value)}
                          slotProps={{ input: { startAdornment: <InputAdornment position="start">$</InputAdornment>, inputProps: { min: 0, step: '0.01' } } }}
                          error={!row.sellingPrice || parseFloat(row.sellingPrice) <= 0}
                          sx={{ width: 120 }}
                        />
                      </TableCell>
                      <TableCell>
                        <IconButton size="small" onClick={() => removeRow(row.localId)}>
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </Box>
        )}
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        {step === 1 && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={goToStep2}
            disabled={selectedColourIds.length === 0 || selectedSizeIds.length === 0}
          >
            Preview ({selectedColourIds.length * selectedSizeIds.length} combinations)
          </Button>
        )}
        {step === 2 && (
          <>
            <Button onClick={() => setStep(1)}>← Back</Button>
            <Button
              variant="contained"
              onClick={() => mutation.mutate()}
              disabled={!canSubmit || mutation.isPending}
              startIcon={mutation.isPending ? <CircularProgress size={16} /> : undefined}
            >
              Generate {rows.length} Variant{rows.length !== 1 ? 's' : ''}
            </Button>
          </>
        )}
      </DialogActions>
    </Dialog>
  )
}
