CREATE TABLE IF NOT EXISTS product_images (
  id uuid PRIMARY KEY,
  product_id uuid NOT NULL REFERENCES products(id),
  colour_id uuid NULL REFERENCES colours(id),
  file_name text NOT NULL,
  thumbnail_file_name text NOT NULL,
  created_at timestamptz NOT NULL,
  updated_at timestamptz NOT NULL,
  created_by text NOT NULL,
  updated_by text NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_product_images_product_colour
  ON product_images (product_id, colour_id) NULLS NOT DISTINCT;