--[[
  Smooth and Temporal Character Movement Script
  This script automatically moves the character along a predefined smooth path by updating position based on both spatial and temporal data.
  The path is loaded from 'pathFile.lua', which contains a Lua table with time and position data.
--]]

-- === Configuration ===

-- Path to the Lua file containing the recorded and processed path
local luaPathFile = "C:\\Path\\To\\Your\\pathFile.lua" -- ➜ **Update this path to your actual pathFile.lua location**

-- Movement update interval (in milliseconds)
local updateInterval = 16 -- ~60 updates per second for smooth movement

-- Movement control variables
local path = {path = {
    {time = 0.0, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.05, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.1, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.15, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.2, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.25, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.3, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.35, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.4, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.45, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.5, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.55, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.6, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.65, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.7, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.75, x = 59635.902, y = -148139.66, z = -5193.003},
    {time = 0.8, x = 59635.902, y = -148139.66, z = -5193.003},
}
local isMoving = false
local movementTimer = nil
local startTime = 0
local elapsedTime = 0

-- Pointers for character position (to be dynamically retrieved)
local posX_ptr = nil
local posY_ptr = nil
local posZ_ptr = nil

-- === Helper Functions ===

-- Function to find address by description in Cheat Engine's Address List
function findAddressByDescription(description)
    local addressList = getAddressList()
    for i = 0, addressList.Count - 1 do
        local entry = addressList[i]
        if entry.Description == description then
            return entry.Address
        end
    end
    return nil -- Address not found
end

-- Function to set the character's position
function setPosition(x, y, z)
    if posX_ptr and posY_ptr and posZ_ptr then
        writeFloat(posX_ptr, x)
        writeFloat(posY_ptr, y)
        writeFloat(posZ_ptr, z)
    else
        print("Error: One or more position pointers are not set.")
    end
end

-- Function to load the path from the Lua file
function loadPath(pathFile)
    local func, err = loadfile(pathFile)
    if not func then
        print("Failed to load Lua path file: " .. err)
        return false
    end

    local success, result = pcall(func)
    if not success then
        print("Error executing Lua path file: " .. result)
        return false
    end

    if type(path) == "table" and #path > 0 then
        return true
    else
        print("Invalid path data in Lua file.")
        return false
    end
end

-- Function to perform linear interpolation between two points
function lerp(a, b, t)
    return a + (b - a) * t
end

-- Function to find the current segment based on elapsed time
function findCurrentSegment(time)
    for i = 1, #path - 1 do
        if time >= path[i].time and time < path[i + 1].time then
            return i, path[i], path[i + 1]
        end
    end
    return nil, nil, nil -- No segment found (path completed)
end

-- === Timer Callback Function ===

function moveCharacter()
    if not isMoving then return end

    -- Calculate elapsed time since movement started
    elapsedTime = (GetTickCount() - startTime) / 1000.0 -- Convert to seconds

    -- Find the current segment
    local segmentIndex, startPoint, endPoint = findCurrentSegment(elapsedTime)
    
    if not segmentIndex then
        -- Path completed
        isMoving = false
        movementTimer.destroy()
        movementTimer = nil
        print("Movement completed.")
        return
    end

    -- Calculate the interpolation factor (t) between startPoint and endPoint
    local deltaTime = endPoint.time - startPoint.time
    if deltaTime == 0 then
        t = 0
    else
        t = (elapsedTime - startPoint.time) / deltaTime
    end

    -- Clamp t to [0, 1] to avoid overshooting due to timer inaccuracies
    if t < 0 then t = 0 end
    if t > 1 then t = 1 end

    -- Perform linear interpolation for x, y, z
    local currentX = lerp(startPoint.x, endPoint.x, t)
    local currentY = lerp(startPoint.y, endPoint.y, t)
    local currentZ = lerp(startPoint.z, endPoint.z, t)

    -- Update the character's position
    setPosition(currentX, currentY, currentZ)
end

-- === Start Movement Function ===

function startMovement()
    if isMoving then
        print("Movement is already running.")
        return
    end

    -- Retrieve X, Y, Z addresses by their descriptions
    posX_ptr = findAddressByDescription("PlayerX") -- ➜ **Replace with your actual description**
    posY_ptr = findAddressByDescription("PlayerY") -- ➜ **Replace with your actual description**
    posZ_ptr = findAddressByDescription("PlayerZ") -- ➜ **Replace with your actual description**

    -- Check if all addresses were found
    if not posX_ptr then
        print("Error: PlayerX address not found in the Address List.")
        return
    end

    if not posY_ptr then
        print("Error: PlayerY address not found in the Address List.")
        return
    end

    if not posZ_ptr then
        print("Error: PlayerZ address not found in the Address List.")
        return
    end

    -- Load the path data
    -- if not loadPath(luaPathFile) then
    --     return
    -- end

    -- Initialize movement variables
    isMoving = true
    startTime = GetTickCount()
    elapsedTime = 0

    -- Create and start the movement timer
    movementTimer = createTimer(nil, false)
    movementTimer.Interval = updateInterval
    movementTimer.OnTimer = moveCharacter
    movementTimer.Enabled = true

    print("Movement started.")
end

-- === Automatically Start Movement ===

-- Start movement as soon as the script is executed
startMovement()
