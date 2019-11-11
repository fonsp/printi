import skimage
import numpy as np
import sys

def quantile_filter(image):
  quantile_values = np.quantile(image, np.linspace(0, 1, 256))

  pixel_map = [np.argmin(np.abs(quantile_values - val)) for val in np.linspace(0, 1, 256)]
  pixel_map = np.array(pixel_map).astype(np.uint8)

  result = np.zeros_like(image, dtype=np.uint8)
  for i, row in enumerate((255*image).astype(np.uint8)):
    result[i] = pixel_map[row]
  return result

image = skimage.io.imread(sys.argv[-2], as_gray=True)

w, h = image.shape
if max(w, h) <= 576:
    image_resized = image
elif min(w, h) <= 576:
    image_resized = image
else:
    scale_ratio = 576 / min(w, h)
    image_resized = skimage.transform.resize(image, (int(w*scale_ratio), int(h*scale_ratio)), anti_aliasing=False)

image_filtered = quantile_filter(image_resized)
skimage.io.imsave(sys.argv[-1], image_filtered)
