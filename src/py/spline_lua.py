import json
import numpy as np
from scipy.interpolate import CubicSpline
import sys

# Configuration
INPUT_JSON_PATH = "spline/welcome_center_plain.json"
OUTPUT_LUA_PATH = "spline/welcome_center_plain.lua"
TIME_SAMPLE_INTERVAL = 0.05
REDUCTION_EPSILON = 0.5
STANDSTILL_THRESHOLD = 0.1
STANDSTIL_MIN_DURATION = 0.5


def load_json(json_path):
    data_dict = {}
    with open(json_path, "r") as f:
        for line in f:
            jline = json.loads(line)
            data_dict[jline["IGT"]] = jline["STATE"]
    data = list(data_dict.values())
    data.sort(key=lambda x: x["IGT"])
    return data

def convert_to_relative_times(path):
    start_time = path[0]["IGT"]
    for point in path:
        point["T"] = point.pop("IGT") - start_time
    return path

def detect_standstills(path):
    standstills = []
    n = len(path)
    i = 0
    while i < n:
        j = i + 1
        while j < n:
            dx = path[j]['X'] - path[i]["X"]
            dy = path[j]['Y'] - path[i]["Y"]
            dz = path[j]['Z'] - path[i]["Z"]
            distance = np.sqrt(dx**2 + dy**2 + dz**2)
            if distance > STANDSTILL_THRESHOLD:
                break
            j += 1
        # Check Duration
        if j - i >= 2:
            duration = path[j-1]['T'] - path[i]['T']
            if duration >= STANDSTIL_MIN_DURATION:
                standstills.append((i, j-1))
                i = j
                continue
        i += 1
    return standstills


def apply_cubic_spline(path):
    ts = [point['T'] for point in path]
    xs = [point['X'] for point in path]
    ys = [point['Y'] for point in path]
    zs = [point['Z'] for point in path]

    # Create cubic splines for x, y, z
    cs_x = CubicSpline(ts, xs)
    cs_y = CubicSpline(ts, ys)
    cs_z = CubicSpline(ts, zs)

    # Generate new time samples
    new_times = np.arange(ts[0], ts[-1], TIME_SAMPLE_INTERVAL)

    # Interpolate positions
    smooth_x = cs_x(new_times)
    smooth_y = cs_y(new_times)
    smooth_z = cs_z(new_times)

    # Compile the smooth path
    smooth_path = []
    for t, x, y, z in zip(new_times, smooth_x, smooth_y, smooth_z):
        smooth_path.append({
            "t": round(t, 3),
            "x": round(x, 3),
            "y": round(y, 3),
            "z": round(z, 3),
        })

    return smooth_path


def reduce_points(smooth_path, standstills, epsilon=REDUCTION_EPSILON):
    """
    Reduce the number of points in the smooth_path by applying the Ramer-Douglas-Peucker (RDP) algorithm.
    Preserve points that are at the start or end of standstill periods.
    
    Args:
        smooth_path (list): List of points with 'relative_time', 'x', 'y', 'z', and 'index'.
        standstills (list): List of tuples indicating standstill periods as (start_index, end_index).
        epsilon (float): Tolerance for RDP.
    
    Returns:
        reduced_path (list): Reduced list of points.
    """
    def rdp(points, epsilon, indices_to_keep, start_global_index):
        """
        Recursive RDP function that preserves points within standstills.
        
        Args:
            points (list): Subset of points being processed.
            epsilon (float): Tolerance for RDP.
            indices_to_keep (set): Set of global indices that must be retained.
            start_global_index (int): Global index of the first point in 'points'.
        
        Returns:
            list: Reduced subset of points.
        """
        if len(points) < 3:
            return points
        
        # Convert points to numpy arrays for vector operations
        start = np.array([points[0]['x'], points[0]['y'], points[0]['z']])
        end = np.array([points[-1]['x'], points[-1]['y'], points[-1]['z']])
        line = end - start
        line_length = np.linalg.norm(line)
        
        if line_length == 0:
            # All points are the same
            distances = [0.0 for _ in points]
        else:
            line_unit = line / line_length
            vectors = np.array([[p['x'], p['y'], p['z']] for p in points]) - start
            cross_prod = np.cross(vectors, line_unit)
            distances = np.linalg.norm(cross_prod, axis=1)
        
        max_distance = max(distances)
        index = distances.tolist().index(max_distance)
        
        if max_distance > epsilon:
            # Recursive call on the two segments
            left = rdp(points[:index+1], epsilon, indices_to_keep, start_global_index)
            right = rdp(points[index:], epsilon, indices_to_keep, start_global_index + index)
            return left[:-1] + right
        else:
            # Check if any point in the current segment needs to be kept
            for i in range(1, len(points)-1):
                global_idx = start_global_index + i
                if global_idx in indices_to_keep:
                    return points
            return [points[0], points[-1]]
    
    # Create a set of global indices to keep (start and end of standstills)
    indices_to_keep = set()
    for start, end in standstills:
        indices_to_keep.add(start)
        indices_to_keep.add(end)
    
    # Apply RDP recursively while preserving standstill points
    reduced_path = rdp(smooth_path, epsilon, indices_to_keep, 0)
    
    # Remove the 'index' field as it's no longer needed
    for point in reduced_path:
        point.pop('index', None)
    
    return reduced_path


def export_to_lua(reduced_path, lua_path):
    with open(lua_path, 'w') as file:
        file.write('path = {\n')
        for point in reduced_path:
            lua_line = f"  {{time = {point['t']}, x = {point['x']}, y = {point['y']}, z = {point['z']}}},\n"
            file.write(lua_line)
        file.write('}\n')
    print(f"Exported smooth path to {lua_path}")


def main(json_file=INPUT_JSON_PATH, lua_file=OUTPUT_LUA_PATH):
    # Step 1: Load JSON data
    path = load_json(INPUT_JSON_PATH)
    
    # Step 2: Convert to relative times
    path = convert_to_relative_times(path)
    
    # Step 3: Detect standstill periods
    standstills = detect_standstills(path)
    print(f"Detected {len(standstills)} standstill periods.")
    
    # Step 4: Apply cubic spline interpolation
    smooth_path = apply_cubic_spline(path)
    
    # Step 5: Reduce the number of points, preserving standstills
    reduced_path = reduce_points(smooth_path, standstills)
    
    # Step 6: Export to Lua table
    export_to_lua(reduced_path, OUTPUT_LUA_PATH)

if __name__ == "__main__":
    if len(sys.argv) > 1:
        json_file = sys.argv[1]
        main(json_file=json_file, lua_file=json_file.removesuffix(".json") + ".lua")
    else:
        main()

