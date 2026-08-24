# Stock Take & Inventory Management System — PRD

## 1. Product Overview

### 1.1 Purpose

The Stock Take & Inventory Management System is a web-based application for clothing retail businesses to manage products, product variants, goods receiving, inventory quantities, and stock-taking activities.

The initial MVP will focus on:

- Product and product variant management
- Goods receiving
- Inventory quantity management
- Stock taking
- Stock variance identification

The system will be designed as an extensible foundation for future capabilities such as purchase orders, multiple stores, transfers, sales integration, returns, and reporting.

### 1.2 Target Users

| User    | Responsibilities                                      |
| ------- | ----------------------------------------------------- |
| Admin   | Manage system configuration and users                 |
| Manager | Manage products, receiving, stock takes and approvals |
| Staff   | Receive goods and perform stock counts                |

### 1.3 Initial Business Assumptions

- The business initially operates a single retail store.
- A product can have multiple variants.
- A variant represents a countable/sellable SKU.
- Stock quantity belongs to inventory, not the product or product variant.
- Goods can be received without a purchase order.
- Purchase orders may be introduced later.
- A product must exist before stock can normally be received against it.
- Product creation during receiving may be supported as an explicit workflow.
- The system initially uses one currency.
- The application is primarily used through desktop, tablet and mobile web browsers.

---

# 2. Goals

## 2.1 MVP Goals

The MVP should allow the business to:

1. Maintain a product catalogue.
2. Define product variants/SKUs.
3. Record the selling price of variants.
4. Receive goods from suppliers.
5. Record the actual purchase cost of received goods.
6. Automatically update inventory when goods are received.
7. Perform a physical stock take.
8. Compare physical stock against system inventory.
9. Identify stock discrepancies.
10. Adjust inventory after an approved stock take.
11. Maintain sufficient history to understand how inventory changed.

## 2.2 Non-Goals for MVP

The following are intentionally outside the initial MVP:

- Multi-store management
- Warehouse management
- Sales/POS integration
- Customer management
- Supplier portal
- Advanced purchasing workflows
- Complex pricing/promotions
- Accounting integration
- Forecasting
- Demand planning
- Advanced inventory costing
- Native mobile applications

The database and architecture should allow these features to be added later.

---

# 3. Core Domain Model

The initial domain consists of:

```text
Product
   │
   └── ProductVariant / SKU
             │
             ├── GoodsReceiptLine
             │
             ├── Inventory
             │
             └── StockTakeItem

Supplier
   │
   └── GoodsReceipt
          │
          └── GoodsReceiptLine
```

The future purchasing flow will be:

```text
Supplier
   ↓
Purchase Order
   ↓
Purchase Order Line
   ↓
Goods Receipt
   ↓
Inventory
```

The MVP must also support:

```text
Supplier
   ↓
Goods Receipt
   ↓
Inventory
```

without requiring a purchase order.

---

# 4. Functional Requirements

## 4.1 Product Management

### Product

A Product represents a product/style rather than an individual stock item.

Example:

```text
Nike Basic T-Shirt
```

A product should contain:

- Product ID
- Product code
- Product name
- Description
- Brand
- Category
- Status
- Created date
- Updated date

### Product Variant

A Product Variant represents the actual sellable and countable SKU.

Example:

```text
Nike Basic T-Shirt
 ├── Black / Small
 ├── Black / Medium
 ├── Black / Large
 ├── White / Small
 ├── White / Medium
 └── White / Large
```

A variant should contain:

- Variant ID
- Product ID
- SKU
- Barcode
- Colour
- Size
- Selling price
- Status
- Created date
- Updated date

### Requirements

The system must allow authorised users to:

- Create products.
- Edit products.
- Activate/deactivate products.
- Create variants.
- Edit variants.
- Assign SKU.
- Assign barcode.
- Assign colour.
- Assign size.
- Set selling price.
- Search products.
- Search by SKU.
- Search by barcode.
- Filter by category/brand/status.

### Business Rules

- SKU must be unique.
- Barcode should be unique where supplied.
- A product can have multiple variants.
- A variant belongs to exactly one product.
- An inactive variant cannot be used for new receiving transactions.
- A variant should not be deleted if it has historical inventory or transaction data.

