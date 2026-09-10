"""Author the desktop companions as editable, self-contained SVG paths.

Run with Python 3.10+: python tools/draw_characters.py
No raster tracing, embedded images, fonts, or third-party modules are used.
The PNGs in Assets/Characters remain the visual references, not runtime assets.
"""

from __future__ import annotations

from contextlib import contextmanager
from html import escape
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "DesktopPetOrHuman" / "Assets" / "Characters"
INK = "#3e333c"
CREAM = "#fff9ee"
SKIN = "#ffdbb7"
PINK = "#ed9a9c"
MOODS = {"idle": "待機", "happy": "開心", "sleep": "睡眠"}
CHARACTERS = [
    ("miaobaibai", "喵白白"), ("miaobubu", "喵布布"),
    ("xiaotu", "小塗"), ("laowangmao", "Old Wang Cat"),
    ("fengxiong", "鋒兄"), ("fengge", "鋒哥"),
    ("xiaoying", "小英"), ("miaoniang", "喵娘"),
    ("tuge", "塗哥"), ("yamei", "牙妹"),
    ("yumei", "魚妹"), ("gugugaga", "ググガガ"),
]


def attributes(**values: object) -> str:
    return " ".join(
        f'{key.replace("_", "-")}="{escape(str(value), quote=True)}"'
        for key, value in values.items() if value is not None
    )


class Art:
    def __init__(self) -> None:
        self.parts: list[str] = []

    def element(self, tag: str, **values: object) -> None:
        self.parts.append(f"<{tag} {attributes(**values)}/>")

    def path(self, d: str, fill: str = "none", **values: object) -> None:
        self.element("path", d=d, fill=fill, **values)

    def ellipse(self, x: float, y: float, rx: float, ry: float,
                fill: str, **values: object) -> None:
        self.element("ellipse", cx=x, cy=y, rx=rx, ry=ry, fill=fill, **values)

    def circle(self, x: float, y: float, r: float, fill: str, **values: object) -> None:
        self.element("circle", cx=x, cy=y, r=r, fill=fill, **values)

    def rect(self, x: float, y: float, w: float, h: float, r: float,
             fill: str, **values: object) -> None:
        self.element("rect", x=x, y=y, width=w, height=h, rx=r, fill=fill, **values)

    @contextmanager
    def group(self, **values: object):
        self.parts.append(f"<g {attributes(**values)}>")
        yield
        self.parts.append("</g>")

    def svg(self, title: str) -> str:
        return (
            '<?xml version="1.0" encoding="utf-8"?>\n'
            '<!-- Editable vector artwork. Source: tools/draw_characters.py -->\n'
            '<svg xmlns="http://www.w3.org/2000/svg" width="320" height="320" '
            'viewBox="0 0 320 320" role="img" aria-labelledby="title">\n'
            f'  <title id="title">{escape(title)}</title>\n'
            '  <desc>Original SVG redraw of the project character reference. '
            'Transparent background; all visible artwork is vector geometry.</desc>\n'
            f'  <g stroke="{INK}" stroke-width="3.2" stroke-linecap="round" '
            'stroke-linejoin="round">\n    '
            + "\n    ".join(self.parts) + '\n  </g>\n</svg>\n'
        )


def spark(a: Art, x: int, y: int, size: float, color: str = "#f4bd57") -> None:
    with a.group(transform=f"translate({x} {y}) scale({size})", stroke="none"):
        a.path("M0 -12 Q3 -3 10 0 Q3 3 0 12 Q-3 3 -10 0 Q-3 -3 0 -12Z", color)


def mood_marks(a: Art, mood: str, high_ears: bool = False) -> None:
    with a.group(id="mood-marks"):
        if mood == "happy":
            spark(a, 49, 87, 1.1)
            spark(a, 265, 106, .8)
            spark(a, 245, 61, .5, "#e58d8c")
            a.path("M60 115 L66 119 M251 144 L258 138", stroke="#eaa266", stroke_width=4)
        elif mood == "sleep":
            # Letterforms are paths so the asset has no font dependency.
            with a.group(stroke="#799bb1", stroke_width=3.8):
                if not high_ears:
                    a.path("M229 91 H242 L230 105 H243")
                a.path("M247 64 H264 L248 82 H265")
                a.path("M265 32 H286 L267 54 H288")


