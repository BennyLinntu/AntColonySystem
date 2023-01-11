using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;

namespace AntColonySystem
{
    // data fileds
    public delegate double Objective_Function(int[] a_Solution);
    public enum OptimizationType { Minimization, Maximization }
    public enum PheromoneUpdateType { AllAnts, OnlyTheBest }

    // functions

    public class AntColony
    {
        // data fileds
        Random rnd;
        int numberOfCites;
        double[,] pheromoneMatrix;
        double[,] heuristicMatrix;
        double pheromoneSquareFactor = 1;
        double heuristicSquareFactor = 3;
        int populationSize = 30;
        double threshold = 0.8;
        int[] candidateCites;
        double[] objectives;
        double[] fitness;
        int[][] soultions;
        int[] soFarTheBestSolution;
        double soFarTheBestObjective;
        double iterationAverageObjective;
        double iterationtheBestObjective;
        int iterationCount;
        int iterationLimit = 500;
        double initialPheromoneValue = 0.001;
        double pheromoneEvaperationRate = 0.01;
        double pheromoneDropMulitplier = 100;
        double pheromoneDropAmount = 0.3;

        Objective_Function objectiveFunction;
        OptimizationType optimizationType;
        PheromoneUpdateType pheromoneUpdateType = PheromoneUpdateType.AllAnts;

        Series seriessoFarTheBestObjective;
        Series seriesiterationAverageObjective;
        Series seriesiterationTheBestObjective;

        // Properties
        [Browsable(false)]
        public int[][] Soultions { get => soultions; set => soultions = value; }
        [Browsable(false)]
        public double[,] Pheromone_Matrix { get => pheromoneMatrix; set => pheromoneMatrix = value; }
        [Browsable(false)]
        public Series SeriesSoFarTheBestObjective { get => seriessoFarTheBestObjective; set => seriessoFarTheBestObjective = value; }
        [Browsable(false)]
        public Series SeriesIterationAverageObjective { get => seriesiterationAverageObjective; set => seriesiterationAverageObjective = value; }
        [Browsable(false)]
        public Series SeriesiterationTheBestObjective { get => seriesiterationTheBestObjective; set => seriesiterationTheBestObjective = value; }
        [Browsable(false)]
        public int[] SoFarTheBestSolution { get => soFarTheBestSolution; set => soFarTheBestSolution = value; }
        [Browsable(false)]
        public double SoFarTheBestObjective { get => soFarTheBestObjective; set => soFarTheBestObjective = value; }
        [Category("Model Parameters")]
        public OptimizationType OptimizationType { get => optimizationType; set => optimizationType = value; }
        [Category("Model Parameters")]
        public int Population { get => populationSize; set => populationSize = value; }
        [Category("Model Parameters")]
        public double PheromoneFactor { get => pheromoneSquareFactor; set => pheromoneSquareFactor = value; }
        [Category("Model Parameters")]
        public double HeuristicFactor { get => heuristicSquareFactor; set => heuristicSquareFactor = value; }
        [Category("Model Parameters")]
        public double DeterministicPercentage { get => threshold; set => threshold = value; }
        [Category("Iteration")]
        public int CurrentIteration { get => iterationCount; set => iterationCount = value; }
        [Category("Iteration")]
        public int IterationLimit { get => iterationLimit; set => iterationLimit = value; }
        [Category("Pheromone Matrix")]
        public double InitialValue { get => initialPheromoneValue; set => initialPheromoneValue = value; }
        [Category("Pheromone Matrix")]
        public PheromoneUpdateType UpdateType { get => pheromoneUpdateType; set => pheromoneUpdateType = value; }
        [Category("Pheromone Matrix")]
        public double DropMultiplier { get => pheromoneDropMulitplier; set => pheromoneDropMulitplier = value; }
        [Category("Pheromone Matrix")]
        public double EvaperationRate { get => pheromoneEvaperationRate; set => pheromoneEvaperationRate = value; }
        [Category("Pheromone Matrix")]
        public double DropAmount { get => pheromoneDropAmount; set => pheromoneDropAmount = value; }

