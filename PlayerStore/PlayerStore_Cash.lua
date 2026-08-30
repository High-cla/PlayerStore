-- 自动定位 PlayerStore 实例并修正所有条目地址（游戏重启后地址变化仍可用）
-- 用法：先启动游戏 → CE 附加进程 → 打开本表（脚本自动运行）
-- 说明：本文件是 PlayerStore_Cash.ct 内 LuaScript 的开发副本，用于 LSP 编辑；
--       修改后需同步回 .ct 文件的 <LuaScript> 标签内。

local offsets = {
  [0] = 0x10, -- 现金
  [1] = 0x5C, -- 零售加价
  [2] = 0x60, -- 违禁加价-低
  [3] = 0x64, -- 违禁加价-中
  [4] = 0x68, -- 违禁加价-高
  [5] = 0x6C, -- 违禁加价-极品
  [6] = 0x78, -- 商店吸引力
  [7] = 0x7C, -- 投影仪加成
  [8] = 0x80, -- 议价加成
}

-- 激活 CE 的 Mono 收集器（关键：不加这步，mono 查询全返回空）
local function activateMono()
  pcall(mono_setMonoMenuItem, true)
  pcall(mono_OnProcessOpened)
end

local function update()
  local cls = mono_findClass("Assembly-CSharp", "PlayerStore")
  if not cls then return false end
  local inst = mono_class_findInstancesOfClassListOnly(cls)
  if not inst or #inst == 0 then return false end
  for i = 1, #inst do
    local obj = inst[i]
    local addr = 0
    if type(obj) == "userdata" then
      addr = mono_object_findRealStartOfObject(obj) or 0
    else
      addr = obj
    end
    if addr ~= 0 then
      -- 严格校验，防止选到垃圾实例：
      -- 真实例特征（实测）：cash 合理(>0)、rent/loan 为正常金额(<10万)、runNumber 小(<1000)
      local ok1, cash = pcall(readInteger, addr + 0x10)
      local ok2, rent = pcall(readInteger, addr + 0x48)
      local ok3, loan = pcall(readInteger, addr + 0x50)
      local ok4, runN = pcall(readInteger, addr + 0x30)
      if ok1 and ok2 and ok3 and ok4
         and cash and cash > 0 and cash < 10000000
         and rent and rent >= 0 and rent < 100000
         and loan and loan >= 0 and loan < 100000
         and runN and runN >= 0 and runN < 1000 then
        local al = getAddressList()
        for id, off in pairs(offsets) do
          local rec = al.getMemoryRecordByID(id)
          if rec then
            rec.Address = string.format("%X", addr + off)
          end
        end
        return true
      end
    end
  end
  return false
end

activateMono()

-- 每 1 秒重试，成功即停（上限 60 秒，防卡死）
local tries = 0
local t = createTimer(nil, false)
t.Interval = 1000
t.OnTimer = function()
  tries = tries + 1
  if update() or tries >= 60 then
    t.destroy()
  end
end
t.Enabled = true