def eyes(a: Art, mood: str, iris: str = "#91704e", girl: bool = False,
         cat: bool = False) -> None:
    with a.group(id="expression"):
        for x in (136, 184):
            if mood == "idle":
                a.ellipse(x, 143, 12.5, 16.5, "#fffdfa", stroke_width=2.3)
                a.ellipse(x + 1, 144.5, 8.4, 12.5, iris, stroke_width=1.8)
                a.ellipse(x + 1, 142, 4.8 if not cat else 3.8, 9.0, INK, stroke="none")
                a.circle(x - 2.5, 137, 3.7, "#fffdfa", stroke="none")
                a.circle(x + 4, 148, 1.6, "#fffdfa", stroke="none")
                a.path(f"M{x-12} 135 Q{x} 124 {x+11} 134", stroke_width=3.6)
                if girl:
                    a.path(f"M{x-12} 136 l-4 -4", stroke_width=2.6)
            elif mood == "happy":
                a.path(f"M{x-12} 144 Q{x} 128 {x+12} 144", stroke_width=4)
            else:
                a.path(f"M{x-12} 145 Q{x} 155 {x+12} 144", stroke_width=3.4)
                a.path(f"M{x-9} 149 l-2 3", stroke_width=2)
        a.ellipse(119, 162, 10, 5, PINK, stroke="none", opacity=".58")
        a.ellipse(201, 162, 10, 5, PINK, stroke="none", opacity=".58")
        if cat:
            a.path("M154 158 Q160 154 166 158 L160 164Z", "#dd8d93", stroke_width=1.8)
            if mood == "happy":
                a.path("M149 169 Q160 174 171 169 Q169 185 160 185 Q151 185 149 169Z", "#a95164", stroke_width=2)
                a.path("M154 180 Q161 175 167 181", stroke="#f4a6af", stroke_width=4)
            else:
                a.path("M160 164 v4 Q154 177 149 169 M160 168 Q167 177 172 169", stroke_width=2.4)
            a.path("M111 160 l-17 -4 M111 167 l-15 2 M209 160 l17 -4 M209 167 l15 2", stroke="#9d8582", stroke_width=2)
        else:
            a.path("M158 155 q-2 5 3 5", stroke="#d39a7d", stroke_width=1.8)
            if mood == "happy":
                a.path("M146 170 Q160 175 174 170 Q172 189 160 189 Q148 189 146 170Z", "#864953", stroke_width=2.4)
                a.path("M151 182 Q160 177 169 182 Q160 192 151 182Z", "#ee9aa5", stroke="none")
                a.path("M149 171 Q160 175 171 171", stroke=CREAM, stroke_width=3)
            elif mood == "sleep":
                a.path("M155 173 q5 4 10 0", stroke_width=2.2)
            else:
                a.path("M150 173 Q160 180 171 171", stroke_width=2.4)


def glasses(a: Art, rounded: bool = False) -> None:
    with a.group(id="glasses", stroke="#45434b", stroke_width=4):
        a.rect(115, 125, 40, 34, 16 if rounded else 10, "none")
        a.rect(165, 125, 40, 34, 16 if rounded else 10, "none")
        a.path("M155 138 Q160 134 165 138 M114 131 L104 129 M206 131 L217 129")
        a.path("M120 131 h10 M170 131 h10", stroke="#fff9ef", stroke_width=2, opacity=".7")


def bow(a: Art, x: int, y: int, color: str, scale: float = 1) -> None:
    with a.group(transform=f"translate({x} {y}) scale({scale})", stroke_width=2.4):
        a.path("M0 0 Q-10 -12 -20 -9 Q-23 0 -19 10 Q-9 9 0 0Z", color)
        a.path("M0 0 Q10 -12 20 -9 Q23 0 19 10 Q9 9 0 0Z", color)
        a.path("M-4 2 L-10 21 L0 15 L9 22 L5 2", color)
        a.circle(0, 0, 5, color)


def hand(a: Art, x: int, y: int, raised: bool = False) -> None:
    a.ellipse(x, y, 8.5, 10, SKIN, stroke_width=2.6)
    if raised:
        a.path(f"M{x-4} {y-3} l2 4 M{x+1} {y-5} l2 4", stroke="#cd947e", stroke_width=1.6)


def shoe(a: Art, x: int, y: int, color: str = "#41424b", sneaker: bool = False) -> None:
    a.path(f"M{x-12} {y-9} Q{x-21} {y-2} {x-17} {y+4} Q{x} {y+9} {x+13} {y+4} L{x+12} {y-10}Z", color, stroke_width=2.8)
    if sneaker:
        a.path(f"M{x-16} {y+2} Q{x} {y+6} {x+11} {y+2}", stroke=CREAM, stroke_width=3.4)
        a.path(f"M{x-8} {y-6} h10", stroke=CREAM, stroke_width=2)
    else:
        a.path(f"M{x-10} {y-3} h9", stroke="#77727b", stroke_width=2)