        // cord function
        public AntColony(int numberOfVariable, double[,] heuristicMatrix,
            OptimizationType optimizationType, Objective_Function objectiveFunction)
        {
            // assigning variables
            this.numberOfCites = numberOfVariable;
            this.heuristicMatrix = heuristicMatrix;
            this.optimizationType = optimizationType;
            this.objectiveFunction = objectiveFunction;

            // series
            seriesiterationAverageObjective = new Series();
            seriessoFarTheBestObjective = new Series();
            seriesiterationTheBestObjective = new Series();
            SeriesIterationAverageObjective.ChartType = SeriesChartType.Line;
            SeriesSoFarTheBestObjective.ChartType = SeriesChartType.Line;
            seriesiterationTheBestObjective.ChartType = SeriesChartType.Line;

            // calculate drop muliper
            double max;
            double total_Min = 0;
            for (int i = 0; i < numberOfCites; i++)
            {
                max = double.MinValue;
                for (int j = 0; j < numberOfCites; j++)
                {
                    if (max < heuristicMatrix[i, j] && heuristicMatrix[i, j] != 0)
                        max = heuristicMatrix[i, j];
                }
                total_Min += 1.0 / max;
            }
            pheromoneDropMulitplier = total_Min / 2.0;
        }

        public void Reset()
        {
            // reallocate memory
            rnd = new Random();
            pheromoneMatrix = new double[numberOfCites, numberOfCites];
            candidateCites = new int[numberOfCites];
            fitness = new double[numberOfCites];
            soultions = new int[populationSize][];
            objectives = new double[populationSize];
            soFarTheBestSolution = new int[numberOfCites];
            for (int ant_ID = 0; ant_ID < populationSize; ant_ID++)
                soultions[ant_ID] = new int[numberOfCites];

            // Create initial pheromone matrix
            for (int i = 0; i < numberOfCites; i++)
                for (int j = 0; j < numberOfCites; j++)
                    pheromoneMatrix[i, j] = initialPheromoneValue;

            // Create initial solution
            for (int i = 0; i < populationSize; i++)
            {
                candidateCites = Shuffle_A_Array(numberOfCites, numberOfCites);
                for (int j = 0; j < numberOfCites; j++)
                    soultions[i][j] = candidateCites[j];
            }

            // reset so far the best
            if (optimizationType == OptimizationType.Maximization)
                soFarTheBestObjective = double.MinValue;
            else
                soFarTheBestObjective = double.MaxValue;

            // reset iteration count
            iterationCount = 0;

            // series
            seriesiterationAverageObjective.Points.Clear();
            SeriesSoFarTheBestObjective.Points.Clear();
            seriesiterationTheBestObjective.Points.Clear();
            seriesiterationAverageObjective.Name = "Iteration Average";
            SeriesSoFarTheBestObjective.Name = "So Far The Best";
            seriesiterationTheBestObjective.Name = "Iteration The Best";
            seriesiterationAverageObjective.Color = Color.Green;
            SeriesSoFarTheBestObjective.Color = Color.Red;
            seriesiterationTheBestObjective.Color = Color.Blue;
            seriesiterationTheBestObjective.BorderWidth = 2;
            seriesiterationAverageObjective.BorderWidth = 2;
            SeriesSoFarTheBestObjective.BorderWidth = 2;
        }
        public void RunOneIteration()
        {
            PerformAllSolutionsConstruction();
            PerformsoFartheBestUpdate();
            PerformpheromoneMatrixUpdate();
            SeriesUpdate();
            iterationCount++;

        }

        public void RunToEnd()
        {
            for (int i = 0; i < iterationLimit; i++)
                RunOneIteration();
        }