---

# 5. Reference Data

The MVP should support the following reference data.

## 5.1 Brand

Example:

```text
Nike
Adidas
Puma
```

Fields:

- ID
- Name
- Status

## 5.2 Category

Categories should support hierarchy.

Example:

```text
Men
 ├── T-Shirts
 ├── Shirts
 └── Jeans

Women
 ├── Tops
 ├── Dresses
 └── Jeans
```

Fields:

- ID
- Parent category ID
- Name
- Status

## 5.3 Colour

Fields:

- ID
- Name
- Code
- Status

## 5.4 Size

Fields:

- ID
- Name
- Code
- Sort order
- Status

Example:

```text
XS
S
M
L
XL
```

---

# 6. Supplier Management

The system must maintain suppliers because goods receiving needs to identify where stock came from.

Supplier fields:

- Supplier ID
- Supplier code
- Name
- Email
- Phone
- Status
- Created date
- Updated date

The MVP does not require a supplier portal.

---

# 7. Goods Receiving

## 7.1 Purpose

Goods Receiving records stock that physically arrives at the store.

Goods receiving must work **without a purchase order**.

Example:

```text
GR-0001

Supplier: ABC Clothing

SKU          Quantity     Unit Cost
-------------------------------------
TS-BLK-S        50          $12.00
TS-BLK-M        80          $12.00
TS-WHT-M        30          $13.00
```

## 7.2 Goods Receipt

Fields:

- Receipt ID
- Receipt number
- Supplier
- Received date
- Status
- Received by
- Created date
- Updated date

Statuses:

```text
Draft
Received
Cancelled
```

## 7.3 Goods Receipt Line

Fields:

- Receipt line ID
- Goods receipt ID
- Product variant ID
- Quantity
- Unit cost

### Business Rules

- A goods receipt must contain at least one line.
- Quantity must be greater than zero.
- Unit cost cannot be negative.
- Product variant must exist and be active.
- Completing a goods receipt updates inventory.
- A completed goods receipt cannot be edited directly.
- Corrections should be handled through an adjustment/reversal mechanism.

---

# 8. Inventory

Inventory represents the current system quantity of each product variant.

For the initial single-store MVP:

```text
ProductVariant
      │
      ▼
Inventory
      │
      └── Quantity
```

Example:

```text
SKU: TS-BLK-M
Quantity: 80
```

Inventory should contain:

- ID
- Product variant ID
- Quantity
- Updated date

A unique constraint should exist on:

```text
ProductVariant
```

because there is currently only one store.

The design should allow a future `Location`/`Store` field to be added.

---

# 9. Inventory Transactions

Inventory changes should be traceable.

The system should maintain an inventory transaction/history record.

Transaction types may include:

```text
GoodsReceived
StockTakeAdjustment
ManualAdjustment
Sale              -- future
Return            -- future
TransferIn        -- future
TransferOut       -- future
```

Example:

```text
TS-BLK-M

+80  GoodsReceived
-3   StockTakeAdjustment
+20  GoodsReceived
```

This allows the current quantity to be understood from historical activity.

---

# 10. Purchase Orders — Future Capability

Purchase Orders are not mandatory for MVP receiving.

The future model will be:

```text
PurchaseOrder
      │
      └── PurchaseOrderLine
                │
                ▼
          GoodsReceipt
                │
                ▼
             Inventory
```

A purchase order line should contain:

- Product variant
- Ordered quantity
- Unit cost
- Received quantity

Goods receiving should be able to reference a purchase order when one exists.

Therefore the Goods Receipt should support an optional:

```text
purchase_order_id
```

This allows both:

```text
Direct Receiving
```

and:

```text
PO → Receiving
```

---

# 11. Stock Take

## 11.1 Purpose

Stock Take allows staff to compare physical stock with system inventory.

Example:

```text
System quantity: 100
Physical count:   97
Variance:          -3
```

## 11.2 Stock Take

Fields:

- Stock take ID
- Reference number
- Status
- Start date
- Completion date
- Created by
- Completed by
- Approved by
- Created date
- Updated date

Statuses:

