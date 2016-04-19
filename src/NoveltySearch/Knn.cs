using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Core;

namespace ENTM.NoveltySearch
{
    public class Knn<TGenome> where TGenome : class, IGenome<TGenome>
    {
        public int K { get; }

        private Stopwatch _timer;

        public long TimeSpent => _timer.ElapsedMilliseconds;

        public Knn(int k)
        {
            K = k;
            _timer = new Stopwatch();
        }

        private Dictionary<Behaviour<TGenome>, double[]> neighbourhoods;

        /// <summary>
        /// Initialize KNN with a list of fitness info structs. The supplied start index determines where the algorithm will start comparing values in
        /// the auxFitnessArr array. Use this if not all values of the vector should be considered.
        /// </summary>
        public void Initialize(List<Behaviour<TGenome>> behaviourList, int startIndex)
        {
            _timer.Start();
            Behaviour<TGenome>[] behaviours = behaviourList.ToArray();


            int count = behaviours.Length;
            int vectorLength = behaviours[0].Score._auxFitnessArr.Length;
            neighbourhoods = new Dictionary<Behaviour<TGenome>, double[]>(count);

            for (int i = 0; i < count; i++)
            {
                Behaviour<TGenome> neighbour1 = behaviours[i];
                double[] neighbourhood1 = GetNeighbourhood(neighbour1, count);

                for (int j = i + 1; j < count; j++)
                {
                    Behaviour<TGenome> neighbour2 = behaviours[j];
                    double[] neighbourhood2 = GetNeighbourhood(neighbour2, count);

                    // Euclidian distance squared
                    double total = 0;
                    for (int k = startIndex; k < vectorLength; k++)
                    {
                        double d = neighbour1.Score._auxFitnessArr[k]._value - neighbour2.Score._auxFitnessArr[k]._value;
                        total += d * d;
                    }
                    neighbourhood1[j - 1] = total;
                    neighbourhood2[i] = total;
                }

                Array.Sort(neighbourhood1);
            }

            _timer.Stop();
        }

        public double AverageDistToKnn(Behaviour<TGenome> behaviour)
        {
            double[] neighbourhood = neighbourhoods[behaviour];
            double total = 0;
            for (int i = 0; i < K; i++)
            {
                total += neighbourhood[i];
            }

            return total / K;
        }

        private double[] GetNeighbourhood(Behaviour<TGenome> behaviour, int count)
        {
            double[] neighbourhood;
            if (!neighbourhoods.TryGetValue(behaviour, out neighbourhood))
            {
                neighbourhoods.Add(behaviour, neighbourhood = new double[count]);
            }

            return neighbourhood;
        }

        delegate void TreeVisitor<T>(T nodeData);

        class NTree<T>
        {
            private T data;
            private LinkedList<NTree<T>> children;

            public NTree(T data)
            {
                this.data = data;
                children = new LinkedList<NTree<T>>();
            }

            public void AddChild(T data)
            {
                children.AddFirst(new NTree<T>(data));
            }

            public NTree<T> GetChild(int i)
            {
                foreach (NTree<T> n in children)
                    if (--i == 0)
                        return n;
                return null;
            }

            public void Traverse(NTree<T> node, TreeVisitor<T> visitor)
            {
                visitor(node.data);
                foreach (NTree<T> kid in node.children)
                    Traverse(kid, visitor);
            }
        }
    }
}