HUMANS = {
    "xiaotu": dict(hair="#353039", shine="#60514e", iris="#94704d", top="#8d9194", pants="#557b9e", kind="tee"),
    "fengxiong": dict(hair="#34323a", shine="#686064", iris="#806547", top="#cfb68e", pants="#4e6d8b", kind="jacket"),
    "fengge": dict(hair="#303039", shine="#615962", iris="#8d7052", top="#41424b", pants="#41424b", kind="suit"),
    "xiaoying": dict(hair="#32333c", shine="#636370", iris="#907451", top="#3e5266", pants="#354457", kind="suit"),
    "miaoniang": dict(hair="#d8d9dc", shine="#fbf6eb", iris="#88a653", top=CREAM, pants=CREAM, kind="hood"),
    "tuge": dict(hair="#39332e", shine="#73604c", iris="#a17745", top="#f28e3d", pants="#738160", kind="tee"),
    "yamei": dict(hair="#c94f4e", shine="#ee8271", iris="#c98640", top="#414049", pants="#414049", kind="dress"),
    "yumei": dict(hair="#393741", shine="#655b61", iris="#648ca8", top="#a9cde5", pants="#435976", kind="sailor"),
}


def human_head_transform(mood: str) -> str | None:
    if mood == "sleep":
        return "translate(-5 28) rotate(12 160 140)"
    if mood == "happy":
        return "rotate(-3 160 140)"
    return None


def human_back_hair(a: Art, key: str, mood: str, p: dict[str, str]) -> None:
    hair = p["hair"]
    with a.group(id="back-hair", transform=human_head_transform(mood)):
        if key == "yamei":
            for mirror in (False, True):
                with a.group(transform="translate(320 0) scale(-1 1)" if mirror else None):
                    a.path("M101 90 Q66 90 70 143 Q72 177 54 207 Q43 231 66 245 Q79 252 78 264 Q108 252 96 233 Q85 220 104 189 Q116 156 115 111Z", hair)
                    a.path("M86 113 Q77 142 86 167 Q90 190 73 215 Q62 232 76 241", stroke=p["shine"], stroke_width=5)
        elif key == "yumei":
            a.path("M92 122 Q80 166 86 196 Q75 223 90 244 Q90 264 110 265 L209 263 Q236 252 228 230 Q242 214 231 183 L229 110Z", hair)
            a.path("M104 161 Q92 201 105 224 Q92 242 105 251 M215 159 Q226 193 214 224 Q226 242 215 252", stroke=p["shine"], stroke_width=3)
        elif key == "miaoniang":
            a.path("M103 108 Q83 145 92 179 L87 192 Q116 202 128 187 L196 185 Q215 199 234 188 L226 172 Q235 139 216 107Z", hair)


