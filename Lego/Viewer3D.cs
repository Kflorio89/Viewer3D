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

namespace WindowsApplication1
{
    public partial class Viewer3D : Form
    {

        public Viewer3D()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
            model1.Unlock("US2-FN12E-GPHW6-GX5Y-S78T"); // For more details see 'Product Activation' topic in the documentation.

            model1.ProgressChanged += model1_ProgressChanged;
            model1.WorkCancelled += Model1_WorkCancelled;
            model1.WorkCompleted += model1_WorkCompleted;
        }     

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // @"C:\Users\kflor\OneDrive\Desktop\car_door_v1.stp"
            string file1 = @"C:\Users\kflor\OneDrive\Desktop\car_door_v1.stp";
            string file2 = @"C:\Users\3D Infotech\Desktop\Viewer3D-main\Thermostat.stp";

            model1.ProgressBar.Visible = false;
            CoordinateSystemIcon coor = CoordinateSystemIcon.GetDefaultCoordinateSystemIcon();
            coor.ArrowColorX = Color.Red;
            coor.ArrowColorY = Color.Green;
            coor.ArrowColorZ = Color.Blue;
            coor.LabelColor = Color.White;
            model1.Viewports[0].CoordinateSystemIcon = coor;
            Load3D(file2);
            /*            // Hides the Eyeshot progressBar because in this sample the progess is shown with a WinForms ProgressBar

                        BuildLego bl = new BuildLego();
                        model1.StartWork(bl); */
        }

        private void model1_ProgressChanged(object sender, devDept.Eyeshot.ProgressChangedEventArgs e)
        {
            labelProgressBar.Text = model1.ProgressBar.Text;
            progressBar1.Value = e.Progress;
        }

        private void Model1_WorkCancelled(object sender, EventArgs eventArgs)
        {
            labelProgressBar.Text = "Cancelled";
            progressBar1.Value = 0;
        }

        private void model1_WorkCompleted(object sender, WorkCompletedEventArgs e)
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

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            model1.CancelWork();
        }                  

        private void Loadbtn_Click(object sender, EventArgs e)
        {
            string path = Pathtxt.Text.Trim();
            if (File.Exists(path))
            {
                Load3D(path);
            }
            else
            {
                MessageBox.Show("File not found");
            }
        }

        private void Load3D(string path)
        {
            // Pathtxt

            ReadFileAsync rfa = CreateReadFileAsync(path);
            if (rfa == null)
            {
                MessageBox.Show($"Error in reading file from path: {path}");
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