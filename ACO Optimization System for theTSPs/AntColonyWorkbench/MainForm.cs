using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using AntColonySystem;

namespace AntColonyWorkbench
{
    /// <summary>
    /// WinForms front-end for the Ant Colony System TSP solver.
    /// The form owns only presentation logic; all optimisation is in
    /// <see cref="AntColony"/>.
    /// </summary>
    public partial class MainForm : Form
    {
        private int numberofCites;
        private double[,] coordinates;
        private double[,] distanceInversed;
        private string edgeWeightType = "EUC_2D";
        private AntColony acs;

        public MainForm()
        {
            InitializeComponent();
            ConfigureCharts();
            SetOperationState(false);
        }

        private void ConfigureCharts()
        {
            chtLineShow.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chtLineShow.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chtLineShow.ChartAreas[0].AxisX.MajorTickMark.Enabled = false;
            chtLineShow.ChartAreas[0].AxisX.LabelStyle.Enabled = false;
            chtLineShow.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            chtLineShow.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
            chtLineShow.ChartAreas[0].AxisY.MajorTickMark.Enabled = false;
            chtLineShow.ChartAreas[0].AxisY.LabelStyle.Enabled = false;
            chtLineShow.ChartAreas[0].BackColor = Color.WhiteSmoke;
            chtResult.Titles.Clear();
            chtResult.Titles.Add("Convergence");
            chtResult.ChartAreas[0].BackColor = Color.White;
            chtResult.ChartAreas[0].Visible = true;
            chtResult.ChartAreas[0].AxisX.Enabled = AxisEnabled.True;
            chtResult.ChartAreas[0].AxisY.Enabled = AxisEnabled.True;
            chtResult.ChartAreas[0].AxisX.LabelStyle.Format = "0";
            chtResult.ChartAreas[0].AxisY.LabelStyle.Format = "0";
            chtLineShow.Series[0].ChartType = SeriesChartType.Point;
            chtLineShow.Series[0].ChartArea = "ChartArea1";
            chtLineShow.Series[0].MarkerStyle = MarkerStyle.Circle;
            chtLineShow.Series[0].MarkerSize = 7;
            chtLineShow.Series[0].MarkerColor = Color.White;
            chtLineShow.Series[0].MarkerBorderColor = Color.Black;
            chtLineShow.Series[0].Name = "Cities";
            chtLineShow.Series[1].ChartType = SeriesChartType.Line;
            chtLineShow.Series[1].BorderWidth = 2;
            chtLineShow.Series[1].Color = Color.RoyalBlue;
            chtLineShow.Series[1].Name = "Best tour";
        }

        private void SetOperationState(bool hasSolution)
        {
            btnReset.Enabled = acs != null;
            btnRunOne.Enabled = hasSolution;
            btnRuntoEnd.Enabled = hasSolution;
        }

        /// <summary>
        /// Reads a TSPLIB EUC_2D instance. Returns false and shows a friendly
        /// message instead of throwing for malformed or unsupported files.
        /// </summary>
        public bool LoadProblem(string path)
        {
            try
            {
                var lines = File.ReadAllLines(path);
                string name = Path.GetFileNameWithoutExtension(path);
                string comment = string.Empty;
                string type = "TSP";
                string edgeWeightType = "EUC_2D";
                int dimension = 0;
                int coordStart = -1;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("NAME", StringComparison.OrdinalIgnoreCase))
                        name = ValueAfterColon(line);
                    else if (line.StartsWith("COMMENT", StringComparison.OrdinalIgnoreCase))
                        comment = ValueAfterColon(line);
                    else if (line.StartsWith("TYPE", StringComparison.OrdinalIgnoreCase))
                        type = ValueAfterColon(line);
                    else if (line.StartsWith("DIMENSION", StringComparison.OrdinalIgnoreCase))
                        dimension = int.Parse(ValueAfterColon(line), CultureInfo.InvariantCulture);
                    else if (line.StartsWith("EDGE_WEIGHT_TYPE", StringComparison.OrdinalIgnoreCase))
                        edgeWeightType = ValueAfterColon(line);
                    else if (line.Equals("NODE_COORD_SECTION", StringComparison.OrdinalIgnoreCase))
                    {
                        coordStart = i + 1;
                        break;
                    }
                }

