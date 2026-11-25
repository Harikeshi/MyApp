const MAX_SIDE = 1280;
const QUALITY = 0.85;
const MAX_SOURCE_BYTES = 15 * 1024 * 1024;

/**
 * Картинку уменьшаем до 1280 пикселей по большей стороне и сохраняем как JPEG: запись целиком лежит в базе,
 * фото с телефона на несколько мегабайт ей ни к чему. Прозрачность заливается белым, у GIF остаётся первый кадр.
 */
export async function imageToDataUrl(file: File): Promise<string> {
  if (!file.type.startsWith("image/")) throw new Error(`«${file.name}» не картинка`);
  if (file.size > MAX_SOURCE_BYTES) throw new Error(`Картинка «${file.name}» больше ${MAX_SOURCE_BYTES / 1024 / 1024} МБ`);

  let bitmap: ImageBitmap;
  try {
    bitmap = await createImageBitmap(file);
  } catch {
    throw new Error(`Не удалось открыть картинку «${file.name}»`);
  }

  const scale = Math.min(1, MAX_SIDE / Math.max(bitmap.width, bitmap.height));
  const canvas = document.createElement("canvas");
  canvas.width = Math.max(1, Math.round(bitmap.width * scale));
  canvas.height = Math.max(1, Math.round(bitmap.height * scale));

  const ctx = canvas.getContext("2d");
  if (!ctx) throw new Error("Браузер не умеет обрабатывать картинки");
  ctx.fillStyle = "#fff";
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  ctx.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
  bitmap.close();

  return canvas.toDataURL("image/jpeg", QUALITY);
}
