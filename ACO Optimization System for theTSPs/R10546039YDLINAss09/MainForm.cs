using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using AntColonySystem;

namespace R10546039YDLINAss09
{
    public partial class MainForm : Form
    {

        // data fields
        int numberofCites;
        double[,] coordinates;
        double[,] distanceInversed;
        int[] soFartheBestRouth;
        AntColony acs = null;


        public MainForm()
        {
            InitializeComponent();
            chtLineShow.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chtLineShow.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chtLineShow.ChartAreas[0].AxisX.MajorTickMark.Enabled = false;
            chtLineShow.ChartAreas[0].AxisX.LabelStyle.Enabled = false;
            chtLineShow.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            chtLineShow.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
            chtLineShow.ChartAreas[0].AxisY.MajorTickMark.Enabled = false;
            chtLineShow.ChartAreas[0].AxisY.LabelStyle.Enabled = false;
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            chtResult.Series.Clear();
            acs.Reset();
            chtResult.Series.Add(acs.SeriesIterationAverageObjective);
            chtResult.Series.Add(acs.SeriesiterationTheBestObjective);
            chtResult.Series.Add(acs.SeriesSoFarTheBestObjective);
            // clear the chart and rtb, also need to add
            rtbPheromone.Clear();
            rtbSolution.Clear();
            chtLineShow.Series[1].Points.Clear();
            for (int i = 0; i < acs.Population; i++)
            {
                rtbSolution.AppendText($"{acs.ReturnASolution(i)}\n");
            }
            for (int i = 0; i < numberofCites; i++)
            {
                rtbPheromone.AppendText($"{acs.ReturnPheromoneMatrix(i, 3)}\n");
            }
            string str = string.Empty;
            
            for (int i = 0; i < numberofCites; i++)
            {
                str += acs.SoFarTheBestSolution[i] + " ";
            }
            double a = Math.Round(acs.SoFarTheBestObjective, 3);
            rtbShowFixedData.Clear();
            rtbShowFixedData.AppendText($"So Far the Best Soltion: {str}\n");
            rtbShowFixedData.AppendText($"So Far the Shortest Length: {a}\n");

            for (int i = 0; i < numberofCites; i++)
            {
                int index = acs.SoFarTheBestSolution[i];
                chtLineShow.Series[1].Points.AddXY(coordinates[index, 0], coordinates[index, 1]);
            }
            chtLineShow.Series[1].Points.AddXY(coordinates[acs.SoFarTheBestSolution[0], 0], coordinates[acs.SoFarTheBestSolution[numberofCites - 1], 1]);
            ppgShow.Update();
            chtLineShow.Update();
            chtResult.Update();
            btnRunOne.Enabled = true;
            btnRuntoEnd.Enabled = true;
            
        }
        // open data
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // check
            OpenFileDialog dlgOpen = new OpenFileDialog();
            if (dlgOpen.ShowDialog() != DialogResult.OK) return;

            // read line
            StreamReader sr = new StreamReader(dlgOpen.FileName);
            string str = string.Empty;
            str = sr.ReadLine();
            string problemName = str.Split(':')[1].Replace(" ", string.Empty);
            str = sr.ReadLine();
            string problemComment = str.Split(':')[1];
            str = sr.ReadLine();
            string problemType = str.Split(':')[1].Replace(" ", string.Empty);
            str = sr.ReadLine();
            numberofCites = int.Parse(str.Split(':')[1].Replace(" ", string.Empty));
            str = sr.ReadLine();
            string edgeWeightType = str.Split(':')[1].Replace(" ", string.Empty);

            chtLineShow.Series[0].Points.Clear();
            chtLineShow.Series[1].Points.Clear();
            str = sr.ReadLine();
            coordinates = new double[numberofCites, 2];
            char[] seps = { ' ', ',' };
            for (int i = 0; i < numberofCites; i++)
            {
                string[] item = sr.ReadLine().Split(seps, StringSplitOptions.RemoveEmptyEntries);
                coordinates[i, 0] = double.Parse(item[1]);
                coordinates[i, 1] = double.Parse(item[2]);
                DataPoint dp = new DataPoint(coordinates[i, 0], coordinates[i, 1]);
                dp.MarkerStyle = MarkerStyle.Square;
                dp.MarkerBorderWidth = 1;
                dp.MarkerSize = 5;
                dp.MarkerColor = Color.White;
                dp.MarkerBorderColor = Color.Black;
                dp.Label = (i).ToString();
                chtLineShow.Series[0].Points.Add(dp);
            }
            distanceInversed = new double[numberofCites, numberofCites];
            for (int i = 0; i < numberofCites; i++)
            {
                for (int j = 0; j < numberofCites; j++)
                {
                    if(i == j)
                    {
                        distanceInversed[i, j] = 0;
                    }
                    else
                    {
                        distanceInversed[i, j] = 1.0 / CalculateDistance(i, j);
                    }
                }

            }
            ShowProblemDescription(problemName, problemComment, problemType, edgeWeightType);
            sr.Close();

