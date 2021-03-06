﻿using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using WindowConfigurator.Geometry;
using WindowConfigurator.Core;

namespace WindowConfigurator
{
    public class AddIntermediate : Command
    {
        static AddIntermediate _instance;
        public AddIntermediate()
        {
            _instance = this;
        }

        ///<summary>The only instance of the MyCommand1 command.</summary>
        public static AddIntermediate Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "AddIntermediate"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("The {0} command will add an intermediate now.", EnglishName);

            Point3d pt0;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("Please select the start point");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No start point was selected.");
                    return getPointAction.CommandResult();
                }
                pt0 = getPointAction.Point();
            }

            Point3d pt1;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("Please select the end point");
                getPointAction.SetBasePoint(pt0, true);
                getPointAction.DynamicDraw +=
                  (sender, e) => e.Display.DrawLine(pt0, e.CurrentPoint, System.Drawing.Color.DarkRed);
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No end point was selected.");
                    return getPointAction.CommandResult();
                }
                pt1 = getPointAction.Point();
                if (pt0.Z != pt1.Z)
                {
                    RhinoApp.WriteLine("Invalid transom.");
                    return getPointAction.CommandResult();
                }
            }

            using (GetString getArticleNumberAction = new GetString())
            {
                getArticleNumberAction.SetCommandPrompt("Please input the article Number.");
                if (getArticleNumberAction.Get() != GetResult.String)
                {
                    RhinoApp.WriteLine("No article number was input.");
                    return getArticleNumberAction.CommandResult();
                }

                //TODO AddArticleNumber to Window wireframe.
            }

            Guid guid = doc.Objects.AddLine(pt0, pt1);

            Point3 p0;
            Point3 p1;
            if (pt0.Y < pt1.Y)
            {
                p0 = new Point3(pt0.X, pt0.Y, pt0.Z);
                p1 = new Point3(pt1.X, pt1.Y, pt1.Z);
            }
            else
            {
                p0 = new Point3(pt1.X, pt1.Y, pt1.Z);
                p1 = new Point3(pt0.X, pt0.Y, pt0.Z);
            }
            
            /// TODO: Update extrusion guid
            Transom transom = new Transom(p0, p1, guid, guid);
            InitializeWindow.window.wireFrame.addIntermediate(transom);

            doc.Views.Redraw();

            RhinoApp.WriteLine("The {0} command added one transom to the document.", EnglishName);

            return Result.Success;
        }
    }
}