def human_body(a: Art, key: str, mood: str, p: dict[str, str]) -> None:
    kind, top, pants = p["kind"], p["top"], p["pants"]
    girl = kind in ("dress", "sailor", "hood")
    with a.group(id="body"):
        if mood == "sleep":
            a.path("M100 273 Q83 284 105 291 Q157 303 218 287 Q235 278 215 267Z", "#c5dbe2" if key != "yamei" else "#e9c5c3", stroke="#80969f", stroke_width=2.4)
            a.path("M130 233 Q102 235 105 260 Q124 281 164 275 L169 252Z", pants)
            a.path("M185 234 Q217 238 212 260 Q195 278 156 269 L150 250Z", pants)
            shoe(a, 137, 272, "#f4ece2" if kind in ("tee", "jacket", "hood") else "#41424b", True)
            shoe(a, 187, 272, "#f4ece2" if kind in ("tee", "jacket", "hood") else "#41424b", True)
        elif girl:
            for x in (141, 178):
                with a.group(transform=f"rotate({-15 if x == 141 else 22} {x} 247)" if mood == "happy" else None):
                    a.path(f"M{x-9} 238 v29 q9 8 18 0 v-29", SKIN, stroke_width=2.6)
                    if kind == "sailor":
                        a.rect(x-9, 258, 18, 12, 2, CREAM, stroke_width=2)
                    shoe(a, x, 278, CREAM if kind == "hood" else "#41424b")
        else:
            for x in (140, 180):
                with a.group(transform=f"rotate({18 if x == 140 else -26} {x} 231)" if mood == "happy" else None):
                    a.path(f"M{x-15} 222 L{x-14} 271 Q{x} 276 {x+13} 270 L{x+16} 222Z", pants)
                    a.path(f"M{x-9} 242 l1 15", stroke="#fff6e9", opacity=".18", stroke_width=3)
                    if key == "tuge":
                        a.path(f"M{x-10} 244 l9 -5 l9 10 l-6 8 l-13 -1Z", "#535f4b", stroke="none")
                    shoe(a, x, 279, "#eee9df" if kind in ("tee", "jacket") else "#41424b", kind in ("tee", "jacket"))

        # Arms are drawn behind the torso. The gesture changes the silhouette.
        for mirror in (False, True):
            with a.group(transform="translate(320 0) scale(-1 1)" if mirror else None):
                if mood == "happy":
                    a.path("M134 192 Q117 190 102 169 L84 152 L74 166 Q91 199 121 216Z", top)
                    a.path("M84 155 L97 171", stroke="#fff8ee", opacity=".4", stroke_width=3)
                    hand(a, 76, 151, True)
                elif mood == "sleep":
                    a.path("M133 195 Q113 197 109 221 L120 232 L142 213Z", top)
                else:
                    a.path("M132 191 Q116 192 111 209 L104 236 L119 242 L141 206Z", top)
                    hand(a, 111, 242)
                    if kind in ("suit", "sailor"):
                        a.path("M106 230 l14 5", stroke=CREAM, stroke_width=3)

        a.path("M144 181 L142 195 Q160 206 178 195 L176 181Z", SKIN, stroke_width=2.5)
        a.path("M133 189 L146 195 Q160 203 174 195 L187 189 Q197 207 198 235 Q162 247 121 235 Q122 208 133 189Z", top)
        a.path("M126 224 Q160 235 196 224 L198 235 Q160 247 121 235Z", INK, stroke="none", opacity=".09")

        if kind == "suit":
            a.path("M142 190 L160 232 L178 190 Q160 199 142 190Z", CREAM, stroke_width=2)
            a.path("M155 201 L165 201 L164 211 L168 222 L160 232 L152 222 L156 211Z", "#e89846" if key == "fengge" else "#728fa5", stroke_width=1.8)
            if key == "fengge":
                a.path("M155 216 l8 -6 M155 224 l11 -8", stroke="#fff2c6", stroke_width=2)
            a.path("M139 190 L133 205 L144 209 L137 216 L160 239 L184 214 L175 208 L185 201 L180 190", stroke_width=2.2)
            a.circle(164, 234, 2.3, "#b5a89c", stroke_width=1.3)
            a.path("M126 228 l12 2 M180 230 l12 -2", stroke_width=2)
        elif kind == "jacket":
            a.path("M145 193 L148 236 L173 236 L176 192 Q160 201 145 193Z", "#3e4852", stroke_width=2.2)
            a.path("M139 191 L135 206 L144 212 M183 191 L187 206 L177 213", stroke_width=2)
            a.rect(127, 210, 14, 12, 2, "#68727a", stroke_width=1.8)
            a.rect(181, 210, 12, 12, 2, "#68727a", stroke_width=1.8)
            a.circle(134, 214, 1.6, CREAM, stroke="none")
        elif kind in ("dress", "sailor"):
            a.path("M128 228 L115 250 Q160 269 207 250 L193 228Z", pants)
            a.path("M138 236 L134 253 M154 237 v20 M170 237 l3 19 M185 235 l8 18", stroke="#777887" if kind == "dress" else "#75869a", stroke_width=2)
            if kind == "sailor":
                a.path("M139 189 L160 206 L181 189 L188 198 L160 216 L133 199Z", "#435976", stroke_width=2)
                a.path("M139 195 L160 210 L181 195", stroke=CREAM, stroke_width=1.8)
                bow(a, 160, 211, "#435976", .65)
            else:
                a.path("M142 190 l18 17 l18 -17", CREAM, stroke_width=1.8)
                bow(a, 160, 203, "#d26361", .72)
                a.rect(127, 229, 67, 9, 3, "#373139", stroke_width=2)
                a.rect(155, 228, 13, 12, 2, "#deb565", stroke_width=1.6)
                a.rect(159, 231, 5, 6, 1, "#49414a", stroke="none")
                a.circle(157, 220, 2.5, "#e4c57c", stroke="none")
        elif kind == "hood":
            a.path("M139 193 Q130 208 144 215 M180 193 Q190 208 177 215 M160 202 v35", stroke="#c7bdb2", stroke_width=2)
            a.path("M137 215 v11 M181 215 v11", stroke="#9f9290", stroke_width=2)
            a.path("M128 222 Q136 212 143 224 L142 239 L122 235Z", "#dfaa64", stroke="none")
            a.path("M191 207 Q178 207 179 219 Q186 225 196 219Z", "#575159", stroke="none")
        else:
            a.path("M145 194 Q160 206 176 194", stroke="#49464b", stroke_width=2)
            a.path("M135 224 q25 5 49 0", stroke="#fff8ed", opacity=".3", stroke_width=2)
            if key == "tuge":
                a.rect(142, 210, 36, 12, 2, "#fff4d6", stroke_width=1.5)
                # A small lightning emblem remains readable at desktop-pet size.
                a.path("M162 211 l-7 6 h5 l-2 5 l9 -7 h-6Z", "#b06b38", stroke="none")
        if mood == "sleep":
            a.path("M112 226 Q128 246 151 246 M207 225 Q193 244 173 245", stroke=top, stroke_width=12)
            hand(a, 151, 244)
            hand(a, 175, 244)
            if kind in ("tee", "jacket", "suit"):
                a.path("M147 238 H174 L171 261 Q160 267 151 261Z", "#faf0df", stroke_width=2.4)
                a.path("M174 242 Q187 239 183 251 Q181 255 173 253", stroke="#806d66", stroke_width=3)
                a.ellipse(160.5, 238, 13, 4, "#765747", stroke_width=2)