        private void PerformAllSolutionsConstruction()
        {
            for (int i = 0; i < populationSize; i++)
            {
                ConstructASoultion(i, 0);
                objectives[i] = objectiveFunction(soultions[i]);
            }
        }
        private void PerformsoFartheBestUpdate()
        {
            double objective_Min = objectives[0];
            double objecive_Max = objectives[0];
            for (int i = 1; i < populationSize; i++)
            {
                if (objective_Min > objectives[i])
                    objective_Min = objectives[i];
                if (objecive_Max < objectives[i])
                    objecive_Max = objectives[i];
            }

            int index_Of_the_Best;
            if (optimizationType == OptimizationType.Maximization)
            {
                if (soFarTheBestObjective < objecive_Max)
                {
                    soFarTheBestObjective = objecive_Max;
                    index_Of_the_Best = Array.IndexOf(objectives, objecive_Max);
                    for (int i = 0; i < numberOfCites; i++)
                        soFarTheBestSolution[i] = soultions[index_Of_the_Best][i];
                }
                iterationtheBestObjective = objecive_Max;
            }
            else
            {
                if (soFarTheBestObjective > objective_Min)
                {
                    index_Of_the_Best = Array.IndexOf(objectives, objective_Min);
                    soFarTheBestObjective = objective_Min;
                    for (int i = 0; i < numberOfCites; i++)
                        soFarTheBestSolution[i] = soultions[index_Of_the_Best][i];
                }
                iterationtheBestObjective = objective_Min;
            }

            iterationAverageObjective = objectives.Sum() / (double)populationSize;
        }
        private void PerformpheromoneMatrixUpdate()
        {
            // evapetate
            for (int i = 0; i < numberOfCites; i++)
            {
                for (int j = 0; j < numberOfCites; j++)
                {
                    double tow = pheromoneMatrix[i, j];
                    pheromoneMatrix[i, j] = tow * (1.0 - pheromoneEvaperationRate);
                }
            }

            // dropping pheromone
            int p1;
            int p2;

            if (pheromoneUpdateType == PheromoneUpdateType.AllAnts)
            {
                // all ants drops 
                for (int ant_ID = 0; ant_ID < populationSize; ant_ID++)
                {
                    for (int i = 0; i < numberOfCites; i++)
                    {
                        p1 = soultions[ant_ID][i];
                        if (i == numberOfCites - 1)
                            p2 = soultions[ant_ID][0];
                        else
                            p2 = soultions[ant_ID][i + 1];
                        double amount = pheromoneEvaperationRate * pheromoneDropAmount;
                        pheromoneMatrix[p1, p2] += amount;
                        //pheromone_Matrix[p2, p1] = amount;
                    }
                }
            }
            else
            {
                double smallest = double.MaxValue;
                double largest = double.MinValue;
                for (int i = 0; i < populationSize; i++)
                {
                    if (smallest > objectives[i])
                        smallest = objectives[i];
                    if (largest < objectives[i])
                        largest = objectives[i];
                }
                int index_The_Best;
                if (optimizationType == OptimizationType.Maximization)
                    index_The_Best = Array.IndexOf(objectives, largest);
                else
                    index_The_Best = Array.IndexOf(objectives, smallest);

                // dropping
                for (int j = 0; j < numberOfCites; j++)
                {
                    p1 = soultions[index_The_Best][j];
                    if (j == numberOfCites - 1)
                        p2 = soultions[index_The_Best][0];
                    else
                        p2 = soultions[index_The_Best][j + 1];
                    double amount = pheromoneEvaperationRate * pheromoneDropAmount;
                    pheromoneMatrix[p1, p2] += amount;
                    //pheromone_Matrix[p2, p1] = amount;
                }
            }

        }

