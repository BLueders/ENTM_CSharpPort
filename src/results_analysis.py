import os
import csv

results = []

for root, dirs, files in os.walk(os.getcwd()):
    for file in files:
        if file == "results.csv":
            results.append(os.path.join(root, file))

print "Found %s results files" % len(results)

with open("analysis.csv", 'w') as output:
    fields = ["Run", "Solves", "Average Generations", "Average Champion Fitness"]
    writer = csv.DictWriter(output, fieldnames=fields)
    writer.writeheader()

    run = 0

    for r in results:
        run += 1
        solves = 0
        gens = []
        fitness = []

        with open(r) as input:
            reader = csv.DictReader(input)
        
            for row in reader:
                if (row["Solved"] == "True"):
                    solves += 1

                fitness.append(float(row["Champion Fitness"]))
                gens.append(float(row["Generations"]))


        row = {}
        
        row["Run"] = run
        row["Solves"] = solves
        row["Average Generations"] = sum(gens) / len(gens)
        row["Average Champion Fitness"] = sum(fitness) / len(fitness)

        writer.writerow(row)



