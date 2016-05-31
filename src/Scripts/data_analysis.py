import os
import sys
import csv
import re

data = []

pattern = "data.*?\.csv"

wdir = os.getcwd()
for root, dirs, files in os.walk(wdir):
    for file in files:
        if re.match(pattern, file):
            data.append(os.path.join(root, file))

print "Found %s data files" % len(data)

with open("data_output.csv", 'w') as output:
    fields = ["Run", "Maximum Achieved Objective Fitness"]

    writer = csv.DictWriter(output, lineterminator='\n', fieldnames=fields)
    writer.writeheader()
    
    run = 0

    for d in data:

        run += 1
        max = 0

        with open(d) as input:
            reader = csv.DictReader(input)
            
            for row in reader:
                score = row[" Obj 0: Objective Max Score"]
                if score > max:
                    max = score

        row = {}
        row["Run"] = run
        row["Maximum Achieved Objective Fitness"] = max
        writer.writerow(row)