                if (dimension < 2 || coordStart < 0)
                    throw new InvalidDataException("The file is not a TSPLIB TSP instance with a NODE_COORD_SECTION.");
                if (!edgeWeightType.Equals("EUC_2D", StringComparison.OrdinalIgnoreCase) &&
                    !edgeWeightType.Equals("CEIL_2D", StringComparison.OrdinalIgnoreCase) &&
                    !edgeWeightType.Equals("ATT", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Unsupported EDGE_WEIGHT_TYPE '{edgeWeightType}'. Use EUC_2D, CEIL_2D or ATT.");

                var pts = new double[dimension, 2];
                var seen = new bool[dimension];
                int count = 0;
                for (int i = coordStart; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0) continue;
                    if (line.Equals("EOF", StringComparison.OrdinalIgnoreCase)) break;
                    var parts = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3) continue;
                    int id = int.Parse(parts[0], CultureInfo.InvariantCulture);
                    if (id < 1 || id > dimension)
                        throw new InvalidDataException($"City id {id} is outside the declared dimension {dimension}.");
                    pts[id - 1, 0] = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    pts[id - 1, 1] = double.Parse(parts[2], CultureInfo.InvariantCulture);
                    seen[id - 1] = true;
                    count++;
                }
                if (count != dimension || seen.Any(v => !v))
                    throw new InvalidDataException($"Expected {dimension} cities but found {count}.");

                numberofCites = dimension;
                this.edgeWeightType = edgeWeightType;
                coordinates = pts;
                distanceInversed = new double[numberofCites, numberofCites];
                for (int i = 0; i < numberofCites; i++)
                {
                    for (int j = 0; j < numberofCites; j++)
                    {
                        distanceInversed[i, j] = i == j ? 0 : 1.0 / Math.Max(Distance(i, j), 1e-9);
                    }
                }

                acs = new AntColony(numberofCites, distanceInversed, OptimizationType.Minimization, TourLength);
                ppgShow.SelectedObject = acs;
                acs.Reset();
                PopulateResultChart();
                DrawCities();
                UpdateSolutionDisplay();
                SetOperationState(true);
                ShowProblemDescription(name, comment, type, edgeWeightType);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load the TSP file:\r\n" + ex.Message,
                    "Open TSP instance", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        private static string ValueAfterColon(string line)
        {
            int colon = line.IndexOf(':');
            return colon >= 0 ? line.Substring(colon + 1).Trim() : line.Trim();
        }

        private double Distance(int a, int b)
        {
            double dx = coordinates[a, 0] - coordinates[b, 0];
            double dy = coordinates[a, 1] - coordinates[b, 1];
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (edgeWeightType.Equals("CEIL_2D", StringComparison.OrdinalIgnoreCase))
                return Math.Ceiling(d);
            if (edgeWeightType.Equals("ATT", StringComparison.OrdinalIgnoreCase))
            {
                double r = Math.Sqrt((dx * dx + dy * dy) / 10.0);
                double t = Math.Round(r, MidpointRounding.AwayFromZero);
                return t < r ? t + 1 : t;
            }
            return d;
        }

        public double TourLength(int[] solution)
        {
            double distance = 0;
            for (int i = 0; i < solution.Length; i++)
                distance += Distance(solution[i], solution[(i + 1) % solution.Length]);
            return distance;
        }

        private void DrawCities()
        {
            chtLineShow.Series[0].Points.Clear();
            chtLineShow.Series[1].Points.Clear();
            for (int i = 0; i < numberofCites; i++)
            {
                var dp = new DataPoint(coordinates[i, 0], coordinates[i, 1])
                {
                    Label = i.ToString(CultureInfo.InvariantCulture),
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 7,
                    MarkerColor = Color.White,
                    MarkerBorderColor = Color.Black
                };
                chtLineShow.Series[0].Points.Add(dp);
            }
            FitChartAxes();
        }

        private void FitChartAxes()
        {
            double minX = Enumerable.Range(0, numberofCites).Min(i => coordinates[i, 0]);
            double maxX = Enumerable.Range(0, numberofCites).Max(i => coordinates[i, 0]);
            double minY = Enumerable.Range(0, numberofCites).Min(i => coordinates[i, 1]);
            double maxY = Enumerable.Range(0, numberofCites).Max(i => coordinates[i, 1]);
            double padX = Math.Max((maxX - minX) * 0.08, 1);
            double padY = Math.Max((maxY - minY) * 0.08, 1);
            chtLineShow.ChartAreas[0].AxisX.Minimum = minX - padX;
            chtLineShow.ChartAreas[0].AxisX.Maximum = maxX + padX;
            chtLineShow.ChartAreas[0].AxisY.Minimum = minY - padY;
            chtLineShow.ChartAreas[0].AxisY.Maximum = maxY + padY;
            chtLineShow.ChartAreas[0].AxisX.IsMarginVisible = false;
            chtLineShow.ChartAreas[0].AxisY.IsMarginVisible = false;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open a TSPLIB instance";
                dlg.Filter = "TSPLIB files (*.tsp)|*.tsp|All files (*.*)|*.*";
                dlg.InitialDirectory = FindSamplesDirectory();
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                LoadProblem(dlg.FileName);
            }
        }

