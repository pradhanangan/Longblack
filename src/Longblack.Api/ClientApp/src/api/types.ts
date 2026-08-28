export interface BrandDto {
  id: string
  name: string
  status: string
}

export interface CategoryDto {
  id: string
  parentCategoryId: string | null
  name: string
  status: string
}

export interface ColourDto {
  id: string
  name: string
  code: string
  status: string
}

export interface SizeDto {
  id: string
  name: string
  code: string
  sortOrder: number
  status: string
}

export interface ProductVariantDto {
  id: string
  productId: string
  sku: string
  barcode: string | null
  colourId: string
  colourName: string | null
  sizeId: string
  sizeName: string | null
  sellingPrice: number
  status: string
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
}

export interface ProductDto {
  id: string
  productCode: string
  name: string
  description: string | null
  brandId: string | null
  brandName: string | null
  categoryId: string | null
  categoryName: string | null
  status: string
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
  variants?: ProductVariantDto[]
}
