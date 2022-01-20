using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot;
using devDept.Graphics;
using devDept.Geometry;
using System.Collections;
using Environment = devDept.Eyeshot.Environment;
using Region = devDept.Eyeshot.Entities.Region;
using devDept.Eyeshot.Translators;
using System.IO;
using System.Reflection;
using System.Threading;

namespace WindowsApplication1
{
    public partial class Viewer3D : Form
    {
        public string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static FileSystemWatcher watcher;
        string scanFilePath = "";
        double currZMin = double.MaxValue;
        double currZMax = double.MinValue;
        double currXMax = double.MinValue;
        double currXMin = double.MaxValue;
        double currYMax = double.MinValue;
        double currYMin = double.MaxValue;

        public Viewer3D()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
            model1.Unlock("US2-FN12E-GPHW6-GX5Y-S78T"); // For more details see 'Product Activation' topic in the documentation.
            scanFilePath = Path.Combine(path, "scan.txt");
            File.WriteAllText(scanFilePath, "");
            //MessageBox.Show($"Scan file path: {scanFile}");
            model1.Grid.AutoSize = false;
            model1.ProgressChanged += Model1_ProgressChanged;
            model1.WorkCancelled += Model1_WorkCancelled;
            model1.WorkCompleted += Model1_WorkCompleted;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            model1.ProgressBar.Visible = false;
            CoordinateSystemIcon coor = CoordinateSystemIcon.GetDefaultCoordinateSystemIcon();
            coor.ArrowColorX = Color.Red;
            coor.ArrowColorY = Color.Green;
            coor.ArrowColorZ = Color.Blue;
            coor.LabelColor = Color.White;
            model1.Viewports[0].CoordinateSystemIcon = coor;
            /*Entity ent = FunctionPlot();
            // adds it to the vieport
            model1.Entities.Add(ent);
            // Sets trimetric view
            model1.SetView(viewType.Trimetric);

            // Fits the model in the viewport
            model1.ZoomFit();*/
            Watch(path);
        }

