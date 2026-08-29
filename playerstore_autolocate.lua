-- PlayerStore autolocate script
-- Usage: mcp2cli @cheatengine evaluate-lua --stdin
-- or bash background loop every 2 seconds

local offsets = {[0]=0x10,[1]=0x5C,[2]=0x60,[3]=0x64,[4]=0x68,[5]=0x6C,[6]=0x78,[7]=0x7C,[8]=0x80}
local function update()
  local cls = mono_findClass('Assembly-CSharp', 'PlayerStore')
  if not cls then return false end
  local inst = mono_class_findInstancesOfClassListOnly(cls)
  if not inst then return false end
  for i = 1, #inst do
    local obj = inst[i]
    local addr = 0
    if type(obj) == 'userdata' then addr = mono_object_findRealStartOfObject(obj) or 0
    else addr = obj end
    if addr ~= 0 then
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
          if rec then rec.Address = string.format('%X', addr + off) end
        end
        return true
      end
    end
  end
  return false
end
pcall(mono_setMonoMenuItem, true)
pcall(mono_OnProcessOpened)
return update()