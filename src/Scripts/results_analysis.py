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
    fields = ["Run", "Comment", "Start time", "Total time spent", "Average time spent", "Experiments", "Solves", "Solve Percentage", "Mean Generations Solved", "Generations Solved Standard Deviation", "Mean Solved Hidden Nodes", "Mean Solved Birth Generation", "Mean Champion Fitness", "Mean Champion Complexity", "Mean Champion Hidden Nodes"]
    writer = csv.DictWriter(output, lineterminator='\n', fieldnames=fields)
    writer.writeheader()

    run = 0

    for r in results:
        run += 1
        times = []
        fitness = []
        complexity = []
        hiddens = []
        solvedGens = []
        solvedHiddens = []
        solvedBirthGens = []

        count = sum(1 for row in open(r))
        comment = "-"
        startTime = None

        with open(r) as input:
            reader = csv.DictReader(input)

            i = 0
            for row in reader:
                i += 1

                if i == 1:
                   if row.has_key("Comment"):
                        comment = row["Comment"]

                   if row.has_key("Start Time"):
                        startTime = datetime.datetime.strptime(row["Start Time"], "%m%d%Y-%H%M%S")

                t = time.strptime(row["Time"], "%Hh:%Mm:%Ss:%fms")
                times.append(datetime.timedelta(hours=t.tm_hour, minutes=t.tm_min, seconds=t.tm_sec))

                fitness.append(float(row["Champion Fitness"]))
                complexity.append(float(row["Champion Complexity"]))
                hiddens.append(float(row["Champion Hidden Nodes"]))
    
                if row["Solved"] == "True":
                    solvedGens.append(float(row["Generations"]))
                    solvedHiddens.append(int(row["Champion Hidden Nodes"]))
                    solvedBirthGens.append(int(row["Champion Birth Generation"]))

        
        totalTime = sum(times, datetime.timedelta(0))
        avgTime = totalTime / count

        solvedCount = len(solvedGens)
        
        gensSolvedMean = -1
        gensSolvedStdDev = -1
        meanSolvedHiddenNodes = -1
        meanSolvedBirthGens = -1

        if solvedCount > 0:
            gensSolvedMean = sum(solvedGens) / solvedCount

            # Standard deviation            
            gSum = 0
            for g in solvedGens:
                v = g - gensSolvedMean
                gSum += v * v
       
                gensSolvedStdDev = math.sqrt(gSum / solvedCount)
        
            meanSolvedHiddenNodes = float(sum(solvedHiddens)) / solvedCount
            meanSolvedBirthGens = float(sum(solvedBirthGens)) / solvedCount

        row = {}
        
        row["Run"] = run
        row["Comment"] = comment
        row["Start time"] = startTime
        row["Total time spent"] = totalTime
        row["Average time spent"] = avgTime
        row["Experiments"] = count
        row["Solves"] = len(solvedGens)
        row["Solve Percentage"] = "%s%%" % round(float(solvedCount) / count * 100, 2)
        row["Mean Generations Solved"] = round(gensSolvedMean, 2)
        row["Generations Solved Standard Deviation"] = round(gensSolvedStdDev, 2)
        row["Mean Solved Hidden Nodes"] = round(meanSolvedHiddenNodes, 2)
        row["Mean Solved Birth Generation"] = round(meanSolvedBirthGens, 2)
        row["Mean Champion Fitness"] = round(sum(fitness) / count, 4)
        row["Mean Champion Complexity"] = round(sum(complexity) / count, 2)
        row["Mean Champion Hidden Nodes"] = round(sum(hiddens) / count, 2)

        writer.writerow(row)

print "Success"