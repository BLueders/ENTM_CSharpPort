import os
import csv
import time
import datetime
import math

results = []



for root, dirs, files in os.walk(os.getcwd()):
    for file in files:
        if file == "results.csv":
            results.append(os.path.join(root, file))

print "Found %s results files" % len(results)

with open("analysis.csv", 'w') as output:
    fields = ["Run", "Comment", "Average time spent", "Experiments", "Solves", "Solve Percentage", "Mean Generations Solved", "Generations Solved Standard Deviation", "Mean Solved Hidden Nodes", "Mean Solved Birth Generation", "Mean Champion Fitness", "Mean Champion Complexity"]
    writer = csv.DictWriter(output, lineterminator='\n', fieldnames=fields)
    writer.writeheader()

    run = 0

    for r in results:
        run += 1
        times = []
        fitness = []
        complexity = []
        solvedGens = []
        solvedHiddens = []
        solvedBirthGens = []

        count = 0
        comment = ""

        with open(r) as input:
            reader = csv.DictReader(input)
        
            for row in reader:
                count += 1
                
                if count == 1 and row.has_key("Comment"):
                    comment = row["Comment"]
                
                tstruct = time.strptime(row["Time"], "%Hh:%Mm:%Ss:%fms")
                t = datetime.timedelta(hours=tstruct.tm_hour, minutes=tstruct.tm_min, seconds=tstruct.tm_sec)
                times.append(t)

                fitness.append(float(row["Champion Fitness"]))
                complexity.append(float(row["Champion Complexity"]))
    
                if row["Solved"] == "True":
                    solvedGens.append(float(row["Generations"]))
                    solvedHiddens.append(int(row["Champion Hidden Nodes"]))
                    solvedBirthGens.append(int(row["Champion Birth Generation"]))

        avgTime = sum(times, datetime.timedelta(0)) / count

        solvedCount = len(solvedGens)
        gensSolvedMean = sum(solvedGens) / solvedCount

        # Standard deviation            
        gSum = 0
        for g in solvedGens:
            v = g - gensSolvedMean
            gSum += v * v
       
        gensSolvedStdDev = math.sqrt(gSum / solvedCount)
        
        row = {}
        
        row["Run"] = run
        row["Comment"] = comment
        row["Average time spent"] = avgTime
        row["Experiments"] = count
        row["Solves"] = len(solvedGens)
        row["Solve Percentage"] = "%s%%" % round(float(len(solvedGens)) / count * 100, 2)
        row["Mean Generations Solved"] = round(gensSolvedMean, 2)
        row["Generations Solved Standard Deviation"] = round(gensSolvedStdDev, 2)
        row["Mean Solved Hidden Nodes"] = round(float(sum(solvedHiddens)) / solvedCount, 2)
        row["Mean Solved Birth Generation"] = round(float(sum(solvedBirthGens)) / solvedCount, 2)
        row["Mean Champion Fitness"] = round(sum(fitness) / count, 2)
        row["Mean Champion Complexity"] = round(sum(complexity) / count, 2)

        writer.writerow(row)

print "Success"