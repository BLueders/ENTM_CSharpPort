import os
import csv

results = []

for root, dirs, files in os.walk(os.getcwd()):
    for file in files:
        if file == "results.csv":
            results.append(os.path.join(root, file))

print "Found %s results files" % len(results)

with open("analysis.csv", 'w') as output:
    fields = ["Run", "Solves", "Average Generations", "Average Generations Solved", "Average Champion Fitness"]
    writer = csv.DictWriter(output, lineterminator='\n', fieldnames=fields)
    writer.writeheader()

    run = 0

    for r in results:
        run += 1
        fitness = []
        gens = []
        solvedGens = []

        with open(r) as input:
            reader = csv.DictReader(input)
        
            for row in reader:
                fitness.append(float(row["Champion Fitness"]))
                gens.append(float(row["Generations"]))
    
                if row["Solved"] == "True":
                    solvedGens.append(float(row["Generations"]))

        row = {}
        
        row["Run"] = run
        row["Solves"] = len(solvedGens)
        row["Average Generations"] = sum(gens) / len(gens)
        row["Average Generations Solved"] = sum(solvedGens) / len(solvedGens)
        row["Average Champion Fitness"] = sum(fitness) / len(fitness)

        writer.writerow(row)