```text
Draft
InProgress
Completed
Approved
Cancelled
```

## 11.3 Stock Take Item

Each product variant being counted becomes a stock take item.

Fields:

- Stock take item ID
- Stock take ID
- Product variant ID
- Expected quantity
- Counted quantity
- Variance
- Status

Variance:

```text
Counted Quantity - Expected Quantity
```

Example:

```text
SKU        Expected    Counted    Variance
-------------------------------------------
TS-BLK-S       20         20          0
TS-BLK-M       30         28         -2
TS-BLK-L       15         17         +2
```

---

# 12. Stock Counting

The stock-counting interface should support:

- Product search
- SKU search
- Barcode scanning
- Quantity entry
- Saving counts
- Resuming an incomplete stock take
- Viewing uncounted items
- Viewing variance
- Recounting

The UI should be responsive and suitable for tablet/mobile use.

Example workflow:

```text
Scan Barcode
      ↓
Find Product Variant
      ↓
Display SKU / Size / Colour
      ↓
Enter Physical Quantity
      ↓
Save Count
```

---

# 13. Recount

The system should allow a product variant to be counted more than once.

For example:

```text
Initial Count: 28
Recount:       29
Final Count:   29
```

A count history should be retained rather than silently overwriting the previous count.

This supports auditability.

---

# 14. Stock Take Approval

After counting is complete:

```text
In Progress
     ↓
Completed
     ↓
Manager Review
     ↓
Approved
```

Once approved:

- The final physical quantity becomes the inventory quantity.
- An inventory adjustment transaction is created.
- The stock take becomes read-only.

Example:

```text
Inventory before:       100
Physical count:          97
Adjustment:               -3
Inventory after:         97
```

---

# 15. Audit Requirements

The system should record:

- Created by
- Created date
- Updated by
- Updated date
- Received by
- Counted by
- Completed by
- Approved by

Important inventory events should be traceable.

Users should be able to determine:

> Who changed this quantity and why?

---

# 16. User Interface

## Main Navigation

The MVP navigation should be:

```text
Dashboard

Products
  ├── Products
  └── Variants

Receiving
  ├── Goods Receipts
  └── New Receipt

Inventory

Stock Take
  ├── Stock Takes
  └── New Stock Take

Suppliers

Settings
```

Purchase Orders can be added later:

```text
Purchasing
  ├── Purchase Orders
  └── Suppliers
```

---

# 17. Dashboard

The initial dashboard should provide simple information:

```text
Products             1,250
Variants             4,850
Current Stock       12,430

Open Stock Takes         2
Recent Receipts          8
```

Advanced analytics are outside the MVP.

---

# 18. Search and Barcode

Product search should support:

- Product name
- Product code
- SKU
- Barcode

Barcode scanning should populate the product variant automatically.

The system should be designed so that a standard device camera or barcode scanner can be used without requiring a dedicated native mobile application.

---

# 19. Database Schema

Initial tables:

```text
brands
categories
colours
sizes

products
product_variants

suppliers

goods_receipts
goods_receipt_lines

inventory
inventory_transactions

stock_takes
stock_take_items
stock_take_counts
```

Future:

```text
purchase_orders
purchase_order_lines

locations
stores

sales
returns
transfers
```

---

# 20. Key Relationships

```text
Brand
   │
   ▼
Product
   │
   ▼
ProductVariant
   │
   ├──────────────► Inventory
   │
   ├──────────────► GoodsReceiptLine
   │
   └──────────────► StockTakeItem


Supplier
   │
   ▼
GoodsReceipt
   │
   ▼
GoodsReceiptLine
   │
   ▼
ProductVariant
```

Future:

```text
Supplier
   │
   ▼
PurchaseOrder
   │
   ▼
PurchaseOrderLine
   │
   ▼
GoodsReceipt
   │
   ▼
Inventory
```

---

# 21. Pricing

For MVP:

### Selling price

Stored on:

```text
ProductVariant
```

Example:

```text
ProductVariant
----------------------
SKU
SellingPrice
```

### Purchase cost

Stored on:

```text
GoodsReceiptLine
```

Example:

```text
GoodsReceiptLine
----------------------
ProductVariant
Quantity
UnitCost
```

