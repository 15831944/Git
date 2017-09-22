using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(
  typeof(AdjustAreaCommand.AddVertex)
)]

namespace AdjustAreaCommand
{
    public class AddVertex
    {
        Editor ed;
        Database db;

        public AddVertex()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            db = doc.Database;
            ed = doc.Editor;
        }

        [CommandMethod("AVX")]
        public void Avx()
        {
            int ver = AcAp.Version.Major;
            bool segHighlight = ver >= 18;
            bool loop = true;

            PromptEntityOptions peo = new PromptEntityOptions(
                "\nSelet asegment where to add a vertex: ");
            peo.SetRejectMessage("\nPolyline only: ");
            peo.AllowNone = false;
            peo.AllowObjectOnLockedLayer = false;
            peo.AddAllowedClass(typeof(Polyline), false);
            while (loop)
            {
                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                {
                    loop = false;
                    continue;
                }
                Matrix3d UCS = ed.CurrentUserCoordinateSystem;
                ObjectId objId = per.ObjectId;
                try
                {
                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        Polyline pline = (Polyline)trans.GetObject(objId, OpenMode.ForRead, false);
                        Point3d pickPt = pline.GetClosestPointTo(
                            per.PickedPoint.TransformBy(UCS),
                            ed.GetCurrentView().ViewDirection,
                            false);
                        double param = pline.GetParameterAtPoint(pickPt);
                        int index = (int)param;

                        Matrix3d OCS = Matrix3d.PlaneToWorld(pline.Normal);
                        Point3d transPt = pickPt.TransformBy(OCS);

                        if (!OCS.CoordinateSystem3d.Zaxis.IsEqualTo(UCS.CoordinateSystem3d.Zaxis))
                            ed.CurrentUserCoordinateSystem = PlineUCS(pline, index);

                        var aperture = (Int16)AcAp.GetSystemVariable("APERTURE");
                        double viewSize = (double)AcAp.GetSystemVariable("VIEWSIZE");
                        Point2d screenSize = (Point2d)AcAp.GetSystemVariable("SCREENSIZE");
                        double tol = 2 * aperture * viewSize / screenSize.Y;
                        Tolerance tolerance = new Tolerance(tol, tol);

                        int endParam = pline.Closed ?
                            pline.NumberOfVertices :
                            pline.NumberOfVertices - 1;
                        Vector3d vec;
                        using (Polyline ghost = new Polyline())
                        {
                            ghost.ColorIndex = 1;

                            if (!pline.Closed && pickPt.IsEqualTo(pline.GetPoint3dAt(0), tolerance))
                            {
                                vec = pline.GetFirstDerivative(0);
                                double bulge = pline.GetBulgeAt(0);
                                double width = pline.GetStartWidthAt(0);
                                Point2d p0 = transPt.GetPoint2d();
                                Point2d p1 = pline.GetPoint2dAt(0);

                                ghost.AddVertexAt(0, p0, bulge, width, width);
                                ghost.AddVertexAt(1, p1, bulge, width, width);
                                ghost.Normal = pline.Normal;
                                ghost.Elevation = pline.Elevation;
                                VertexJig jig = new VertexJig(ghost, pickPt, 0, vec, bulge,
                                    width, width);
                                PromptResult res = ed.Drag(jig);
                                if (res.Status == PromptStatus.OK)
                                {
                                    pline.UpgradeOpen();
                                    pline.AddVertexAt(index, ghost.GetPoint2dAt(0),
                                        ghost.GetBulgeAt(0), width, width);
                                }
                            }
                            else if (!pline.Closed && pickPt.IsEqualTo(pline.GetPoint3dAt(endParam),
                                tolerance))
                            {
                                vec = pline.GetFirstDerivative(endParam);
                                double bulge = pline.GetBulgeAt(index);
                                double width = pline.GetEndWidthAt(endParam);
                                Point2d p0 = pline.GetPoint2dAt(endParam);
                                Point2d p1 = new Point2d(transPt.X, transPt.Y);

                                ghost.AddVertexAt(0, p0, bulge, width, width);
                                ghost.AddVertexAt(1, p1, bulge, width, width);
                                ghost.Normal = pline.Normal;
                                ghost.Elevation = pline.Elevation;
                                VertexJig jig = new VertexJig(ghost, pickPt, 1, vec,
                                    bulge, width, width);

                                PromptResult res = ed.Drag(jig);
                                if (res.Status == PromptStatus.OK)
                                {
                                    pline.UpgradeOpen();
                                    pline.AddVertexAt(endParam + 1, ghost.GetPoint2dAt(1),
                                        ghost.GetBulgeAt(0), width, width);
                                    pline.SetBulgeAt(endParam, ghost.GetBulgeAt(0));
                                }
                            }
                            else
                            {
                                vec = pline.GetFirstDerivative(index);
                                double bulge = pline.GetBulgeAt(index);
                                double sWidth = pline.GetStartWidthAt(index);
                                double eWidth = pline.GetEndWidthAt(index);
                                Point2d p0 = pline.GetPoint2dAt(index);
                                Point2d p1 = transPt.GetPoint2d();
                                Point2d p2;
                                if (!pline.Closed)
                                    p2 = pline.GetPoint2dAt(index + 1);
                                else
                                {
                                    try { p2 = pline.GetPoint2dAt(index + 1); }
                                    catch { p2 = pline.GetPoint2dAt(0); }
                                }

                                FullSubentityPath subId = new FullSubentityPath(
                                    new ObjectId[] { pline.ObjectId },
                                    new SubentityId(SubentityType.Edge,
                                    new IntPtr((long)index + 1)));
                                pline.Highlight(subId, false);
                                ghost.AddVertexAt(0, p0, bulge, sWidth, 0.0);
                                ghost.AddVertexAt(1, p1, bulge, 0, eWidth);
                                ghost.AddVertexAt(2, p2, 0.0, 0.0, 0.0);
                                ghost.Normal = pline.Normal;
                                ghost.Elevation = pline.Elevation;
                                VertexJig jig = new VertexJig(ghost, pickPt, 1, vec,
                                    bulge, sWidth, eWidth);
                                PromptResult res = ed.Drag(jig);
                                if (res.Status == PromptStatus.OK)
                                {
                                    pline.UpgradeOpen();
                                    pline.SetStartWidthAt(index, ghost.GetStartWidthAt(1));
                                    pline.AddVertexAt(
                                        index + 1,
                                        ghost.GetPoint2dAt(1),
                                        ghost.GetBulgeAt(1),
                                        ghost.GetStartWidthAt(1),
                                        eWidth);
                                    pline.SetBulgeAt(index, ghost.GetBulgeAt(0));
                                }
                                pline.Unhighlight(subId, false);
                            }
                        }
                        trans.Commit();
                    }
                }
                catch(Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    ed.WriteMessage("\nError: " + ex.Message + ex.StackTrace);
                }
                finally
                {
                    ed.CurrentUserCoordinateSystem = UCS;
                }
            }
        }

        [CommandMethod("DVX")]
        public void Dvx()
        {
            bool loop = true;
            while (loop)
            {
                PromptEntityOptions peo = new PromptEntityOptions(
                    "\n Select a vertex to remove: ");
                peo.SetRejectMessage("\n Polyline only: ");
                peo.AllowNone = false;
                peo.AllowObjectOnLockedLayer = false;
                peo.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status == PromptStatus.Cancel)
                    loop = false;
                if (per.Status == PromptStatus.OK)
                {
                    ObjectId objId = per.ObjectId;
                    try
                    {
                        using (Transaction trans = db.TransactionManager.StartTransaction())
                        {
                            if (trans.GetObject(objId, OpenMode.ForWrite, false) is Polyline pline)
                            {
                                if (pline.NumberOfVertices > 2)
                                {
                                    Point3d pickPt = pline.GetClosestPointTo(
                                        per.PickedPoint.TransformBy(ed.CurrentUserCoordinateSystem),
                                        ed.GetCurrentView().ViewDirection,
                                        false);
                                    double param = pline.GetParameterAtPoint(pickPt);
                                    int index = (int)param;
                                    if (param - Math.Truncate(param) > 0.5)
                                        index++;
                                    pline.RemoveVertexAt(index);
                                }
                                else
                                {
                                    ed.WriteMessage("\nOnly two vertices left.");
                                }
                            }
                            trans.Commit();
                        }
                    }
                    catch(Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        ed.WriteMessage("\nError: " + ex.Message);
                    }
                }
            }
        }

        private Matrix3d PlineUCS(Polyline pline, int param)
        {
            Point3d origin = pline.GetPoint3dAt(param);
            Vector3d xDir = origin.GetVectorTo(pline.GetPoint3dAt(param + 1)).GetNormal();
            Vector3d zDir = pline.Normal;
            Vector3d yDir = zDir.CrossProduct(xDir).GetNormal();
            return Matrix3d.AlignCoordinateSystem(
                Point3d.Origin, 
                Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                origin,
                xDir, yDir, zDir);
        }

        class VertexJig : EntityJig
        {
            Polyline pline;
            Point3d point;
            int index;
            Vector3d vector;
            double bulge, sWidth, eWidth;
            public VertexJig(Polyline pline, Point3d point, int index,
                Vector3d vector, double bulge, double sWidth, double eWidth) : base(pline)
            {
                this.pline = pline;
                this.point = point;
                this.index = index;
                this.vector = vector;
                this.bulge = bulge;
                this.sWidth = sWidth;
                this.eWidth = eWidth;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                JigPromptPointOptions jppo = new JigPromptPointOptions(
                    "\nSpecify the new vertex: ");
                jppo.UserInputControls = UserInputControls.Accept3dCoordinates;
                var ppr = prompts.AcquirePoint(jppo);
                if (ppr.Status == PromptStatus.OK)
                {
                    if (ppr.Value.IsEqualTo(point))
                        return SamplerStatus.NoChange;
                    else
                    {
                        point = ppr.Value;
                        return SamplerStatus.OK;
                    }
                }
                return SamplerStatus.Cancel;
            }

            protected override bool Update()
            {
                if (pline.NumberOfVertices == 3)
                {
                    Point3d transPt = point.TransformBy(Matrix3d.WorldToPlane(pline.Normal));
                    Point2d pt = new Point2d(transPt.X, transPt.Y);
                    double length = pline.GetDistanceAtParameter(2);
                    double dist1 = pline.GetDistanceAtParameter(1);
                    double dist2 = length - dist1;
                    double width = sWidth < eWidth ?
                        (dist1 * (eWidth - sWidth) / length) + sWidth :
                        (dist2 * (sWidth - eWidth) / length) + eWidth;
                    double angle = Math.Atan(bulge);
                    pline.SetPointAt(index, pt);
                    pline.SetEndWidthAt(0, width);
                    pline.SetStartWidthAt(1, width);
                    pline.SetBulgeAt(0, Math.Tan(angle * dist1 / length));
                    pline.SetBulgeAt(1, Math.Tan(angle * dist2 / length));
                }
                else if(index == 0)
                {
                    Point3d transPt = point.TransformBy(Matrix3d.WorldToPlane(pline.Normal));
                    Point2d pt = new Point2d(transPt.X, transPt.Y);
                    pline.SetPointAt(index, pt);
                    if (bulge != 0.0)
                    {
                        Vector3d vec = point.GetVectorTo(pline.GetPoint3dAt(1));
                        double ang = vec.GetAngleTo(vector, pline.Normal);
                        pline.SetBulgeAt(0, Math.Tan(ang / 2.0));
                    }
                }
                else
                {
                    Point3d transPt = point.TransformBy(Matrix3d.WorldToPlane(pline.Normal));
                    Point2d pt = new Point2d(transPt.X, transPt.Y);
                    pline.SetPointAt(index, pt);
                    if (bulge != 0.0)
                    {
                        Vector3d vec = pline.GetPoint3dAt(0).GetVectorTo(point);
                        double ang = vector.GetAngleTo(vec, pline.Normal);
                        pline.SetBulgeAt(0, Math.Tan(ang / 2.0));
                    }
                }
                return true;
            }
        }
    }
}
