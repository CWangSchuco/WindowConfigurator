﻿using System;
using System.IO;
using System.Collections.Generic;
using Rhino;
using Rhino.Input;
using Rhino.Commands;
using Rhino.Geometry;
using netDxf;
using netDxf.Entities;



namespace WindowConfigurator
{
    public class MyCommand1 : Command
    {
        static MyCommand1 _instance;
        public MyCommand1()
        {
            _instance = this;
        }

        ///<summary>The only instance of the MyCommand1 command.</summary>
        public static MyCommand1 Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "MyCommand1"; }
        }


        private static DxfDocument OpenProfile(string filename)
        {
            // open the profile file
            FileInfo fileInfo = new FileInfo(filename);

            // check if profile file is valid
            if (!fileInfo.Exists)
            {
                RhinoApp.WriteLine("THE FILE {0} DOES NOT EXIST", filename);
                return null;
            }
            DxfDocument dxf = DxfDocument.Load(filename, new List<string> { @".\Support" });

            // check if there has been any problems loading the file,
            if (dxf == null)
            {
                RhinoApp.WriteLine("ERROR LOADING {0}", filename);
                return null;
            }

            return dxf;
        }

        public List<Polygon> GetGeometry(string filename)
        {
            // read the dxf file
            DxfDocument dxfTest = OpenProfile(filename);
            int numberSegments = 1;
            int blockNumber = -1;

            var polygons = new List<Polygon>();

            // loop over all relevant blacks and store the hatch boundaries
            foreach (var bl in dxfTest.Blocks)
            {
                RhinoApp.WriteLine("The x location {0} ", bl.Origin.X);
                
                foreach (var ent in bl.Entities)
                {
                    if (ent.Layer.Name.ToString().Contains("hatch"))
                    {
                        Polygon Poly = new Polygon();
                        blockNumber++;
                        netDxf.Entities.HatchPattern hp = netDxf.Entities.HatchPattern.Solid;
                        netDxf.Entities.Hatch myHatch = new netDxf.Entities.Hatch(hp, false);
                        myHatch = (netDxf.Entities.Hatch)ent;
                        int pathNumber = -1;

                        foreach (var bPath in myHatch.BoundaryPaths)
                        {
                            pathNumber++;
                            var contour = new List<Point3d>();

                            // Store the contour
                            for (int i = 0; i < bPath.Edges.Count; i++)
                            {
                                RhinoApp.WriteLine(bPath.Edges[i].Type.ToString().ToLower());
                                switch (bPath.Edges[i].Type.ToString().ToLower())
                                {
                                    case "polyline":
                                        var myPolyline = (HatchBoundaryPath.Polyline)bPath.Edges[i];
                                        foreach (var vertex in myPolyline.Vertexes)
                                        {

                                            var vPolyline = new Point3d();
                                            vPolyline.Y = vertex.Y;
                                            if (vertex.X < 0)
                                                vPolyline.X = -vertex.X;
                                            else
                                                vPolyline.X = vertex.X;
                                            contour.Add(vPolyline);
                                        }
                                        break;

                                    case "line":

                                        var myLine = (HatchBoundaryPath.Line)bPath.Edges[i];
                                        var vLine = new Point3d();
                                        vLine.X = myLine.Start.X;
                                        vLine.Y = myLine.Start.Y;
                                        contour.Add(vLine);
                                        break;

                                    case "arc":
                                        var myArc = (HatchBoundaryPath.Arc)bPath.Edges[i];
                                        double delta = (myArc.EndAngle - myArc.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vArc = new Point3d();
                                            double angleArc = (myArc.StartAngle + j * delta) * Math.PI / 180.0;
                                            if (myArc.IsCounterclockwise == true)
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(angleArc);
                                            }
                                            else
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(Math.PI + angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(Math.PI - angleArc);
                                            }
                                            contour.Add(vArc);
                                        }
                                        break;

                                    case "ellipse":
                                        var myEllipse = (HatchBoundaryPath.Ellipse)bPath.Edges[i];
                                        double deltaEllipse = (myEllipse.EndAngle - myEllipse.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vEllipse = new Point3d();
                                            var ellipseRadius = Math.Sqrt(Math.Pow(myEllipse.EndMajorAxis.X, 2) + Math.Pow(myEllipse.EndMajorAxis.Y, 2));

                                            double angleEllipse = (myEllipse.StartAngle + j * deltaEllipse) * Math.PI / 180.0;
                                            if (myEllipse.IsCounterclockwise == true)
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(angleEllipse);
                                            }
                                            else
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(Math.PI + angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(Math.PI - angleEllipse);
                                            }
                                            contour.Add(vEllipse);
                                        }
                                        break;

                                }
                            }

                            bool isHole = true;
                            // Check if is hole
                            if (blockNumber > -1)
                            {
                                if (pathNumber == 0)
                                {
                                    isHole = false;
                                }
                                Poly.AddContour(contour, isHole);
                            }
                        }
                        polygons.Add(Poly);
                    }
                }
            }
            return polygons;
        }

        public Curve CreateCurve(List<Point3d> points)
        {
            RhinoApp.WriteLine("{0} points in current polygon loaded", points.Count);

            List<NurbsCurve> lines = new List<NurbsCurve>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                lines.Add(new Rhino.Geometry.Line(points[i], points[i + 1]).ToNurbsCurve());
            }
            lines.Add(new Rhino.Geometry.Line(points[points.Count - 1], points[0]).ToNurbsCurve());
            Curve contour = Curve.JoinCurves(lines.ToArray())[0];

            return contour;
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            RhinoApp.WriteLine("The {0} command will extrude a wireframe right now.", EnglishName);

            RhinoApp.WriteLine("Please select a dxf file.");
            var fileDialog = new Rhino.UI.OpenFileDialog { Filter = "DXF Files (*.dxf)|*.dxf" };
            if (!fileDialog.ShowOpenDialog())
                return Result.Cancel;

            string filename = fileDialog.FileName;
            List<Polygon> geometry = GetGeometry(filename);
            RhinoApp.WriteLine("{0} polygons loaded", geometry.Count);

            List<Point3d> extrusionVertex = new List<Point3d>();
            extrusionVertex.Add(new Point3d(1000, 0, 0));
            extrusionVertex.Add(new Point3d(0, 0, 0));
            extrusionVertex.Add(new Point3d(0, 0, 1000));
            extrusionVertex.Add(new Point3d(1000, 0, 1000));
            
            
            
            
            Curve rail_crv = CreateCurve(extrusionVertex);
            rail_crv.Domain = new Interval(0, 4000);

            doc.Objects.AddCurve(rail_crv);

            //Curve extrusionPath = new Rhino.Geometry.Line(new Point3d(0, 0, 0), new Point3d(0, 0, 1000)).ToNurbsCurve();

            Polygon polygon = geometry[0];
            List<Point3d> points = polygon.outCountour;
            RhinoApp.WriteLine("{0} points in current polygon loaded", points.Count);
            if (points.Count < 1)
            {
                RhinoApp.WriteLine("Error: polygon must contain at least one point");
            }

            Curve cross_sections = CreateCurve(polygon.outCountour);

            doc.Objects.AddCurve(cross_sections);

            //Rhino.DocObjects.ObjRef rail_ref;
            //var rc = RhinoGet.GetOneObject("Select rail curve", false, Rhino.DocObjects.ObjectType.Curve, out rail_ref);
            //if (rc != Result.Success)
            //    return rc;

            //var rail_crv = rail_ref.Curve();
            //if (rail_crv == null)
            //{
            //    RhinoApp.WriteLine("No rail selected");
            //    return Result.Failure;
            //}

            //var gx = new Rhino.Input.Custom.GetObject();
            //gx.SetCommandPrompt("Select cross section curves");
            //gx.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            //gx.EnablePreSelect(false, true);
            //gx.GetMultiple(1, 0);
            //if (gx.CommandResult() != Result.Success)
            //    return gx.CommandResult();

            //var cross_sections = new List<Rhino.Geometry.Curve>();
            //for (int i = 0; i < gx.ObjectCount; i++)
            //{
            //    var crv = gx.Object(i).Curve();
            //    if (crv != null)
            //        cross_sections.Add(crv);
            //}
            //if (cross_sections.Count < 1)
            //{
            //    RhinoApp.WriteLine("No cross section selected");
            //    return Result.Failure;
            //}


            var breps = Brep.CreateFromSweep(rail_crv, cross_sections, true, doc.ModelAbsoluteTolerance);
            RhinoApp.WriteLine("Brep numbers: {0} ", breps.Length);
            for (int i = 0; i < breps.Length; i++)
                doc.Objects.AddBrep(breps[i]);








            //var breps = Brep.CreateFromSweep(rail_crv, cross_sections, true , doc.ModelAbsoluteTolerance);
            //RhinoApp.WriteLine("Brep numbers: {0} ", breps.Length);
            //for (int i = 0; i < breps.Length; i++)
            //    doc.Objects.AddBrep(breps[i]);
            doc.Views.Redraw();
            RhinoApp.WriteLine("Sweep success");
            return Result.Success;
        }
    }
}