def human_head(a: Art, key: str, mood: str, p: dict[str, str]) -> None:
    girl = p["kind"] in ("dress", "sailor", "hood")
    hair, shine = p["hair"], p["shine"]
    with a.group(id="head", transform=human_head_transform(mood)):
        if key == "miaoniang":
            a.path("M94 99 Q77 70 82 42 Q112 42 129 73 M190 74 Q214 43 238 42 Q241 74 226 106", CREAM)
            a.path("M90 55 Q108 60 116 78 L95 90Z", "#e8a4a0", stroke="none")
            a.path("M210 75 Q223 53 231 51 L227 88Z", "#e8a4a0", stroke="none")
            a.path("M97 111 Q86 71 116 59 L131 71Z", "#58505a", stroke="none")
            a.path("M190 74 Q206 59 225 64 L221 97Z", "#e3a253", stroke="none")
            a.path("M89 128 Q88 69 158 61 Q225 62 235 121 L232 159 Q226 193 195 193 H124 Q88 187 89 128Z", CREAM)
            a.path("M102 129 Q100 79 157 77 Q216 78 222 131 L217 159 Q207 184 160 190 Q111 183 101 158Z", hair, stroke="#c0b7b4", stroke_width=2.6)
        if key == "yamei":
            a.path("M96 91 L95 40 Q122 43 137 76 M184 75 Q205 44 230 39 L223 97", hair)
            a.path("M104 53 L106 82 L126 76Z M220 53 L214 83 L195 76Z", "#f8c6aa", stroke_width=2)
        a.ellipse(95, 145, 12, 16, SKIN, stroke_width=2.8)
        a.ellipse(225, 145, 12, 16, SKIN, stroke_width=2.8)
        a.path("M92 140 q8 -3 8 8 M227 139 q-8 -2 -8 8", stroke="#d8a07f", stroke_width=2)
        a.path("M96 127 Q96 73 159 73 Q224 75 224 130 L222 155 Q217 191 160 197 Q105 191 98 158Z", SKIN)
        a.path("M101 158 Q114 184 160 187 Q204 184 220 157 Q212 192 160 197 Q111 190 101 158Z", "#efbc98", stroke="none", opacity=".7")

        if key == "miaoniang":
            a.path("M103 137 Q96 88 142 83 Q193 69 218 113 L219 145 L207 123 L199 109 L197 132 Q179 123 171 101 Q164 117 147 127 L149 104 Q130 133 118 136 L122 111Z", hair, stroke_width=2.5)
            a.path("M151 86 Q169 80 180 85 L188 115 Q172 108 169 96 L164 114Z", "#df9a57", stroke="none")
            a.path("M121 99 Q126 89 139 88 M190 93 l11 9", stroke=shine, stroke_width=4)
        elif key == "yamei":
            a.path("M97 146 Q82 106 108 81 Q128 58 162 65 Q212 61 226 111 L222 149 L206 129 L198 104 Q188 123 171 137 L174 107 Q159 137 140 141 L145 117 Q127 137 113 139 L119 119 L107 154Z", hair)
            a.path("M111 106 Q128 78 155 77 M174 78 Q198 79 212 105", stroke=shine, stroke_width=5)
            a.path("M156 85 Q158 103 148 119 M185 88 q4 11 -4 24", stroke=shine, stroke_width=2.4)
            bow(a, 96, 95, "#423740", .65)
            bow(a, 226, 93, "#423740", .65)
        elif key == "yumei":
            a.path("M95 158 Q82 125 96 93 Q114 62 157 62 Q209 58 225 103 Q235 127 222 158 L210 131 L202 104 Q187 132 164 137 L171 118 Q145 143 122 138 L137 122 Q113 131 103 130Z", hair)
            a.path("M109 104 Q123 77 155 76 M175 75 Q201 77 215 102", stroke=shine, stroke_width=5)
            a.path("M179 85 Q165 112 142 122", stroke=shine, stroke_width=2.6)
            a.path("M207 111 l9 11 M205 116 l7 9", stroke="#d4b98c", stroke_width=2.5)
        elif key in ("fengxiong", "fengge", "xiaoying"):
            a.path("M94 150 L90 124 Q77 103 94 82 L88 77 L106 74 Q118 54 151 59 Q164 46 190 62 Q223 63 230 101 Q238 121 225 148 L216 126 Q218 103 202 91 Q175 83 156 99 Q134 121 111 126 L116 109 Q105 119 104 142Z", hair)
            a.path("M103 96 Q122 68 155 73 Q177 59 202 76", stroke=shine, stroke_width=5)
            a.path("M115 102 Q145 78 172 81 M203 87 Q222 100 223 121", stroke=shine, stroke_width=2.6)
            if key == "xiaoying":
                a.path("M152 91 Q139 123 115 132 L124 108Z", hair, stroke_width=2)
            a.path("M123 119 Q137 113 149 120 M173 118 Q185 112 198 119", stroke=hair, stroke_width=3)
        else:
            a.path("M95 153 L89 125 L85 132 L88 105 L80 107 L94 87 L90 82 L114 73 L111 65 L137 64 L141 55 L159 61 L175 54 L185 64 L203 61 L207 71 L226 80 L223 88 L235 107 L229 111 L229 137 L222 131 L218 151 L209 128 L205 103 L187 119 L189 99 L166 120 L169 100 L147 122 L150 104 L130 123 L132 107 L112 130 L111 112 L103 139Z", hair)
            a.path("M104 96 L126 82 L125 78 L150 79 L155 72 L174 77 L185 72 L199 83", stroke=shine, stroke_width=4.6)
            a.path("M196 89 l11 9", stroke=shine, stroke_width=2.6)

        eyes(a, mood, p["iris"], girl)
        if key in ("fengxiong", "fengge", "xiaoying"):
            glasses(a, key == "xiaoying")
        if key == "fengxiong":
            a.path("M149 166 Q155 162 160 166 Q166 161 171 166", stroke="#72604f", stroke_width=2)
            a.path("M154 184 q7 3 13 -1", stroke="#9e8068", stroke_width=1.7)


