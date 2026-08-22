from __future__ import annotations

from collections import deque
from dataclasses import dataclass
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = ROOT / "DesktopPetOrHuman" / "Assets" / "Sheets"
OUT = ROOT / "DesktopPetOrHuman" / "Assets" / "Characters"


@dataclass(frozen=True)
class CharacterSheet:
    key: str
    display_name: str
    source: str
    grid: tuple[int, int]
    cells: dict[str, tuple[int, ...]]
    transparent: bool = True


SHEETS = [
    CharacterSheet(
        "miaobaibai",
        "喵白白",
        "ChatGPT Image 2026年8月21日 下午07_10_26.png",
        (4, 3),
        {"idle": (0, 0), "happy": (3, 1), "sleep": (1, 2)},
    ),
    CharacterSheet(
        "miaobubu",
        "喵布布",
        "ChatGPT Image 2026年8月21日 下午07_13_34.png",
        (4, 3),
        {"idle": (0, 0), "happy": (3, 1), "sleep": (370, 710, 735, 1050)},
    ),
    CharacterSheet(
        "xiaotu",
        "小塗",
        "ChatGPT Image 2026年8月21日 下午07_16_07.png",
        (4, 3),
        {"idle": (0, 0), "happy": (1065, 325, 1448, 750), "sleep": (2, 2)},
    ),
    CharacterSheet(
        "laowangmao",
        "Old Wang Cat",
        "ChatGPT Image 2026年8月22日 上午12_10_53.png",
        (4, 3),
        {"idle": (0, 0), "happy": (0, 1), "sleep": (2, 0)},
    ),
    CharacterSheet(
        "fengxiong",
        "鋒兄",
        "ChatGPT Image 2026年8月21日 下午07_19_31.png",
        (4, 3),
        {"idle": (0, 0), "happy": (3, 1), "sleep": (2, 2)},
    ),
    CharacterSheet(
        "xiaoying",
        "小英",
        "ChatGPT Image 2026年8月22日 上午12_11_08.png",
        (4, 3),
        {"idle": (0, 0), "happy": (3, 0), "sleep": (2, 1)},
    ),
    CharacterSheet(
        "miaoniang",
        "喵娘",
        "ChatGPT Image 2026年8月22日 上午12_19_17.png",
        (4, 3),
        {"idle": (0, 0), "happy": (0, 1), "sleep": (1120, 355, 1530, 672)},
    ),
    CharacterSheet(
        "tuge",
        "塗哥",
        "ChatGPT Image 2026年8月22日 上午12_23_33.png",
        (4, 3),
        {"idle": (0, 0), "happy": (940, 15, 1254, 380), "sleep": (3, 2)},
    ),
    CharacterSheet(
        "yamei",
        "牙妹",
        "ChatGPT Image 2026年8月22日 上午12_30_17.png",
        (4, 3),
        {"idle": (0, 0), "happy": (3, 0), "sleep": (360, 400, 805, 690)},
    ),
    CharacterSheet(
        "yumei",
        "魚妹",
        "ChatGPT Image 2026年8月22日 上午12_31_45.png",
        (4, 3),
        {"idle": (0, 0), "happy": (2, 1), "sleep": (0, 2)},
    ),
    CharacterSheet(
        "gugugaga",
        "ググガガ",
        "ChatGPT Image 2026年8月22日 上午12_38_24.png",
        (4, 3),
        {"idle": (0, 0), "happy": (3, 0), "sleep": (0, 1)},
    ),
]


def near_background(pixel: tuple[int, int, int, int], transparent_sheet: bool) -> bool:
    r, g, b, a = pixel
    if a == 0:
        return True
    if transparent_sheet and a < 16:
        return True
    if r > 238 and g > 238 and b > 238:
        return True
    if abs(r - g) < 8 and abs(g - b) < 8 and 150 <= r <= 235:
        return True
    if transparent_sheet and r < 8 and g < 8 and b < 8:
        return True
    return False


