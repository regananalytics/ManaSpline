import json
import sys

json_file = "spline/spline.json"

def main(json_file = None):
    with open(json_file, 'r') as f:
        lines = f.readlines()

    records = {}
    for line in lines:
        jline = json.loads(line)
        records[jline["IGT"]] = jline

    records = list(records.values())
    records.sort(key=lambda x: x['IGT'])

    with open(json_file, "w") as f:
        for record in records:
            f.write(json.dumps(record) + "\n")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        json_file = sys.argv[1]
    main(json_file=json_file)