def human(a: Art, key: str, mood: str) -> None:
    p = HUMANS[key]
    human_back_hair(a, key, mood, p)
    if key in ("miaoniang", "yamei"):
        with a.group(id="tail"):
            a.path("M199 233 Q233 250 246 225 Q256 207 247 203 Q237 198 234 217 Q231 231 211 219", "#e4a45f" if key == "miaoniang" else p["hair"])
            if key == "miaoniang":
                a.path("M240 209 Q246 211 248 215 M233 226 l8 7", stroke="#534b52", stroke_width=7)
    human_body(a, key, mood, p)
    human_head(a, key, mood, p)
    mood_marks(a, mood, high_ears=key in ("miaoniang", "yamei"))


def cat_head(a: Art, key: str, mood: str) -> None:
    hood = key == "miaobaibai"
    lucky = key == "laowangmao"
    fur = "#fbf6eb"
    patch = "#be9b87" if lucky else "#e7a258"
    with a.group(id="head", transform="translate(-18 63) rotate(-12 160 145) scale(.93)" if mood == "sleep" else "rotate(-4 160 140)" if mood == "happy" else None):
        a.path("M98 102 Q77 75 81 42 Q111 43 131 77 M189 77 Q212 43 239 42 Q242 77 221 104", fur if hood else patch)
        a.path("M94 87 Q89 66 90 56 Q111 63 119 79Z M204 79 Q224 57 231 56 Q231 77 223 91Z", "#eaa2a2", stroke_width=2.3)
        if hood:
            a.path("M88 138 Q86 75 158 67 Q228 68 233 133 L230 170 Q224 206 160 209 Q95 204 88 171Z", "#eee6d9")
            a.path("M101 140 Q101 87 159 82 Q215 84 220 140 L216 164 Q209 191 160 195 Q111 190 102 164Z", fur, stroke="#ad9e98", stroke_width=2.8)
        else:
            a.path("M95 130 Q93 79 158 77 Q224 76 225 130 L233 148 L226 153 L232 163 Q214 195 160 201 Q105 196 88 163 L95 153 L87 148Z", fur)
        if key == "miaobubu":
            a.path("M98 128 Q94 90 132 81 L151 85 Q141 104 126 115 L126 102 Q115 123 98 128Z", patch, stroke="none")
            a.path("M145 79 L154 97 L161 86 L169 105 L177 80Z", "#51454b", stroke="none")
            a.path("M177 83 Q211 84 219 115 Q200 108 191 98Z", patch, stroke="none")
        elif lucky:
            a.path("M168 79 Q201 76 218 101 L220 123 Q197 142 185 114 Q174 105 168 79Z", patch, stroke="none")
            a.ellipse(126, 109, 8, 4, patch, stroke="none", transform="rotate(-15 126 109)")
        else:
            a.path("M137 86 L147 107 L157 91 L163 112 L177 85Z", "#7b7374", stroke="none")
        eyes(a, mood, "#967555" if lucky else "#8c9c63", cat=True)
        if hood:
            a.path("M96 141 Q97 91 144 82 M177 83 Q215 93 223 132", stroke="#fffdf6", stroke_width=5)


