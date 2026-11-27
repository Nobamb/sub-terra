-- B-214: 정산기 화면용 8bit 도트 시트. 16x16 셀 8열 x 2행 = 128x32.
-- 행0: 구리/철/리튬 원석, 변환 섬광 3프레임 / 행1: 금화 회전 4프레임, 반짝임 금화
local P = app.pixelColor
local function C(hex)
  return P.rgba(tonumber(hex:sub(2, 3), 16), tonumber(hex:sub(4, 5), 16), tonumber(hex:sub(6, 7), 16), 255)
end

local spr = Sprite(128, 32)
local img = Image(128, 32)

local function put(cx, cy, x, y, c)
  if x >= 0 and y >= 0 and x < 16 and y < 16 then img:drawPixel(cx * 16 + x, cy * 16 + y, c) end
end

-- ───── 금화 (SMW 느낌의 세로 타원 + 가운데 홈) ─────
local OUT, GOLD, LIGHT, DARK, SLOT = C("#6b3c0c"), C("#ffd24d"), C("#fff3a8"), C("#d68a14"), C("#b86a0a")

local function coin(cx, cy, rx, mirror, glint)
  for y = 0, 15 do
    for x = 0, 15 do
      local sx = mirror and (15 - x) or x
      local nx, ny = (sx + 0.5 - 8) / rx, (y + 0.5 - 8) / 8
      if nx * nx + ny * ny <= 1 then
        local c
        if rx < 2.6 then
          c = (math.abs(sx + 0.5 - 8) < 0.9) and DARK or OUT
        else
          local ix, iy = (sx + 0.5 - 8) / (rx - 1), (y + 0.5 - 8) / 7
          local inner = ix * ix + iy * iy
          if inner > 1 then
            c = OUT
          else
            c = GOLD
            if inner > 0.5 then
              if (nx + ny) > 0.15 then c = DARK else c = LIGHT end
            end
            if rx >= 3.5 and math.abs(sx + 0.5 - 8) < rx * 0.16 + 0.6 and math.abs(y + 0.5 - 8) < 4.2 then
              c = ((sx + 0.5 - 8) < 0) and SLOT or DARK
              if (sx + 0.5 - 8) >= 0 and math.abs(y + 0.5 - 8) < 4.2 then c = DARK end
            end
          end
        end
        put(cx, cy, x, y, c)
      end
    end
  end
  if glint then
    local W = C("#ffffff")
    for _, p in ipairs({ { 3, 3 }, { 2, 3 }, { 4, 3 }, { 3, 2 }, { 3, 4 } }) do put(cx, cy, p[1], p[2], W) end
  end
end

coin(0, 1, 6.6, false, false)
coin(1, 1, 4.6, false, false)
coin(2, 1, 2.4, false, false)
coin(3, 1, 4.6, true, false)
coin(4, 1, 6.6, false, true)

-- ───── 원석 3종 (같은 바위 모양, 색과 박힌 광물만 다르게) ─────
local ROCK = {
  "................",
  "....KKKKKKKK....",
  "..KKMMMMMMMMKK..",
  ".KMMMMMMMMMMMMK.",
  ".KMMMMMMMMMMMMK.",
  "KMMMMMMMMMMMMMMK",
  "KMMMMMMMMMMMMMMK",
  "KMMMMMMMMMMMMMMK",
  "KMMMMMMMMMMMMMMK",
  "KMMMMMMMMMMMMMMK",
  "KMMMMMMMMMMMMMMK",
  ".KMMMMMMMMMMMMK.",
  ".KMMMMMMMMMMMMK.",
  "..KKMMMMMMMMKK..",
  "....KKKKKKKK....",
  "................",
}

local function lighten(hex, a)
  local r, g, b = tonumber(hex:sub(2, 3), 16), tonumber(hex:sub(4, 5), 16), tonumber(hex:sub(6, 7), 16)
  r, g, b = math.min(255, r + a), math.min(255, g + a), math.min(255, b + a)
  return P.rgba(r, g, b, 255)
end

local function ore(cx, rockHex, outHex, speckHex, speckHi, specks, tall)
  for y = 0, 15 do
    for x = 0, 15 do
      local ch = ROCK[y + 1]:sub(x + 1, x + 1)
      if ch == "K" then
        put(cx, 0, x, y, C(outHex))
      elseif ch == "M" then
        local c
        if x + y <= 10 then c = lighten(rockHex, 22) elseif x + y >= 21 then c = C(rockHex) else c = C(rockHex) end
        if x + y >= 21 then
          local r, g, b = tonumber(rockHex:sub(2, 3), 16), tonumber(rockHex:sub(4, 5), 16), tonumber(rockHex:sub(6, 7), 16)
          c = P.rgba(math.max(0, r - 28), math.max(0, g - 28), math.max(0, b - 28), 255)
        end
        put(cx, 0, x, y, c)
      end
    end
  end
  for _, s in ipairs(specks) do
    local h = tall and 3 or 2
    for dy = 0, h - 1 do
      for dx = 0, 1 do
        put(cx, 0, s[1] + dx, s[2] + dy, (dx == 0 and dy == 0) and C(speckHi) or C(speckHex))
      end
    end
  end
end

ore(0, "#7a6a5a", "#2a1c14", "#e0793a", "#ffb070", { { 3, 4 }, { 9, 3 }, { 6, 7 }, { 11, 8 }, { 4, 11 }, { 9, 11 } }, false)
ore(1, "#6e7480", "#1c2028", "#9fb4cc", "#eaf3ff", { { 4, 3 }, { 8, 5 }, { 2, 8 }, { 10, 7 }, { 6, 10 }, { 11, 11 } }, false)
ore(2, "#6a6478", "#241c30", "#c79bff", "#f4e8ff", { { 4, 4 }, { 9, 4 }, { 6, 8 }, { 11, 9 }, { 3, 10 } }, true)

-- ───── 변환 섬광 3프레임 ─────
local WH, YL = C("#ffffff"), C("#fff3a8")
local function star(cx, arm, diag, dotsR, col)
  for i = -arm, arm do put(cx, 0, 8 + i, 8, col); put(cx, 0, 8, 8 + i, col) end
  for i = 1, diag do
    put(cx, 0, 8 + i, 8 + i, YL); put(cx, 0, 8 - i, 8 + i, YL)
    put(cx, 0, 8 + i, 8 - i, YL); put(cx, 0, 8 - i, 8 - i, YL)
  end
  if dotsR > 0 then
    for _, p in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 }, { 1, 1 }, { -1, 1 }, { 1, -1 }, { -1, -1 } }) do
      put(cx, 0, 8 + p[1] * dotsR, 8 + p[2] * dotsR, WH)
    end
  end
end
star(3, 2, 1, 0, WH)
star(4, 5, 3, 0, WH)
star(5, 1, 0, 6, YL)

local cel = spr:newCel(spr.layers[1], spr.frames[1], img, Point(0, 0))
spr:saveAs(app.params["out"] .. ".aseprite")
spr:saveCopyAs(app.params["out"] .. ".png")
