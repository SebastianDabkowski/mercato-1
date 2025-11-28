# Product Image Uploads

This directory is used for storing product images uploaded by sellers.

## Structure

```
uploads/
  products/
    {product-id}/
      {image-id}.jpg           # Original image
      {image-id}_thumb.jpg     # Thumbnail (150x150)
      {image-id}_opt.jpg       # Optimized version (max 1200x1200)
```

## Notes

- Images are organized by product ID for easy management
- Original files are preserved
- Thumbnail and optimized versions are generated automatically
- All processed images are saved as JPEG with 85% quality