def paw(a: Art, x: int, y: int, pads: bool = False) -> None:
    a.ellipse(x, y, 15, 16, CREAM, stroke_width=2.8)
    if pads:
        a.ellipse(x, y+4, 6.5, 5.5, "#e7a0a3", stroke="none")
        for dx, dy in ((-8, -4), (0, -8), (8, -4)):
            a.circle(x+dx, y+dy, 2.7, "#e7a0a3", stroke="none")
    else:
        a.path(f"M{x-5} {y+6} v5 M{x+3} {y+6} v5", stroke="#c6aea2", stroke_width=1.8)


def ingot(a: Art, x: int, y: int, scale: float = 1) -> None:
    with a.group(transform=f"translate({x} {y}) scale({scale})", stroke="#9b6935", stroke_width=2.5):
        a.path("M-30 -8 Q0 8 30 -8 L21 11 Q0 23 -21 11Z", "#efb94e")
        a.ellipse(0, -4, 17, 12, "#f9d974")
        a.path("M-10 -8 Q0 -16 9 -9", stroke="#fff3bd", stroke_width=4)


def cat(a: Art, key: str, mood: str) -> None:
    hood, lucky = key == "miaobaibai", key == "laowangmao"
    fur = CREAM
    patch = "#bf9a83" if lucky else "#e7a258"
    with a.group(id="body"):
        if mood == "sleep":
            a.path("M153 199 Q171 168 210 181 Q257 187 264 231 Q269 271 226 284 L133 283 Q95 271 115 236Z", "#eee5d8" if hood else fur)
            if not hood:
                a.path("M202 181 Q241 183 253 208 Q232 224 219 210 Q204 200 202 181Z", patch, stroke="none")
            a.path("M242 227 Q263 264 222 272 L172 267 Q158 269 161 281 Q194 296 237 285 Q281 270 266 230 Q260 215 247 218Z", fur if hood else patch)
            if key == "miaobubu":
                a.path("M254 238 l16 -2 M241 271 l8 13 M209 272 l-2 15", stroke="#51464c", stroke_width=12)
            if hood:
                a.path("M192 193 Q243 193 246 218", stroke=CREAM, stroke_width=4)
        else:
            a.path("M199 229 Q239 239 243 208 Q243 191 255 196 Q272 229 245 248 Q224 262 200 249", fur if hood else patch)
            if key == "miaobubu":
                a.path("M249 214 l13 4 M231 243 l4 13", stroke="#50464b", stroke_width=9)
            a.path("M129 186 Q113 202 112 235 Q105 263 130 272 Q159 282 192 271 Q214 263 207 236 Q207 201 190 186Z", "#eee6d9" if hood else fur)
            if key == "miaobubu":
                a.path("M194 204 Q213 218 205 242 Q185 241 185 225Z", patch, stroke="none")
            if lucky:
                a.path("M190 224 Q215 224 203 256 L185 254Z", patch, stroke="none")
            paw(a, 133, 269)
            paw(a, 186, 269)
            if hood:
                a.path("M160 199 v62 M132 242 q27 12 55 0 M137 211 v14 M182 211 v14", stroke="#b3a6a0", stroke_width=2.2)
                a.path("M120 234 Q129 228 139 232 L140 249 Q129 254 119 246Z M201 233 Q192 229 182 232 L182 249 Q193 253 202 245Z", "#f7f0e4", stroke="#b3a6a0", stroke_width=2)

    cat_head(a, key, mood)
    with a.group(id="accessories"):
        if lucky:
            if mood == "sleep":
                with a.group(transform="translate(-20 54) rotate(-12 160 198)"):
                    a.path("M120 188 Q157 204 201 187", stroke="#ba6058", stroke_width=12)
                    a.path("M123 187 l7 7 M143 194 l9 5 M171 196 l9 -2 M190 190 l8 -3", stroke="#fff3dd", stroke_width=6)
                    bow(a, 169, 202, "#c8625c", .7)
            else:
                a.path("M120 190 Q160 207 201 190", stroke="#bd6056", stroke_width=12)
                a.path("M127 191 l6 8 M148 198 l5 6 M173 198 l5 5 M194 192 l4 6", stroke=CREAM, stroke_width=6)
                bow(a, 160, 207, "#cf675f", .82)
                ingot(a, 163, 241, .95)
        if mood == "sleep":
            paw(a, 103, 273)
            paw(a, 134, 277)
        elif mood == "happy":
            a.path("M116 211 Q95 199 82 173 M201 209 Q224 192 238 172", stroke=INK, stroke_width=24)
            a.path("M116 211 Q95 199 82 173 M201 209 Q224 192 238 172", stroke=fur, stroke_width=18)
            paw(a, 80, 164, True)
            paw(a, 239, 163, True)
        elif lucky:
            a.path("M120 221 Q96 219 103 185", stroke=INK, stroke_width=23)
            a.path("M120 221 Q96 219 103 185", stroke=fur, stroke_width=17)
            paw(a, 103, 181, True)
            paw(a, 198, 244)
        elif not hood:
            paw(a, 127, 236)
            paw(a, 194, 236)
    mood_marks(a, mood)
    if lucky and mood == "happy":
        ingot(a, 66, 80, .45)
        ingot(a, 264, 91, .42)


