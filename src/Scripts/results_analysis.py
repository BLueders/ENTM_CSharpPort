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

filename = "results.csv"

wdir = os.getcwd()
for root, dirs, files in os.walk(wdir):
    for file in files:
        if file == filename:
            results.append(os.path.join(root, file))

if len(results) == 0:
    print "No results files found, aborting."
    sys.exit(0)

print "Found %s results files" % len(results)

with open("analysis.csv", 'w') as output:
    fields = ["Run", "Path", "Comment", "Start time", "Total time spent", "Average time spent", "Experiments", "Solves", "Solve Percentage", "Max Generations",
              "Generations Solved Mean", "Generations Solved Standard Deviation", "Generations Solved Min", "Generations Solved Max", 
              "Complexity Solved Mean", "Complexity Solved Standard Deviation", "Complexity Solved Min", "Complexity Solved Max", 
              "Hidden Nodes Solved Mean", "Hidden Nodes Solved Standard Deviation", "Hidden Nodes Solved Min", "Hidden Nodes Solved Max", 
              "Champion Fitness Mean", "Tested Fitness Mean", "Generalization Fitness Mean", 
              "Tested Fitness Solved Mean", "Tested Fitness Solved Standard Deviation", "Generalization Fitness Solved Mean", "Generalization Fitness Solved Standard Deviation", 
              "Champion Complexity Mean", "Champion Hidden Nodes Mean"]

    writer = csv.DictWriter(output, lineterminator='\n', fieldnames=fields)
    writer.writeheader()
    
    run = 0
    
    for r in results:
        run += 1
        path = r.replace(wdir, "").replace(filename, "")

        times = []
        fitness = []
        fitnessTest = []
        fitnessTestSolved = []
        fitnessGen = []
        fitnessGenSolved = []
        
        complexity = []
        hiddens = []

        solvedGens = []
        solvedComplexity = []
        solvedHiddens = []

        maxGens = 0
        minGensSolved = sys.maxsize
        maxGensSolved = 0

        minHiddenNodesSolved = sys.maxsize
        maxHiddenNodesSolved = 0

        minComplexitySolved = sys.maxsize
        maxComplexitySolved = 0


        # -1 for header
        count = sum(1 for row in open(r)) - 1
        comment = "-"
        startTime = None

        with open(r) as input:
            reader = csv.DictReader(input)

            i = 0
            for row in reader:
                i += 1

                solved = row["Solved"] == "True"

                if i == 1:
                   if row.has_key("Comment"):
                        comment = row["Comment"]

                   if row.has_key("Start Time"):
                        startTime = datetime.datetime.strptime(row["Start Time"], "%d%m%Y-%H%M%S")

                t = time.strptime(row["Time"], "%Hh:%Mm:%Ss:%fms")
                times.append(datetime.timedelta(hours=t.tm_hour, minutes=t.tm_min, seconds=t.tm_sec))

                fitness.append(float(row["Champion Fitness"]))
                
                if row.has_key("Tested Fitness"):
                    f = float(row["Tested Fitness"])
                    fitnessTest.append(f)
                    if solved:
                        fitnessTestSolved.append(f)

                if row.has_key("Tested Generalization Fitness"):
                    f = float(row["Tested Generalization Fitness"])
                    fitnessGen.append(f)
                    if solved:
                        fitnessGenSolved.append(f)

                comp = float(row["Champion Complexity"])
                complexity.append(comp)
                
                hid = float(row["Champion Hidden Nodes"])
                hiddens.append(hid)
    
                gens = float(row["Generations"])

                if gens > maxGens:
                    maxGens = gens

                if solved:
                    solvedGens.append(gens)
                    solvedComplexity.append(comp)
                    solvedHiddens.append(hid)
                    
                    if gens < minGensSolved:
                        minGensSolved = gens
                    if gens > maxGensSolved:
                        maxGensSolved = gens

                    if comp < minComplexitySolved:
                        minComplexitySolved = comp
                    if comp > maxComplexitySolved:
                        maxComplexitySolved = comp

                    if hid < minHiddenNodesSolved:
                        minHiddenNodesSolved = hid
                    if hid > maxHiddenNodesSolved:
                        maxHiddenNodesSolved = hid

        
        totalTime = sum(times, datetime.timedelta(0))
        avgTime = totalTime / count

        solvedCount = len(solvedGens)
        
        gensSolvedMean = -1
        gensSolvedStdDev = -1
        complexitySolvedMean = -1
        complexitySolvedStdDev = -1
        hiddenNodesSolvedMean = -1
        hiddenNodesSolvedStdDev = -1

        testedFitnessSolvedStdDev = -1
        generalizationFitnessSolvedStdDev = -1
        
        meanTestedFitness = -1
        if len(fitnessTest) > 0:
            meanTestedFitness = sum(fitnessTest) / len(fitnessTest)

        meanGeneralizationFitness = -1
        if len(fitnessGen) > 0:
            meanGeneralizationFitness = sum(fitnessGen) / len(fitnessGen)

        meanTestedFitnessSolved = -1
        if len(fitnessTestSolved) > 0:
            meanTestedFitnessSolved = sum(fitnessTestSolved) / len(fitnessTestSolved)
            testedFitnessSolvedStdDev = stddev(fitnessTestSolved)

        meanGeneralizationFitnessSolved = -1
        if len(fitnessGenSolved) > 0:
            meanGeneralizationFitnessSolved = sum(fitnessGenSolved) / len(fitnessGenSolved)
            generalizationFitnessSolvedStdDev = stddev(fitnessGenSolved)

        if solvedCount == 0:
            minGensSolved = -1
            maxGensSolved = -1
            minComplexitySolved = -1
            maxComplexitySolved = -1
            minHiddenNodesSolved = -1
            maxHiddenNodesSolved = -1

        elif solvedCount > 0:
            gensSolvedMean = sum(solvedGens) / solvedCount
            gensSolvedStdDev = stddev(solvedGens)
    
            complexitySolvedMean = sum(solvedComplexity) / solvedCount
            complexitySolvedStdDev = stddev(solvedComplexity)

            hiddenNodesSolvedMean = float(sum(solvedHiddens)) / solvedCount
            hiddenNodesSolvedStdDev = stddev(solvedHiddens)



        row = {}
        
        row["Run"] = run
        row["Path"] = path
        row["Comment"] = comment
        row["Start time"] = startTime
        row["Total time spent"] = totalTime
        row["Average time spent"] = avgTime
        row["Experiments"] = count
        row["Solves"] = len(solvedGens)
        row["Solve Percentage"] = "%s%%" % round(float(solvedCount) / count * 100, 2)
        row["Max Generations"] = maxGens

        row["Generations Solved Mean"] = round(gensSolvedMean, 2)
        row["Generations Solved Standard Deviation"] = round(gensSolvedStdDev, 2)
        row["Generations Solved Min"] = minGensSolved
        row["Generations Solved Max"] = maxGensSolved

        row["Complexity Solved Mean"] = round(complexitySolvedMean, 2)
        row["Complexity Solved Standard Deviation"] = round(complexitySolvedStdDev, 2)
        row["Complexity Solved Min"] = minComplexitySolved
        row["Complexity Solved Max"] = maxComplexitySolved

        row["Hidden Nodes Solved Mean"] = round(hiddenNodesSolvedMean, 2)
        row["Hidden Nodes Solved Standard Deviation"] = round(hiddenNodesSolvedStdDev, 2)
        row["Hidden Nodes Solved Min"] = minHiddenNodesSolved
        row["Hidden Nodes Solved Max"] = maxHiddenNodesSolved

        row["Champion Fitness Mean"] = round(sum(fitness) / count, 4)
        row["Tested Fitness Mean"] = round(meanTestedFitness, 4)
        row["Generalization Fitness Mean"] = round(meanGeneralizationFitness, 4)

        row["Tested Fitness Solved Mean"] = round(meanTestedFitnessSolved, 4)
        row["Tested Fitness Solved Standard Deviation"] = round(testedFitnessSolvedStdDev, 4)
        row["Generalization Fitness Solved Mean"] = round(meanGeneralizationFitnessSolved, 4)
        row["Generalization Fitness Solved Standard Deviation"] = round(generalizationFitnessSolvedStdDev, 4)

        row["Champion Complexity Mean"] = round(sum(complexity) / count, 2)
        row["Champion Hidden Nodes Mean"] = round(sum(hiddens) / count, 2)

        writer.writerow(row)

print "Success"