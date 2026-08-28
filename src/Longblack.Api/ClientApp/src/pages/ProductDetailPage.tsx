import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { ProductDto, ProductVariantDto } from '../api/types'
import { ConfirmDialog } from '../components/products/ConfirmDialog'
import { ProductFormDialog } from '../components/products/ProductFormDialog'
import { VariantFormDialog } from '../components/products/VariantFormDialog'
import { useAuth } from '../contexts/AuthContext'
import { useSnackbar } from '../contexts/SnackbarContext'

function useProduct(id: string) {
  return useQuery<ProductDto>({
    queryKey: ['products', id],
    queryFn: () => api.get(`/api/products/${id}`),
  })
}

function useVariants(productId: string) {
  return useQuery<ProductVariantDto[]>({
    queryKey: ['products', productId, 'variants'],
    queryFn: () => api.get(`/api/products/${productId}/variants?status=`),
  })
}

export function ProductDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()
  const { showSuccess, showError } = useSnackbar()
  const queryClient = useQueryClient()
  const canWrite = user?.roles.some((r) => r === 'Manager' || r === 'Admin') ?? false

  const [editOpen, setEditOpen] = useState(false)
  const [deactivateOpen, setDeactivateOpen] = useState(false)
  const [addVariantOpen, setAddVariantOpen] = useState(false)
  const [editVariant, setEditVariant] = useState<ProductVariantDto | undefined>()
  const [deactivateVariant, setDeactivateVariant] = useState<ProductVariantDto | undefined>()

  const { data: product, isLoading: productLoading, isError: productError } = useProduct(id!)
  const { data: variants, isLoading: variantsLoading, isError: variantsError } = useVariants(id!)

  const statusMutation = useMutation({
    mutationFn: (status: string) =>
      api.patch(`/api/products/${id}/status`, { status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      showSuccess('Product status updated.')
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to update status.'),
  })

  const variantStatusMutation = useMutation({
    mutationFn: ({ variantId, status }: { variantId: string; status: string }) =>
      api.patch(`/api/products/${id}/variants/${variantId}/status`, { status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products', id, 'variants'] })
      showSuccess('Variant status updated.')
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to update variant status.'),
  })

  if (productLoading) return <CircularProgress />
  if (productError || !product) return <Typography color="error">Product not found.</Typography>

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
        <IconButton component={Link} to="/products" size="small">
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5">{product.name}</Typography>
        <Chip
          label={product.status}
          size="small"
          color={product.status === 'Active' ? 'success' : 'default'}
          sx={{ ml: 1 }}
        />
      </Box>

      {/* Product fields */}
      <Box sx={{ display: 'grid', gridTemplateColumns: '160px 1fr', gap: 1, mb: 3, maxWidth: 600 }}>
        <Typography color="text.secondary">Product Code</Typography>
        <Typography>{product.productCode}</Typography>
        <Typography color="text.secondary">Description</Typography>
        <Typography>{product.description ?? '—'}</Typography>
        <Typography color="text.secondary">Brand</Typography>
        <Typography>{product.brandName ?? '—'}</Typography>
        <Typography color="text.secondary">Category</Typography>
        <Typography>{product.categoryName ?? '—'}</Typography>
      </Box>

      {/* Action buttons */}
      {canWrite && (
        <Box sx={{ display: 'flex', gap: 1, mb: 3 }}>
          <Button variant="outlined" onClick={() => setEditOpen(true)}>Edit Product</Button>
          {product.status === 'Active' ? (
            <Button variant="outlined" color="warning" onClick={() => setDeactivateOpen(true)}>
              Deactivate
            </Button>
          ) : (
            <Button variant="outlined" color="success" onClick={() => statusMutation.mutate('Active')}>
              Reactivate
            </Button>
          )}
        </Box>
      )}

      <ProductFormDialog open={editOpen} onClose={() => setEditOpen(false)} product={product} />
      <ConfirmDialog
        open={deactivateOpen}
        onClose={() => setDeactivateOpen(false)}
        title="Deactivate Product"
        message={`Are you sure you want to deactivate "${product.name}"?`}
        confirmLabel="Deactivate"
        onConfirm={() => statusMutation.mutate('Inactive')}
      />

      <Divider sx={{ my: 3 }} />

      {/* Variants section */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Variants</Typography>
        {canWrite && (
          <Button variant="contained" size="small" onClick={() => setAddVariantOpen(true)}>
            Add Variant
          </Button>
        )}
      </Box>

      {variantsLoading && <CircularProgress size={24} />}
      {variantsError && <Typography color="error">Failed to load variants.</Typography>}

      {!variantsLoading && !variantsError && variants && (
        variants.length === 0 ? (
          <Typography color="text.secondary">No variants yet.</Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>SKU</TableCell>
                <TableCell>Barcode</TableCell>
                <TableCell>Colour</TableCell>
                <TableCell>Size</TableCell>
                <TableCell align="right">Selling Price</TableCell>
                <TableCell>Status</TableCell>
                {canWrite && <TableCell align="right">Actions</TableCell>}
              </TableRow>
            </TableHead>
            <TableBody>
              {variants.map((v) => (
                <TableRow key={v.id} sx={{ opacity: v.status === 'Inactive' ? 0.5 : 1 }}>
                  <TableCell>{v.sku}</TableCell>
                  <TableCell>{v.barcode ?? '—'}</TableCell>
                  <TableCell>{v.colourName ?? '—'}</TableCell>
                  <TableCell>{v.sizeName ?? '—'}</TableCell>
                  <TableCell align="right">${v.sellingPrice.toFixed(2)}</TableCell>
                  <TableCell>
                    <Chip
                      label={v.status}
                      size="small"
                      color={v.status === 'Active' ? 'success' : 'default'}
                    />
                  </TableCell>
                  {canWrite && (
                    <TableCell align="right">
                      <Button size="small" onClick={() => setEditVariant(v)}>Edit</Button>
                      {v.status === 'Active' ? (
                        <Button size="small" color="warning" onClick={() => setDeactivateVariant(v)}>
                          Deactivate
                        </Button>
                      ) : (
                        <Button size="small" color="success"
                          onClick={() => variantStatusMutation.mutate({ variantId: v.id, status: 'Active' })}>
                          Reactivate
                        </Button>
                      )}
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )
      )}

      {/* Variant dialogs */}
      <VariantFormDialog
        open={addVariantOpen}
        onClose={() => setAddVariantOpen(false)}
        productId={id!}
      />
      <VariantFormDialog
        open={!!editVariant}
        onClose={() => setEditVariant(undefined)}
        productId={id!}
        variant={editVariant}
      />
      <ConfirmDialog
        open={!!deactivateVariant}
        onClose={() => setDeactivateVariant(undefined)}
        title="Deactivate Variant"
        message={`Are you sure you want to deactivate SKU "${deactivateVariant?.sku}"?`}
        confirmLabel="Deactivate"
        onConfirm={() => deactivateVariant && variantStatusMutation.mutate({ variantId: deactivateVariant.id, status: 'Inactive' })}
      />
    </Box>
  )
}