This preserves purchase-cost history.

Purchase cost should **not** be treated as a permanent property of the Product.

Future pricing capabilities can include:

- Price history
- Multiple selling prices
- Promotions
- Store-specific pricing
- Effective dates
- Supplier-specific costs

---

# 22. Technical Requirements

## Frontend

```text
React
TypeScript
Vite
Material UI
TanStack Query
React Hook Form
Zod
```

The frontend should be responsive and PWA-ready.

## Backend

```text
ASP.NET Core
.NET 10
C#
Entity Framework Core
PostgreSQL
ASP.NET Core Identity
```

Use simple application services rather than introducing unnecessary CQRS/event-driven infrastructure.

## Architecture

Use a lightweight modular monolith:

```text
Web/API
   │
   ├── Products
   ├── Receiving
   ├── Inventory
   └── StockTake
          │
          ▼
      Application
          │
          ▼
        Domain
          │
          ▼
    Infrastructure
          │
          ▼
      PostgreSQL
```

---

# 23. Frontend and Backend Hosting

For the MVP, frontend and backend should be deployed together.

```text
                    Internet
                       │
                       ▼
              ┌─────────────────┐
              │ ASP.NET Core    │
              │                 │
              │ React SPA       │
              │ Web API         │
              └────────┬────────┘
                       │
                       ▼
                 PostgreSQL
```

React is built into static files and served by ASP.NET Core.

Example:

```text
https://inventory.example.com/
https://inventory.example.com/api/products
https://inventory.example.com/api/inventory
https://inventory.example.com/api/goods-receipts
```

This provides:

- One deployment
- One domain
- No frontend/backend CORS configuration
- Simple authentication
- Lower infrastructure complexity

---

# 24. Hosting

Preferred initial options:

### Option 1

Azure App Service + managed PostgreSQL

```text
GitHub
   ↓
GitHub Actions
   ↓
Azure App Service
   ↓
PostgreSQL
```

### Option 2

Render Web Service + PostgreSQL

```text
GitHub
   ↓
Render
   ├── .NET application
   └── PostgreSQL
```

The application should not require Kubernetes, ECS/EKS, or other container orchestration for MVP.

---

# 25. CI/CD

GitHub Actions should perform:

```text
Pull Request
    ↓
Build
    ↓
Unit Tests
    ↓
Integration Tests
```

For the main branch:

```text
Build
  ↓
Test
  ↓
Publish
  ↓
Deploy
```

Database migrations should be managed through EF Core migrations.

---

# 26. Non-Functional Requirements

## Performance

The system should:

- Load normal pages within a few seconds.
- Support efficient SKU/barcode searches.
- Support thousands of products and variants.
- Support large stock-take sessions without loading the entire catalogue into the browser.

## Availability

The MVP should rely on managed hosting and managed PostgreSQL where possible.

## Security

- HTTPS required.
- Passwords must never be stored in plain text.
- Role-based access should be implemented.
- API endpoints must enforce authorization.
- Sensitive configuration must be stored outside source control.
- Database credentials must not be exposed to the frontend.

## Auditability

Inventory-changing operations must have a recorded source and user.

## Backup

Managed PostgreSQL backups should be enabled.

---

# 27. MVP Success Criteria

The MVP will be considered successful when a store employee can complete the following workflow:

```text
1. Create Product
       ↓
2. Create Product Variant
       ↓
3. Set Selling Price
       ↓
4. Select Supplier
       ↓
5. Receive Goods
       ↓
6. Record Quantity + Purchase Cost
       ↓
7. Inventory Automatically Increases
       ↓
8. Start Stock Take
       ↓
9. Scan/Search Product Variant
       ↓
10. Enter Physical Count
       ↓
11. Review Variances
       ↓
12. Manager Approves
       ↓
13. Inventory Is Adjusted
```

The most important design principle for the MVP is:

> **ProductVariant identifies what the stock item is; GoodsReceipt records what arrived; Inventory records what the system currently has; StockTake records what was physically counted.**

This separation gives you a simple first release while leaving a clean path to add **Purchase Orders, multiple stores, transfers, sales, returns and warehouse management** later.
