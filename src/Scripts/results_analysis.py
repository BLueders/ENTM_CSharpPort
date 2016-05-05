import os
import csv
import time
import datetime

results = []

for root, dirs, files in os.walk(os.getcwd()):
    for file in files:
        if file == "results.csv":
            results.append(os.path.join(root, file))

print "Found %s results files" % len(results)

with open("analysis.csv", 'w') as output:
    fields = ["Run", "Comment", "Average time spent", "Experiments", "Solves", "Solve Percentage", "Average Generations", "Average Generations Solved", "Average Champion Fitness", "Average Solved Hidden Nodes", "Average Solved Birth Generation"]
    writer = csv.DictWriter(output, lineterminator='\n', fieldnames=fields)
    writer.writeheader()

    run = 0

    for r in results:
        run += 1
        times = []
        fitness = []
        gens = []
        solvedGens = []
        solvedHiddens = []
        solvedBirthGens = []

        rows = 0
        comment = ""

        with open(r) as input:
            reader = csv.DictReader(input)
        
            for row in reader:
                rows += 1
                
                if rows == 1 and row.has_key("Comment"):
                    comment = row["Comment"]
                
                tstruct = time.strptime(row["Time"], "%Hh:%Mm:%Ss:%fms")
                t = datetime.timedelta(hours=tstruct.tm_hour, minutes=tstruct.tm_min, seconds=tstruct.tm_sec)
                times.append(t)

                fitness.append(float(row["Champion Fitness"]))
                gens.append(float(row["Generations"]))
    
                if row["Solved"] == "True":
                    solvedGens.append(float(row["Generations"]))
                    solvedHiddens.append(int(row["Champion Hidden Nodes"]))
                    solvedBirthGens.append(int(row["Champion Birth Generation"]))


        avgTime = sum(times, datetime.timedelta(0)) / len(times)
        solvePct = len(solvedGens) / float(rows) * 100

        row = {}
        
        row["Run"] = run
        row["Comment"] = comment
        row["Average time spent"] = avgTime
        row["Experiments"] = rows
        row["Solves"] = len(solvedGens)
        row["Solve Percentage"] = "%s%%" % round(solvePct, 2)
        row["Average Generations"] = round(sum(gens) / len(gens), 2)
        row["Average Generations Solved"] = round(sum(solvedGens) / len(solvedGens), 2)
        row["Average Champion Fitness"] = round(sum(fitness) / len(fitness), 2)
        row["Average Solved Hidden Nodes"] = round(sum(solvedHiddens) / len(solvedHiddens), 2)
        row["Average Solved Birth Generation"] = round(sum(solvedBirthGens) / len(solvedBirthGens), 2)

        writer.writerow(row)

print "Success"