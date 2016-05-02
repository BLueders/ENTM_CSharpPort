#! /bin/bash
#
#SBATCH --account itureal_gpu     # account
#SBATCH --nodes 1                 # number of nodes
#SBATCH --time 24:00:00            # max time (HH:MM:SS)

echo executing: `basename "$0"`
echo Running on "$(hostname)"
echo Available nodes: "$SLURM_NODELIST"
echo Slurm_submit_dir: "$SLURM_SUBMIT_DIR"
echo Start time: "$(date)"

# Start in total 4*24 MPI ranks on all available CPU cores
srun mono ENTM.exe "seasontask/seasontask-5steps"

echo Done.