            int minSize = Math.Min(chtLineShow.Parent.Size.Width, chtLineShow.Parent.Size.Height);
            chtLineShow.Size = new Size(minSize, minSize);
            chtLineShow.Location = new Point((int)((chtLineShow.Parent.Width / 2.0) - (chtLineShow.Size.Width / 2.0)), 0);
            //rtbShowFixedData.AppendText($"So Far the Best Soltion: \n");
            //rtbShowFixedData.AppendText($"So Far the Shortest Length: \n");
            acs = new AntColony(numberofCites, distanceInversed, OptimizationType.Minimization, ObjectiveFunction);
            ppgShow.SelectedObject = acs;
            btnReset.Enabled = true;
        }

        private double CalculateDistance(int pindexa, int pindexb)
        {
            double distance;
            double[] p1 = new double[2];
            p1[0] = coordinates[pindexa, 0];
            p1[1] = coordinates[pindexa, 1];

            double[] p2 = new double[2];
            p2[0] = coordinates[pindexb, 0];
            p2[1] = coordinates[pindexb, 1];

            double xsqure = Math.Pow((p1[0] - p2[0]), 2);
            double ysqure = Math.Pow((p1[1] - p2[1]), 2);
            distance = Math.Sqrt(xsqure + ysqure);
            return distance;
        }

        private void ShowProblemDescription(string name, string Comment, string stype, string edgeweight)
        {
            rtbShowFixedData.Clear();
            rtbShowFixedData.AppendText($"Title: {name}\n");
            rtbShowFixedData.AppendText($"Description {Comment}\n");
            rtbShowFixedData.AppendText($"Dimension: {numberofCites}\n");
        }
        public double ObjectiveFunction(int[] solution)
        {
            double distance = 0;
            for (int i = 0; i < solution.Length - 1; i++)
            {
                distance += CalculateDistance(solution[i], solution[i + 1]);
            }
            distance += CalculateDistance(solution[0], solution[solution.Length - 1]);
            return distance;
        }

        private void btnRunOne_Click(object sender, EventArgs e)
        {
            acs.RunOneIteration();
            rtbPheromone.Clear();
            rtbSolution.Clear();
            chtLineShow.Series[1].Points.Clear();
            chtLineShow.Series[1].BorderWidth = 2;
            for (int i = 0; i < acs.Population; i++)
            {
                rtbSolution.AppendText($"{acs.ReturnASolution(i)}\n");
            }
            for (int i = 0; i < numberofCites; i++)
            {
                rtbPheromone.AppendText($"{acs.ReturnPheromoneMatrix(i, 3)}\n");
            }
            string str = string.Empty;

            for (int i = 0; i < numberofCites; i++)
            {
                str += acs.SoFarTheBestSolution[i] + " ";
            }
            double a = Math.Round(acs.SoFarTheBestObjective, 3);
            rtbShowFixedData.Clear();
            rtbShowFixedData.AppendText($"So Far the Best Soltion: {str}\n");
            rtbShowFixedData.AppendText($"So Far the Shortest Length: {a}\n");

            for (int i = 0; i < numberofCites; i++)
            {
                int index = acs.SoFarTheBestSolution[i];
                chtLineShow.Series[1].Points.AddXY(coordinates[index, 0], coordinates[index, 1]);
            }
            chtLineShow.Series[1].Points.AddXY(coordinates[acs.SoFarTheBestSolution[0], 0], coordinates[acs.SoFarTheBestSolution[numberofCites - 1], 1]);
            ppgShow.Update();
            chtLineShow.Update();
            chtResult.Update();
        }

        private void btnRuntoEnd_Click(object sender, EventArgs e)
        {
            acs.RunToEnd();
            rtbPheromone.Clear();
            rtbSolution.Clear();
            chtLineShow.Series[1].Points.Clear();
            chtLineShow.Series[1].BorderWidth = 2;
            for (int i = 0; i < acs.Population; i++)
            {
                rtbSolution.AppendText($"{acs.ReturnASolution(i)}\n");
            }
            for (int i = 0; i < numberofCites; i++)
            {
                rtbPheromone.AppendText($"{acs.ReturnPheromoneMatrix(i, 3)}\n");
            }
            string str = string.Empty;

            for (int i = 0; i < numberofCites; i++)
            {
                str += acs.SoFarTheBestSolution[i] + " ";
            }
            double a = Math.Round(acs.SoFarTheBestObjective, 3);
            rtbShowFixedData.Clear();
            rtbShowFixedData.AppendText($"So Far the Best Soltion: {str}\n");
            rtbShowFixedData.AppendText($"So Far the Shortest Length: {a}\n");

            for (int i = 0; i < numberofCites; i++)
            {
                int index = acs.SoFarTheBestSolution[i];
                chtLineShow.Series[1].Points.AddXY(coordinates[index, 0], coordinates[index, 1]);
            }
            chtLineShow.Series[1].Points.AddXY(coordinates[acs.SoFarTheBestSolution[0], 0], coordinates[acs.SoFarTheBestSolution[numberofCites - 1], 1]);
            ppgShow.Update();
            chtLineShow.Update();
            chtResult.Update();
        }
    }
}
