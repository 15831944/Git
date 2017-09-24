using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
using GI = Autodesk.AutoCAD.GraphicsInterface;

[assembly: CommandClass(
  typeof(AdjustAreaCommand.Extras)
)]
namespace AdjustAreaCommand
{
    public class Extras
    {
        Editor ed;
        Document doc;
        Database db;

        Point3d center = new Point3d();
        double radius = 0.001;
        double glyphHeight;
        DBObjectCollection shape = new DBObjectCollection();
        Polyline pline = null;
        int param = 0;
        static double defaultArea = 0;
        double totalArea = 0;

        public Extras()
        {
            doc = AcAp.DocumentManager.MdiActiveDocument;
            ed = doc.Editor;
            db = doc.Database;
            
        }
        [CommandMethod("AAA", CommandFlags.Modal)]
        public void AdjustAreaCommand()
        {
            try
            {
                PromptDoubleOptions pdo = new PromptDoubleOptions(
                    "\nSpecify the polyline area:")
                {
                    AllowNegative = false,
                    AllowNone = false,
                    AllowZero = false,
                    DefaultValue = defaultArea,
                    UseDefaultValue = defaultArea == 0 ? false : true
                };

                PromptDoubleResult pdr = ed.GetDouble(pdo);

                if (pdr.Status == PromptStatus.OK)
                {
                    totalArea = defaultArea = pdr.Value;
                }
                else
                    return;

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    ed.TurnForcedPickOn();
                    ed.PointMonitor += Ed_PointMonitor;
                    
                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect a polyline: ")
                    {
                        AllowNone = false
                    };
                    peo.SetRejectMessage("\n>>>this is not a polyline, Select a polyline: ");

                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status == PromptStatus.OK)
                    {
                        var pl = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                        var pickedPt = per.PickedPoint;
                        pickedPt = pl.GetClosestPointTo(pickedPt, true);
                        double par = Math.Floor(pl.GetParameterAtPoint(pickedPt));
                        AddArea(pl, par, true);
                        trans.Commit();
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
            finally
            {
                ed.PointMonitor -= Ed_PointMonitor;
                EraseTransientGraphics();
                pline = null;
            }
        }

        private void Ed_PointMonitor(object sender, PointMonitorEventArgs e)
        {
            try
            {
                var fsPaths = e.Context.GetPickedEntities();
                var pickedPt = e.Context.ComputedPoint;
                Point2d pixels = e.Context.DrawContext.Viewport.GetNumPixelsInUnitSquare(e.Context.RawPoint);
                int glyphSize = CustomObjectSnapMode.GlyphSize;
                glyphHeight = glyphSize / pixels.Y * 1.0;

                radius = glyphHeight / 2.0;
                center = e.Context.RawPoint + new Vector3d(3 * radius, 3 * radius, 0);

                // nothing under the mouse cursor.
                if (fsPaths == null || fsPaths.Length == 0)
                {
                    EraseTransientGraphics();
                    pline = null;
                    return;
                }
                var oIds = fsPaths[0].GetObjectIds();
                var id = oIds[oIds.GetUpperBound(0)];

                bool sameSegment = false;
                // check if hovering over the same object.
                if (pline != null && pline.Id == id)
                {
                    var p = pline.GetClosestPointTo(pickedPt, true);
                    var par = (int)pline.GetParameterAtPoint(p);
                    sameSegment = par == param;
                }

                if (sameSegment)
                    UpdateTransientGraphics();
                else
                {
                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        pline = trans.GetObject(id, OpenMode.ForRead) as Polyline;
                        EraseTransientGraphics();

                        if (pline == null || !pline.Closed)
                        {
                            strCurrentShape = "X";
                            AddTransientGraphics(null);
                        }
                        else
                        {
                            var p = pline.GetClosestPointTo(pickedPt, true);
                            param = (int)pline.GetParameterAtPoint(p);
                            var pl = pline.Clone() as Polyline;
                            pl.ColorIndex = 33;
                            pl.ConstantWidth = glyphHeight * 0.25;
                            if (!AddArea(pl, param))
                            {
                                strCurrentShape = "X";
                                pl = null;
                            }
                            else
                                strCurrentShape = "V";
                            AddTransientGraphics(pl);
                        }
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
            finally
            {
                
            }
        }

        bool AddArea(Polyline pline, double par, bool save = false)
        {
            double area = pline.GetArea();

            // get the surrounding parameters
            double pre1 = par > 0 ? par - 1 : pline.EndParam - 1;
            double pos1 = par + 1 == pline.EndParam ? 0 : par + 1;
            double pos2 = pos1 == pline.EndParam ? 1 : pos1 + 1;

            // get the the surrounding points
            var p1 = pline.GetPointAtParameter(pre1).GetPoint2d();
            var p2 = pline.GetPointAtParameter(par).GetPoint2d();
            var p3 = pline.GetPointAtParameter(pos1).GetPoint2d();
            var p4 = pline.GetPointAtParameter(pos2).GetPoint2d();

            var l1 = p2.GetDistanceTo(p3);
            var dA = (totalArea - Math.Abs(area));
            var ang1 = p1.GetVectorTo(p2).Angle;
            var ang2 = p4.GetVectorTo(p3).Angle;
            var ang = p2.GetVectorTo(p3).Angle;

            var dAng1 = area > 0 ? (ang - ang1) : (ang1 - ang);
            var dAng2 = area > 0 ? (ang - ang2) : (ang2 - ang);

            //get the offset (h) of the selected line 
            var f = 0.5 * (1 / Math.Tan(dAng2) - 1 / Math.Tan(dAng1));
            // if no enough area
            var val = l1 * l1 + 4 * dA * f;
            if (val < 0)
                return false;
            var h = Math.Abs(ang1 - ang2) < 0.000001 ?
                dA / l1 :
                (-l1 + Math.Sqrt(val)) / 2.0 / f;

            // update the movable end points
            var pt2 = p2.Polar(ang1, h / Math.Sin(dAng1));
            var pt3 = p3.Polar(ang2, h / Math.Sin(dAng2));
            if (save)
            {
                pline.UpgradeOpen();
            }
            pline.SetPointAt((int)par, pt2);
            pline.SetPointAt((int)pos1, pt3);
            return true;
        }

        string strCurrentShape = "V";
        private void AddTransientGraphics(Polyline pl)
        {
            CreateShape(center, radius, strCurrentShape);
            if (pl != null)
                shape.Add(pl);
            IntegerCollection col = new IntegerCollection();
            foreach (Entity ent in shape)
            {
                GI.TransientManager.CurrentTransientManager.AddTransient(
                ent, GI.TransientDrawingMode.DirectShortTerm, 128, col);
            }
        }
        
        private void UpdateTransientGraphics()
        {
            ModifyShape(center, radius, strCurrentShape);
            IntegerCollection col = new IntegerCollection();
            foreach (Entity ent in shape)
            {
                GI.TransientManager.CurrentTransientManager.UpdateTransient(
                ent, col);
            }
        }

        private void EraseTransientGraphics()
        {
            IntegerCollection col = new IntegerCollection();
            foreach (Entity entity in shape)
            {
                GI.TransientManager.CurrentTransientManager.EraseTransient(
                    entity, col);
            }
            shape.Clear();
        }

        void CreateShape(Point3d center, double radius, string curShape)
        {
            if (curShape == "X")
            {
                Line line1 = new Line(
                    center + new Vector3d(-radius, -radius, 0),
                    center + new Vector3d(+radius, +radius, 0))
                {
                    ColorIndex = 1
                };
                Line line2 = new Line(
                    center + new Vector3d(-radius, +radius, 0),
                    center + new Vector3d(+radius, -radius, 0))
                {
                    ColorIndex = 1
                };
                shape = new DBObjectCollection
                {
                    line1,
                    line2
                };
            }
            else if (curShape == "V")
            {
                Line line3 = new Line(
                    center + new Vector3d(-radius, -radius / 2.0, 0),
                    center + new Vector3d(-radius / 2.0, -radius, 0)
                    )
                {
                    ColorIndex = 3
                };
                Line line4 = new Line(
                    center + new Vector3d(-radius / 2.0, -radius, 0),
                    center + new Vector3d(radius, radius, 0)
                    )
                {
                    ColorIndex = 3
                };
                shape = new DBObjectCollection
                {
                    line3,
                    line4
                };
            }
        }

        void ModifyShape(Point3d center, double radius, string curShape)
        {
            if (curShape == "X")
            {
                Line l1 = shape[0] as Line;
                l1.StartPoint = center + new Vector3d(-radius, -radius, 0);
                l1.EndPoint = center + new Vector3d(+radius, +radius, 0);
                Line l2 = shape[1] as Line;
                l2.StartPoint = center + new Vector3d(-radius, +radius, 0);
                l2.EndPoint = center + new Vector3d(+radius, -radius, 0);
            }
            else if (curShape == "V")
            {
                Line l1 = shape[0] as Line;
                l1.StartPoint = center + new Vector3d(-radius, -radius / 2.0, 0);
                l1.EndPoint = center + new Vector3d(-radius / 2.0, -radius, 0);
                Line l2 = shape[1] as Line;
                l2.StartPoint = center + new Vector3d(-radius / 2.0, -radius, 0);
                l2.EndPoint = center + new Vector3d(radius, radius, 0);
            }
        }
    }

    public class Transients
    {

    }
}