def penguin(a: Art, mood: str) -> None:
    coat = "#4c4657"
    with a.group(id="penguin-body"):
        if mood == "sleep":
            a.path("M107 226 Q153 194 221 201 Q253 205 264 232 L283 246 L256 249 Q249 273 212 280 L105 279Z", coat)
            a.path("M187 248 Q211 236 224 257 L232 276 L166 281Z", "#fbf5e7", stroke_width=2.6)
            a.path("M255 252 l12 8 l-19 7", "#eebc55", stroke_width=2.5)
        else:
            for mirror in (False, True):
                with a.group(transform="translate(320 0) scale(-1 1)" if mirror else None):
                    a.path("M130 194 Q105 186 87 172 Q66 166 75 189 Q84 215 114 224Z" if mood == "idle" else "M128 191 Q109 178 90 153 Q75 144 73 160 Q79 199 115 217Z", coat)
            a.path("M130 186 Q104 205 110 247 Q112 274 160 279 Q207 274 213 247 Q217 208 190 186Z", coat)
            a.path("M160 201 Q127 202 126 240 Q124 265 160 266 Q195 263 194 241 Q193 207 160 201Z", "#fff8eb", stroke_width=2.8)
            a.path("M129 268 Q112 275 115 285 Q132 293 151 282 L151 273Z", "#e9b548", stroke_width=2.6)
            a.path("M173 270 Q195 264 204 277 Q209 286 188 289 L169 281Z", "#e9b548", stroke_width=2.6)
            a.path("M125 280 l4 5 M192 279 l-1 5", stroke="#b27e3d", stroke_width=1.8)
    with a.group(id="hooded-head", transform="translate(-18 78) rotate(-13 160 137) scale(.94)" if mood == "sleep" else "rotate(-4 160 135)" if mood == "happy" else None):
        a.path("M82 135 Q80 67 155 58 Q228 55 237 130 L239 173 Q229 204 159 207 Q88 201 79 172Z", coat)
        a.path("M97 113 Q113 75 158 74 Q201 72 222 109", stroke="#70667a", stroke_width=5)
        a.path("M98 154 Q95 106 159 105 Q221 105 221 152 Q218 190 160 196 Q104 190 98 154Z", SKIN)
        a.path("M100 156 L96 139 Q105 105 158 103 Q206 102 221 137 L222 168 L211 155 L209 136 L195 132 L196 141 L180 138 L179 130 L162 131 L161 142 L146 141 L145 132 L129 136 L128 146 L114 146 L111 171Z", "#53404a", stroke_width=2.5)
        a.path("M115 127 Q138 114 159 117 M180 118 q17 1 27 9", stroke="#8c6570", stroke_width=3)
        eyes(a, mood, "#817784", True)
        a.ellipse(133, 91, 7, 9, CREAM, stroke_width=2.2)
        a.ellipse(186, 91, 7, 9, CREAM, stroke_width=2.2)
        a.circle(134, 92, 3, INK, stroke="none")
        a.circle(185, 92, 3, INK, stroke="none")
        a.path("M141 111 L159 97 L179 111 Q161 118 141 111Z", "#f1c65c", stroke_width=2.5)
        a.path("M148 109 l11 -7 l9 6", stroke="#ffe6a3", stroke_width=2)
        a.path("M101 128 l8 5 M99 135 l8 5", stroke="#edb4b4", stroke_width=2.5)
    if mood == "sleep":
        paw(a, 98, 277)
        paw(a, 126, 278)
    else:
        bow(a, 160, 207, "#b4a4b8", .65)
    mood_marks(a, mood)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for key, name in CHARACTERS:
        for mood, label in MOODS.items():
            art = Art()
            if key in HUMANS:
                human(art, key, mood)
            elif key == "gugugaga":
                penguin(art, mood)
            else:
                cat(art, key, mood)
            (OUT / f"{key}_{mood}.svg").write_text(art.svg(f"{name} · {label}"), encoding="utf-8")
    print(f"Wrote {len(CHARACTERS) * len(MOODS)} SVG characters to {OUT}")


if __name__ == "__main__":
    main()