        public void LoadPointCloud()
        {
            Entity ent = FunctionPlot();

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    model1.Grid.Plane = new devDept.Geometry.Plane(new devDept.Geometry.Point3D(0, 0, currZMin), new devDept.Geometry.Vector3D(0D, 0D, 1D));
                    model1.Grid.Min = new Point3D(currXMin + 5, currYMin + 5, currZMin + 5);
                    model1.Grid.Max = new Point3D(currXMax + 5, currYMax + 5, currZMax + 5);
                    model1.Focus();
                    model1.Entities.Clear();
                    // adds it to the vieport
                    model1.Entities.Add(ent);
                    // Sets trimetric view
                    model1.SetView(viewType.Trimetric);
                    // Fits the model in the viewport
                    model1.ZoomFit();
                    model1.Refresh();
                }));
            }
            else
            {
                model1.Focus();
                model1.Entities.Clear();
                // adds it to the vieport
                model1.Entities.Add(ent);
                // Sets trimetric view
                model1.SetView(viewType.Trimetric);
                // Fits the model in the viewport
                model1.ZoomFit();
                model1.Refresh();
            }
        }

        public FastPointCloud FunctionPlot()
        {
            try
            {
                if (!File.Exists(scanFilePath))
                {
                    Console.WriteLine($"Cannot find scan file at path: {scanFilePath}");
                    return new FastPointCloud(new float[] { });
                }
                string[] lines = File.ReadAllLines(scanFilePath);
                PointCloud surface = new PointCloud(lines.Length, 1, PointCloud.natureType.Multicolor);

                // defines the colors of the points
                /*Legend legend = model1.Legends[0];
                legend.ColorTable = Legend.RedToBlue9;
                legend.Visible = true;
                int colorLen = legend.ColorTable.Length;
                legend.Title = "Distance";
                legend.Subtitle = "Absolute point-model distance (mm)";
                */
                double zMin = double.MaxValue;
                double zMax = double.MinValue;
                List<Point3D> points = new List<Point3D>();
                for (int i = 0; i < lines.Length; ++i)
                {
                    string[] coords = lines[i].Split(',');
                    if (coords.Length != 3)
                    {
                        Console.WriteLine($"Line {lines[i]} could not be split into 3 coordinates properly.");
                        continue;
                    }

                    if (!double.TryParse(coords[0], out double x))
                    {
                        Console.WriteLine($"Error parsing x from: {lines[i]}");
                        x = 0;
                    }

                    if (!double.TryParse(coords[1], out double y))
                    {
                        Console.WriteLine($"Error parsing y from: {lines[i]}");
                        y = 0;
                    }

                    if (!double.TryParse(coords[2], out double z))
                    {
                        Console.WriteLine($"Error parsing z from: {lines[i]}");
                        z = 0;
                    }
                    points.Add(new Point3D(x, y, z));
                    if (z < zMin)
                    {
                        zMin = z;
                    }
                    else if (z > zMax)
                    {
                        zMax = z;
                    }

                    if (x < currXMin)
                    {
                        currXMin = x;
                    }
                    else if (x > currXMax)
                    {
                        currXMax = x;
                    }

                    if (y < currYMin)
                    {
                        currYMin = y;
                    }
                    else if (y > currYMax)
                    {
                        currYMax = y;
                    }
                }
                currZMin = zMin;
                currZMax = zMax;
                /*for (int i = 0; i < points.Count; ++i)
                {
                    Color clr = HsvToRgbWhiteEnd(ZNorm(points[i].Z, zMin, zMax) * 360 - 120, 1, 1);
                    surface.Vertices[i] = new PointRGB(points[i].X, points[i].Y, points[i].Z, clr);
                }*/

                for (int i = 0; i < points.Count; ++i)
                {
                    Color clr = transitionOfHueRange(ZNorm(points[i].Z, zMin, zMax), 360, 0);
                    surface.Vertices[i] = new PointRGB(points[i].X, points[i].Y, points[i].Z, clr);
                }

                return surface.ConvertToFastPointCloud();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown in FunctionPlot(): " + ex.Message);
                return new FastPointCloud(new float[] { });
            }
        }

        public static Color transitionOfHueRange(double percentage, int startHue, int endHue)
        {
            // From 'startHue' 'percentage'-many to 'endHue'
            // Finally map from [0°, 360°] -> [0, 1.0] by dividing
            double hue = ((percentage * (endHue - startHue)) + startHue) / 360;

            double saturation = 1.0;
            double lightness = 0.5;

            // Get the color
            return hslColorToRgb(hue, saturation, lightness);
        }

        public static Color hslColorToRgb(double hue, double saturation, double lightness)
        {
            if (saturation == 0.0)
            {
                // The color is achromatic (has no color)
                // Thus use its lightness for a grey-scale color
                int grey = percToColor(lightness);
                return Color.FromArgb(grey, grey, grey);
            }

            double q;
            if (lightness < 0.5)
            {
                q = lightness * (1 + saturation);
            }
            else
            {
                q = lightness + saturation - lightness * saturation;
            }
            double p = 2 * lightness - q;

            double oneThird = 1.0 / 3;
            double red = percToColor(hueToRgb(p, q, hue + oneThird));
            double green = percToColor(hueToRgb(p, q, hue));
            double blue = percToColor(hueToRgb(p, q, hue - oneThird));

            return Color.FromArgb((int)red, (int)green, (int)blue);
        }

        public static double hueToRgb(double p, double q, double t)
        {
            if (t < 0)
            {
                t += 1;
            }
            if (t > 1)
            {
                t -= 1;
            }

            if (t < 1.0 / 6)
            {
                return p + (q - p) * 6 * t;
            }
            if (t < 1.0 / 2)
            {
                return q;
            }
            if (t < 2.0 / 3)
            {
                return p + (q - p) * (2.0 / 3 - t) * 6;
            }
            return p;
        }

        public static int percToColor(double percentage)
        {
            return (int)Math.Round(percentage * 255);
        }

        public double ZNorm(double z, double zMin, double zMax)
        {
            return (zMax != zMin) ? ((zMax - z) / (zMax - zMin)) : 0;
        }
        public Color HsvToRgbWhiteEnd(double h, double S, double V)
        {
            int r, g, b;
            double H = h;

            while (H < 0)
            {
                H += 360;
            }

            while (H >= 360)
            {
                H -= 360;
            }

            double R, G, B;
            if (V <= 0)
            {
                R = G = B = 0;
            }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = 1;
                        G = 1 - tv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = ClampToByte((int)(R * 255.0));
            g = ClampToByte((int)(G * 255.0));
            b = ClampToByte((int)(B * 255.0));

            return Color.FromArgb(255, r, g, b);
        }
        public byte ClampToByte(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return (byte)i;
        }
        /*public FastPointCloud FunctionPlot()
        {
            try
            {
                string[] lines = File.ReadAllLines(@"C:\Users\3D Infotech\Desktop\scan.txt");
                PointCloud surface = new PointCloud(lines.Length, 1, PointCloud.natureType.Multicolor);
                surface.Color = Color.White;
                for (int i = 0; i < lines.Length; ++i)
                {
                    string[] coords = lines[i].Split(',');
                    if (coords.Length != 3)
                    {
                        Console.WriteLine($"Line {lines[i]} could not be split into 3 coordinates properly.");
                        continue;
                    }

                    if (!double.TryParse(coords[0], out double x))
                    {
                        Console.WriteLine($"Error parsing x from: {lines[i]}");
                        x = 0;
                    }

                    if (!double.TryParse(coords[1], out double y))
                    {
                        Console.WriteLine($"Error parsing y from: {lines[i]}");
                        y = 0;
                    }

                    if (!double.TryParse(coords[2], out double z))
                    {
                        Console.WriteLine($"Error parsing z from: {lines[i]}");
                        z = 0;
                    }
                    //surface.Vertices[i] = new Point3D(x, y, z);
                    surface.Vertices[i] = new PointRGB(x, y, z, Color.White);
                    
                }
                return surface.ConvertToFastPointCloud();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown in FunctionPlot(): " + ex.Message);
                return new FastPointCloud(new float[] { });
            }
        }*/

        public void Watch(string path)
        {
            // Create a new FileSystemWatcher and set its properties.
            watcher = new FileSystemWatcher
            {
                Path = path,

                /* Watch for changes in LastAccess and LastWrite times, and
                      the renaming of files or directories. */
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                   | NotifyFilters.FileName | NotifyFilters.DirectoryName,

                // Only watch text files.      
                Filter = "*.txt*"
            };

            // Add event handler.
            watcher.Changed += new FileSystemEventHandler(OnChanged);

            // Begin watching.      
            watcher.EnableRaisingEvents = true;
        }

        // Define the event handler.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (File.Exists(scanFilePath))
            {
                Thread.Sleep(250);
                LoadPointCloud();
            }
            else
            {
                Console.WriteLine($"File at location:{scanFilePath} not found.");
            }
        }

        private void Model1_ProgressChanged(object sender, devDept.Eyeshot.ProgressChangedEventArgs e)
        {
            labelProgressBar.Text = model1.ProgressBar.Text;
            progressBar1.Value = e.Progress;
        }

        private void Model1_WorkCancelled(object sender, EventArgs eventArgs)
        {
            labelProgressBar.Text = "Cancelled";
            progressBar1.Value = 0;
        }

        private void Model1_WorkCompleted(object sender, WorkCompletedEventArgs e)
        {
            labelProgressBar.Text = "Completed";
            if (e.WorkUnit is ReadFileAsync)
            {
                ReadFileAsync ra = (ReadFileAsync)e.WorkUnit;

                // updates model units and its related combo box
                if (e.WorkUnit is ReadFileAsyncWithBlocks)
                {
                    model1.Units = ((ReadFileAsyncWithBlocks)e.WorkUnit).Units;
                }

                ra.AddToScene(model1, new RegenOptions() { Async = true });
            }
            else if (e.WorkUnit is Regeneration)
            {
                model1.Entities.UpdateBoundingBox();
                model1.ZoomFit();
                model1.Invalidate();
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            model1.CancelWork();
        }

        private void Loadbtn_Click(object sender, EventArgs e)
        {
            string pathtxt = Pathtxt.Text.Trim();
            if (File.Exists(pathtxt))
            {
                Load3D(pathtxt);
            }
            else
            {
                Console.WriteLine($"File at location:{pathtxt} not found.");
            }
        }

        private void Load3D(string filePath)
        {
            // Pathtxt

            ReadFileAsync rfa = CreateReadFileAsync(filePath);
            if (rfa == null)
            {
                Console.WriteLine($"Error in reading file from path: {filePath}");
                return;
            }
            model1.Clear();

            model1.StartWork(rfa);

            model1.SetView(viewType.Trimetric, true, model1.AnimateCamera);
        }

        private ReadFileAsync CreateReadFileAsync(string fileName)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"File not found: {fileName}");
                return null;
            }
            string ext = System.IO.Path.GetExtension(fileName);

            if (ext != null)
            {
                ext = ext.TrimStart('.').ToLower();

                try
                {
                    switch (ext)
                    {
                        case "asc":
                            return new ReadASC(fileName);
                        case "stl":
                            return new ReadSTL(fileName);
                        case "obj":
                            return new ReadOBJ(fileName);
                        case "las":
                            return new ReadLAS(fileName);
                        case "3ds":
                            return new Read3DS(fileName);
                        case "igs":
                        case "iges":
                            return new ReadIGES(fileName);
                        case "stp":
                        case "step":
                            return new ReadSTEP(fileName);
                        case "ifc":
                        case "ifczip":
                            return new ReadIFC(fileName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading file of type: {ext}, exception: {ex.Message}");
                }
            }
            return null;
        }
    }

}