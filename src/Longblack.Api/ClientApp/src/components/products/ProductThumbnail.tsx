import ImageOutlinedIcon from '@mui/icons-material/ImageOutlined'
import { Box } from '@mui/material'

interface Props {
  url?: string | null
  size?: number
  alt?: string
}

// Falls back to a placeholder icon when there is no Resolved Image.
export function ProductThumbnail({ url, size = 40, alt = '' }: Props) {
  return url ? (
    <Box
      component="img"
      src={url}
      alt={alt}
      loading="lazy"
      sx={{ width: size, height: size, objectFit: 'cover', borderRadius: 1, display: 'block' }}
    />
  ) : (
    <Box
      sx={{
        width: size,
        height: size,
        borderRadius: 1,
        bgcolor: 'action.hover',
        color: 'text.disabled',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <ImageOutlinedIcon fontSize="small" />
    </Box>
  )
}
