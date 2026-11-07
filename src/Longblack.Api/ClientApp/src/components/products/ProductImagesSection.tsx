import { Box, Button, Typography } from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef } from 'react'
import { api } from '../../api/client'
import type { ProductImageDto, ProductVariantDto } from '../../api/types'
import { useSnackbar } from '../../contexts/SnackbarContext'
import { ProductThumbnail } from './ProductThumbnail'

interface Props {
  productId: string
  variants: ProductVariantDto[] | undefined
  canWrite: boolean
}

interface SlotProps {
  label: string
  hint: string
  image: ProductImageDto | undefined
  canWrite: boolean
  basePath: string
  productId: string
}

function ImageSlot({ label, hint, image, canWrite, basePath, productId }: SlotProps) {
  const { showError, showSuccess } = useSnackbar()
  const queryClient = useQueryClient()
  const inputRef = useRef<HTMLInputElement>(null)

  function refresh() {
    queryClient.invalidateQueries({ queryKey: ['products'] })
    queryClient.invalidateQueries({ queryKey: ['stock-takes'] })
  }

  const uploadMutation = useMutation({
    mutationFn: (file: File) => api.putFile<ProductImageDto>(`/api/products/${productId}${basePath}`, file),
    onSuccess: () => {
      refresh()
      showSuccess('Image saved.')
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to upload image.'),
  })

  const removeMutation = useMutation({
    mutationFn: () => api.delete(`/api/products/${productId}${basePath}`),
    onSuccess: () => {
      refresh()
      showSuccess('Image removed.')
    },
    onError: (err: unknown) => showError((err as Error).message ?? 'Failed to remove image.'),
  })

  const busy = uploadMutation.isPending || removeMutation.isPending

  return (
    <Box sx={{ width: 160 }}>
      <Box sx={{ mb: 1 }}>
        <ProductThumbnail url={image?.imageUrl} size={160} alt={label} />
      </Box>
      <Typography variant="body2" sx={{ fontWeight: 500 }}>
        {label}
      </Typography>
      <Typography variant="caption" color="text.secondary" component="div">
        {image ? hint : 'No image'}
      </Typography>
      {canWrite && (
        <Box sx={{ display: 'flex', gap: 0.5, mt: 0.5 }}>
          <input
            ref={inputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            hidden
            onChange={(e) => {
              const file = e.target.files?.[0]
              e.target.value = ''
              if (file) uploadMutation.mutate(file)
            }}
          />
          <Button size="small" disabled={busy} onClick={() => inputRef.current?.click()}>
            {image ? 'Replace' : 'Upload'}
          </Button>
          {image && (
            <Button size="small" color="error" disabled={busy} onClick={() => removeMutation.mutate()}>
              Remove
            </Button>
          )}
        </Box>
      )}
    </Box>
  )
}

export function ProductImagesSection({ productId, variants, canWrite }: Props) {
  const { data: images } = useQuery<ProductImageDto[]>({
    queryKey: ['products', productId, 'images'],
    queryFn: () => api.get(`/api/products/${productId}/images`),
  })

  const colours = new Map<string, string>()
  for (const v of variants ?? []) colours.set(v.colourId, v.colourName ?? 'Unknown')

  return (
    <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
      <ImageSlot
        label="Product Image"
        hint="Fallback for colours without a photo"
        image={images?.find((i) => i.colourId === null)}
        canWrite={canWrite}
        basePath="/image"
        productId={productId}
      />
      {[...colours].map(([colourId, name]) => (
        <ImageSlot
          key={colourId}
          label={name}
          hint="Shared by all sizes"
          image={images?.find((i) => i.colourId === colourId)}
          canWrite={canWrite}
          basePath={`/colours/${colourId}/image`}
          productId={productId}
        />
      ))}
    </Box>
  )
}