        private void ConstructASoultion(int antid, int nthStep)
        {
            if (nthStep == numberOfCites) // last step, record and return
            {
                // record candidate cities to solution
                for (int i = 0; i < numberOfCites; i++)
                    soultions[antid][i] = candidateCites[numberOfCites - i - 1];
                return;
            }
            if (nthStep == 0) // first step, reset candidate cities, and rnd starting point, 
            {
                // reset candidate cities
                candidateCites = Shuffle_A_Array(numberOfCites, numberOfCites);

                // swap first city with last candidate
                int firstCity = rnd.Next(0, numberOfCites);
                int indexofcityToinsolution = Array.IndexOf(soultions[antid], firstCity);
                (candidateCites[indexofcityToinsolution], candidateCites[numberOfCites - 1])
                    = (candidateCites[numberOfCites - 1], candidateCites[indexofcityToinsolution]);

                // find the next step
                nthStep++;
                ConstructASoultion(antid, nthStep);
                return;
            }
            int cityFrom = candidateCites[numberOfCites - nthStep];
            int index = 0;
            int cityTo;
            double distance_invese;
            double pheromone_density;
            if (rnd.NextDouble() >= threshold)
            {

                double Sumsofar = 0;
                double destination;

                // fill in fitness
                for (int i = 0; i < numberOfCites - nthStep; i++)
                {
                    distance_invese = heuristicMatrix[cityFrom, candidateCites[i]];
                    pheromone_density = pheromoneMatrix[cityFrom, candidateCites[i]];
                    fitness[i] = Math.Pow(distance_invese, heuristicSquareFactor)
                        * Math.Pow(pheromone_density, pheromoneSquareFactor)
                        + Sumsofar;
                    Sumsofar = fitness[i];
                }

                // randomly select
                destination = rnd.NextDouble() * Sumsofar;
                while (fitness[index] <= destination)
                    index++;

            }
            else
            {

                double largestFitness;
                // fill in fitness
                for (int i = 0; i < numberOfCites - nthStep; i++)
                {
                    distance_invese = heuristicMatrix[cityFrom, candidateCites[i]];
                    pheromone_density = pheromoneMatrix[cityFrom, candidateCites[i]];
                    fitness[i] = Math.Pow(distance_invese, heuristicSquareFactor)
                        * Math.Pow(pheromone_density, pheromoneSquareFactor);
                }

                // Determind select
                largestFitness = fitness[0];
                for (int i = 0; i < numberOfCites - nthStep; i++)
                    if (largestFitness < fitness[i])
                        largestFitness = fitness[i];

                index = Array.IndexOf(fitness, largestFitness);

            }
            cityTo = candidateCites[index];

            // swap city to  with the end element
            int indexofcityTo = Array.IndexOf(candidateCites, cityTo);
            (candidateCites[indexofcityTo], candidateCites[numberOfCites - nthStep - 1])
                = (candidateCites[numberOfCites - 1 - nthStep], candidateCites[indexofcityTo]);

            // find the next step
            nthStep++;
            ConstructASoultion(antid, nthStep);
        }

        public int[] Shuffle_A_Array(int n, int take)
        {
            int[] arr = Enumerable.Range(0, n).OrderBy(x => rnd.Next()).Take(take).ToList().ToArray();
            return arr;
        }
        private void SeriesUpdate()
        {
            seriesiterationAverageObjective.Points.AddXY(iterationCount, iterationAverageObjective);
            seriessoFarTheBestObjective.Points.AddXY(iterationCount, soFarTheBestObjective);
            SeriesiterationTheBestObjective.Points.AddXY(iterationCount, iterationtheBestObjective);
        }
        public string ReturnASolution(int id)
        {
            string str = string.Format("Ant {0:00 }:  ", id);
            for (int i = 0; i < numberOfCites; i++)
            {
                str += soultions[id][i].ToString();
                str += " ";
            }
            return str;
        }
        public string ReturnPheromoneMatrix(int r, int dc)
        {
            string str = string.Empty;
            double value;
            for (int i = 0; i < numberOfCites; i++)
            {
                value = Math.Round(pheromoneMatrix[r, i], dc);
                str += string.Format("  {0:00.000}  ", value);
            }
            return str;
        }

    }
}

