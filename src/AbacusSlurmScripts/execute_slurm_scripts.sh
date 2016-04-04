#!/bin/bash
for f in slurmscript_*
do
  echo "Processing $f file..."
  # queue job described in file f
  sbatch $f
done
echo Done with all files.