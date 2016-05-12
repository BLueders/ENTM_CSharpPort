import os
import sys
import csv
import time
import datetime
import math

# Standard deviation            
def stddev(values):
    count = len(values)
    mean = sum(values) / count
    vsum = 0
    for val in values:
        v = val - mean
        vsum += v * v
       
    return math.sqrt(vsum / count)


results = []

for root, dirs, files in os.walk(os.getcwd()):
    for file in files:
        if file == "results.csv":
            results.append(os.path.join(root, file))

print "Found %s results files" % len(results)

with open("analysis.csv", 'w') as output:
    fields = ["Run", "Comment", "Start time", "Total time spent", "Average time spent", "Experiments", "Solves", "Solve Percentage", 
              "Mean Generations Solved", "Min Generations Solved", "Max Generations Solved", "Generations Solved Standard Deviation", 
              "Mean Solved Hidden Nodes", "Min Solved Hidden Nodes", "Max Solved Hidden Nodes", "Solved Hidden Nodes Standard Deviation", 
              "Mean Champion Fitness", "Mean Champion Complexity", "Mean Champion Hidden Nodes"]

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

        minSolvedGens = sys.maxsize
        maxSolvedGens = 0

        minSolvedHiddens = sys.maxsize
        maxSolvedHiddens = 0

        # -1 for header
        count = sum(1 for row in open(r)) - 1
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
                
                hid = float(row["Champion Hidden Nodes"])
                hiddens.append(hid)
    
                if row["Solved"] == "True":
                    gens = float(row["Generations"])
                    solvedGens.append(gens)
                    solvedHiddens.append(hid)
                    
                    if gens < minSolvedGens:
                        minSolvedGens = gens
                    elif gens > maxSolvedGens:
                        maxSolvedGens = gens

                    if hid < minSolvedHiddens:
                        minSolvedHiddens = hid
                    elif hid > maxSolvedHiddens:
                        maxSolvedHiddens = hid

        
        totalTime = sum(times, datetime.timedelta(0))
        avgTime = totalTime / count

        solvedCount = len(solvedGens)
        
        gensSolvedMean = -1
        gensSolvedStdDev = -1
        meanSolvedHiddenNodes = -1
        hiddensSolvedStdDev = -1

        if solvedCount == 0:
            minSolvedGens = -1
            maxSolvedGens = -1
            minSolvedHiddens = -1
            maxSolvedHiddens = -1

        if solvedCount > 0:
            gensSolvedMean = sum(solvedGens) / solvedCount
            gensSolvedStdDev = stddev(solvedGens)
    
            meanSolvedHiddenNodes = float(sum(solvedHiddens)) / solvedCount
            hiddensSolvedStdDev = stddev(solvedHiddens)



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
        row["Min Generations Solved"] = minSolvedGens
        row["Max Generations Solved"] = maxSolvedGens
        row["Generations Solved Standard Deviation"] = round(gensSolvedStdDev, 2)

        row["Mean Solved Hidden Nodes"] = round(meanSolvedHiddenNodes, 2)
        row["Min Solved Hidden Nodes"] = minSolvedHiddens
        row["Max Solved Hidden Nodes"] = maxSolvedHiddens
        row["Solved Hidden Nodes Standard Deviation"] = round(hiddensSolvedStdDev, 2)

        row["Mean Champion Fitness"] = round(sum(fitness) / count, 4)
        row["Mean Champion Complexity"] = round(sum(complexity) / count, 2)
        row["Mean Champion Hidden Nodes"] = round(sum(hiddens) / count, 2)

        writer.writerow(row)

print "Success"