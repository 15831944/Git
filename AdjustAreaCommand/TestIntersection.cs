using Autodesk.AutoCAD.ApplicationServices;
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
using GI = Autodesk.AutoCAD.GraphicsInterface;

[assembly: CommandClass(
  typeof(AdjustAreaCommand.TestIntersection)
)]

namespace AdjustAreaCommand
{
    public class TestIntersection
    {
        DBObjectCollection _markers = null;

        [CommandMethod("TEST_intersection")]
        public void TestMethod()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\nSelect a Polyline: ");
            peo.SetRejectMessage("\nNot a polyline");
            peo.AddAllowedClass(typeof(Polyline), true);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
                return;
            ObjectId plOid = per.ObjectId;

            PromptPointResult ppr = ed.GetPoint(
                new PromptPointOptions("\nPick an internal point: "));
            if (ppr.Status != PromptStatus.OK)
                return;
            Point3d testPoint = ppr.Value;
            PromptAngleOptions pao = new PromptAngleOptions("\nSpecify ray direction: ")
            {
                BasePoint = testPoint,
                UseBasePoint = true
            };
            var rayAngle = ed.GetAngle(pao);
            if (rayAngle.Status != PromptStatus.OK)
                return;
            Point3d tempPoint = testPoint.Add(Vector3d.XAxis);
            tempPoint = tempPoint.RotateBy(rayAngle.Value, Vector3d.ZAxis, testPoint);
            Vector3d rayDir = tempPoint - testPoint;

            ClearTransientGraphics();
            _markers = new DBObjectCollection();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                Curve plCurve = trans.GetObject(plOid, OpenMode.ForRead) as Curve;
                for (int cnt = 0; cnt < 2; cnt++)
                {
                    if (cnt == 1)
                        rayDir = rayDir.Negate();
                    using (Ray ray = new Ray())
                    {
                        ray.BasePoint = testPoint;
                        ray.UnitDir = rayDir;
                        Point3dCollection intersectionPts = new Point3dCollection();
                        plCurve.IntersectWith(ray, Intersect.OnBothOperands, intersectionPts,
                            IntPtr.Zero, IntPtr.Zero);
                        foreach(Point3d pt in intersectionPts)
                        {
                            Circle marker = new Circle(pt, Vector3d.ZAxis, 0.2);
                            _markers.Add(marker);
                            IntegerCollection col = new IntegerCollection();
                            GI.TransientManager.CurrentTransientManager.AddTransient(
                                marker,
                                GI.TransientDrawingMode.Highlight, 128, col);
                            ed.WriteMessage("\n" + pt.ToString());

                        }
                    }
                }
                trans.Commit();
            }
        }

        void ClearTransientGraphics()
        {
            GI.TransientManager tm = GI.TransientManager.CurrentTransientManager;
            IntegerCollection col = new IntegerCollection();
            if (_markers != null)
            {
                foreach(DBObject marker in _markers)
                {
                    tm.EraseTransient(marker, col);
                    marker.Dispose();
                }
            }
        }

        void WWW()
        {
           //ObjectSnapInfo osi;
        }
    }
}