def remove_background(image: Image.Image, transparent_sheet: bool) -> Image.Image:
    image = image.convert("RGBA")
    width, height = image.size
    pixels = image.load()
    queue: deque[tuple[int, int]] = deque()
    visited: set[tuple[int, int]] = set()

    for x in range(width):
        queue.append((x, 0))
        queue.append((x, height - 1))
    for y in range(height):
        queue.append((0, y))
        queue.append((width - 1, y))

    while queue:
        x, y = queue.popleft()
        if (x, y) in visited or not (0 <= x < width and 0 <= y < height):
            continue

        visited.add((x, y))
        pixel = pixels[x, y]
        if not near_background(pixel, transparent_sheet):
            continue

        pixels[x, y] = (pixel[0], pixel[1], pixel[2], 0)
        queue.extend(((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))

    return image


def trim_alpha(image: Image.Image) -> Image.Image:
    bbox = image.getbbox()
    return image.crop(bbox) if bbox else image


def add_padding(image: Image.Image, padding: int = 12) -> Image.Image:
    padded = Image.new("RGBA", (image.width + padding * 2, image.height + padding * 2), (0, 0, 0, 0))
    padded.alpha_composite(image, (padding, padding))
    return padded


def remove_bottom_artifacts(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    width, height = image.size
    pixels = image.load()
    visited: set[tuple[int, int]] = set()
    components: list[tuple[int, int, int, int, int, list[tuple[int, int]]]] = []

    for start_y in range(height):
        for start_x in range(width):
            if (start_x, start_y) in visited or pixels[start_x, start_y][3] == 0:
                continue

            queue: deque[tuple[int, int]] = deque([(start_x, start_y)])
            points: list[tuple[int, int]] = []
            min_x = max_x = start_x
            min_y = max_y = start_y

            while queue:
                x, y = queue.popleft()
                if (x, y) in visited or not (0 <= x < width and 0 <= y < height):
                    continue
                visited.add((x, y))
                if pixels[x, y][3] == 0:
                    continue

                points.append((x, y))
                min_x = min(min_x, x)
                max_x = max(max_x, x)
                min_y = min(min_y, y)
                max_y = max(max_y, y)
                queue.extend(((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))

            components.append((len(points), min_x, min_y, max_x, max_y, points))

    if not components:
        return image

    largest = max(component[0] for component in components)
    for area, min_x, min_y, max_x, max_y, points in components:
        component_width = max_x - min_x + 1
        component_height = max_y - min_y + 1
        is_low = min_y > height * 0.66
        is_small = area < max(900, largest * 0.08)
        is_shadow_shaped = component_width > component_height * 2 or area < 260
        if is_low and is_small and is_shadow_shaped:
            for x, y in points:
                pixels[x, y] = (0, 0, 0, 0)

    return image


def crop_cell(sheet: Image.Image, columns: int, rows: int, cell: tuple[int, ...]) -> Image.Image:
    if len(cell) == 4:
        return sheet.crop(cell)

    cell_width = sheet.width / columns
    cell_height = sheet.height / rows
    column, row = cell
    margin_x = cell_width * 0.035
    margin_y = cell_height * 0.035
    left = int(column * cell_width + margin_x)
    top = int(row * cell_height + margin_y)
    right = int((column + 1) * cell_width - margin_x)
    bottom = int((row + 1) * cell_height - margin_y)
    return sheet.crop((left, top, right, bottom))


def save_sprite(sheet_config: CharacterSheet, mood: str, cell: tuple[int, ...]) -> None:
    sheet = Image.open(SOURCE_DIR / sheet_config.source).convert("RGBA")
    sprite = crop_cell(sheet, *sheet_config.grid, cell)
    sprite = remove_background(sprite, sheet_config.transparent)
    sprite = add_padding(trim_alpha(sprite))
    sprite.thumbnail((420, 420), Image.Resampling.LANCZOS)
    sprite.save(OUT / f"{sheet_config.key}_{mood}.png")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for sheet in SHEETS:
        for mood, cell in sheet.cells.items():
            save_sprite(sheet, mood, cell)


if __name__ == "__main__":
    main()