        private static string FindSamplesDirectory()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 5 && !string.IsNullOrEmpty(dir); i++)
            {
                string candidate = Path.Combine(dir, "samples");
                if (Directory.Exists(candidate)) return candidate;
                dir = Directory.GetParent(dir)?.FullName;
            }
            return Environment.CurrentDirectory;
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            if (acs == null)
            {
                MessageBox.Show(this, "Open a TSP instance first.", "No instance",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            acs.Reset();
            PopulateResultChart();
            UpdateSolutionDisplay();
            SetOperationState(true);
        }

        private void PopulateResultChart()
        {
            chtResult.Series.Clear();
            chtResult.Series.Add(acs.SeriesIterationAverageObjective);
            chtResult.Series.Add(acs.SeriesiterationTheBestObjective);
            chtResult.Series.Add(acs.SeriesSoFarTheBestObjective);
            foreach (Series series in chtResult.Series)
                series.ChartArea = chtResult.ChartAreas[0].Name;
            chtResult.ChartAreas[0].AxisX.Title = "Iteration";
            chtResult.ChartAreas[0].AxisY.Title = "Tour length";
            chtResult.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.Gainsboro;
            chtResult.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Gainsboro;
            chtResult.ChartAreas[0].RecalculateAxesScale();
        }

        private void btnRunOne_Click(object sender, EventArgs e)
        {
            if (acs == null) return;
            acs.RunOneIteration();
            UpdateSolutionDisplay();
            if (acs.CurrentIteration >= acs.IterationLimit) SetOperationState(false);
        }

        private void btnRuntoEnd_Click(object sender, EventArgs e)
        {
            if (acs == null) return;
            acs.RunToEnd();
            UpdateSolutionDisplay();
            SetOperationState(false);
        }

        private void UpdateSolutionDisplay()
        {
            if (acs == null) return;
            rtbPheromone.Clear();
            rtbSolution.Clear();
            int rows = Math.Min(numberofCites, 20);
            for (int i = 0; i < rows; i++)
                rtbPheromone.AppendText(acs.ReturnPheromoneMatrix(i, 4) + Environment.NewLine);
            if (rows < numberofCites)
                rtbPheromone.AppendText($"... {numberofCites - rows} more rows ..." + Environment.NewLine);

            int ants = Math.Min(acs.Population, 20);
            for (int i = 0; i < ants; i++)
                rtbSolution.AppendText(acs.ReturnASolution(i) + Environment.NewLine);

            string route = string.Join(" -> ", acs.SoFarTheBestSolution.Select(c => c.ToString()));
            rtbShowFixedData.Clear();
            rtbShowFixedData.AppendText($"Best tour: {route}" + Environment.NewLine);
            rtbShowFixedData.AppendText($"Tour length: {acs.SoFarTheBestObjective:F3}" + Environment.NewLine);
            rtbShowFixedData.AppendText($"Iteration: {acs.CurrentIteration} / {acs.IterationLimit}" + Environment.NewLine);

            chtLineShow.Series[1].Points.Clear();
            foreach (int city in acs.SoFarTheBestSolution)
                chtLineShow.Series[1].Points.AddXY(coordinates[city, 0], coordinates[city, 1]);
            if (acs.SoFarTheBestSolution.Length > 0)
                chtLineShow.Series[1].Points.AddXY(
                    coordinates[acs.SoFarTheBestSolution[0], 0],
                    coordinates[acs.SoFarTheBestSolution[0], 1]);

            FitChartAxes();
            chtLineShow.Invalidate();
            if (chtResult.ChartAreas.Count > 0)
                chtResult.ChartAreas[0].RecalculateAxesScale();
            chtResult.Update();
            chtResult.Invalidate();
            ppgShow.Refresh();
        }

        private void ShowProblemDescription(string name, string comment, string type, string edgeWeightType)
        {
            rtbShowFixedData.Clear();
            rtbShowFixedData.AppendText($"Name: {name}" + Environment.NewLine);
            if (!string.IsNullOrWhiteSpace(comment))
                rtbShowFixedData.AppendText($"Comment: {comment}" + Environment.NewLine);
            rtbShowFixedData.AppendText($"Type: {type} / {edgeWeightType}" + Environment.NewLine);
            rtbShowFixedData.AppendText($"Dimension: {numberofCites}" + Environment.NewLine);
            rtbShowFixedData.AppendText("Ready. Press Reset to initialise the colony." + Environment.NewLine);
        }

        /// <summary>Loads a bundled instance and optionally runs iterations (used by --demo).</summary>
        public void LoadDemo(string path, int iterations)
        {
            if (!LoadProblem(path)) return;
            btnReset_Click(this, EventArgs.Empty);
            if (iterations > 0)
            {
                acs.IterationLimit = iterations;
                acs.RunToEnd();
                UpdateSolutionDisplay();
                SetOperationState(false);
            }
        }
    }
}


