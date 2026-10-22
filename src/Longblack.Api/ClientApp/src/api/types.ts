export interface BrandDto {
  id: string
  name: string
  code: string
  status: string
}

export interface CategoryDto {
  id: string
  parentCategoryId: string | null
  name: string
  code: string
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

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface GoodsReceiptLineDto {
  id: string
  goodsReceiptId: string
  productVariantId: string
  sku: string
  barcode: string | null
  quantity: number
  unitCost: number
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
}

export interface SupplierDto {
  id: string
  code: string
  name: string
  email: string
  phone: string
  status: string
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
}

export interface GoodsReceiptDto {
  id: string
  receiptNumber: string
  storeId: string
  storeName: string
  supplierId: string
  supplierName: string
  purchaseOrderId: string | null
  purchaseOrderNumber: string | null
  receivedDate: string
  status: string
  receivedBy: string | null
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
  lines: GoodsReceiptLineDto[]
  warnings: string[]
}

export interface PurchaseOrderLineDto {
  id: string
  purchaseOrderId: string
  productVariantId: string
  sku: string
  barcode: string | null
  quantity: number
  unitCost: number
  receivedQuantity: number
  outstandingQuantity: number
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
}

export interface PurchaseOrderSendDto {
  id: string
  recipient: string
  cc: string | null
  succeeded: boolean
  error: string | null
  sentAt: string
  sentBy: string
}

export interface LinkedGoodsReceiptDto {
  id: string
  receiptNumber: string
  status: string
  receivedDate: string
}

export interface PurchaseOrderDto {
  id: string
  poNumber: string | null
  supplierId: string
  supplierName: string
  supplierEmail: string
  expectedDeliveryDate: string | null
  notes: string | null
  status: string
  isOverdue: boolean
  issuedAt: string | null
  issuedBy: string | null
  sentAt: string | null
  sentBy: string | null
  total: number
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
  lines: PurchaseOrderLineDto[]
  sends: PurchaseOrderSendDto[]
  goodsReceipts: LinkedGoodsReceiptDto[]
}

export interface InventoryDto {
  productVariantId: string
  sku: string
  barcode: string | null
  productName: string
  colourName: string | null
  sizeName: string | null
  status: string
  storeId: string
  storeName: string
  quantity: number
  updatedAt: string | null
}

export interface InventoryTransactionDto {
  id: string
  productVariantId: string
  storeId: string
  storeName: string
  type: string
  quantityDelta: number
  sourceType: string
  sourceId: string
  createdAt: string
  createdBy: string
}

export interface StockTakeItemDto {
  id: string
  stockTakeId: string
  productVariantId: string
  sku: string
  barcode: string | null
  productName: string
  colourName: string | null
  sizeName: string | null
  expectedQuantity: number
  countedQuantity: number | null
  variance: number | null
  status: string
}

export interface StockTakeDto {
  id: string
  referenceNumber: string
  storeId: string
  storeName: string
  brandId: string | null
  brandName: string | null
  categoryId: string | null
  categoryName: string | null
  status: string
  startDate: string | null
  completionDate: string | null
  completedBy: string | null
  approvedDate: string | null
  approvedBy: string | null
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
  items: StockTakeItemDto[]
}

export interface StockTakeCountDto {
  id: string
  stockTakeItemId: string
  quantity: number
  countedAt: string
  countedBy: string
}

export interface StoreDto {
  id: string
  name: string
  type: string
  status: string
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
}

export interface StockTransferLineDto {
  id: string
  stockTransferId: string
  productVariantId: string
  sku: string
  barcode: string | null
  productName: string
  dispatchedQuantity: number
  receivedQuantity: number | null
  variance: number | null
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
}

export interface StockTransferDto {
  id: string
  transferNumber: string
  sourceStoreId: string
  sourceStoreName: string
  destinationStoreId: string
  destinationStoreName: string
  status: string
  dispatchedDate: string | null
  dispatchedBy: string | null
  receivedDate: string | null
  receivedBy: string | null
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
  lines: StockTransferLineDto[]
}

export interface SaleLineDto {
  id: string
  saleId: string
  productVariantId: string
  sku: string
  barcode: string | null
  productName: string
  quantity: number
  unitPrice: number
  discount: number
  lineTotal: number
  availableQuantity: number | null
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
}

export interface SaleDto {
  id: string
  saleNumber: string
  storeId: string
  storeName: string
  status: string
  paymentType: string | null
  paymentAmount: number | null
  total: number
  completedDate: string | null
  completedBy: string | null
  voidedDate: string | null
  voidedBy: string | null
  createdAt: string
  updatedAt: string
  createdBy: string
  updatedBy: string
  lines: SaleLineDto[]
}
