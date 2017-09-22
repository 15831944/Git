using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
//using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

[assembly: CommandClass(
  typeof(AdjustAreaCommand.CCC)
)]

namespace AdjustAreaCommand
{
    class ArxImports
    {
        public struct Ads_name
        {
            public IntPtr a;
            public IntPtr b;
        };

        [StructLayout(LayoutKind.Sequential, Size = 32)]
        public struct Resbuf { }

        [DllImport("accore.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            ExactSpelling = true)]
        public static extern PromptStatus acedSSGet(
            string str, IntPtr pt1, IntPtr pt2,
            IntPtr filter, out Ads_name ss);

        [DllImport("accore.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            ExactSpelling = true)]
        public static extern PromptStatus acedSSFree(
            ref Ads_name ss);

        [DllImport("accore.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            ExactSpelling = true)]
        public static extern PromptStatus acedSSLength(
            ref Ads_name ss, out int len);

        [DllImport("accore.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            ExactSpelling = true)]
        public static extern PromptStatus acedSSName(
            ref Ads_name ss, int i, out Ads_name name);

        [DllImport("acdb19.dll",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            ExactSpelling = true)]
        public static extern ErrorStatus acdbGetObjectId(
            out ObjectId id, ref Ads_name name);
    }
    
    public class CCC
    {

        static List<ObjectId>
            FindAtPoint(Point3d worldPoint, bool selectAll = true)
        {
            List<ObjectId> ids = new List<ObjectId>();

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Matrix3d wcs2ucs =
                doc.Editor.CurrentUserCoordinateSystem.Inverse();

            Point3d ucsPoint = worldPoint.TransformBy(wcs2ucs);

            string arg = selectAll ? ":E" : string.Empty;

            IntPtr ptrPoint = Marshal.UnsafeAddrOfPinnedArrayElement(
                worldPoint.ToArray(), 0);


            PromptStatus prGetResult = ArxImports.acedSSGet(
                arg, ptrPoint, IntPtr.Zero, IntPtr.Zero, out ArxImports.Ads_name sset);

            ArxImports.acedSSLength(ref sset, out int len);

            if (len <= 0)
                return ids;

            for (int i = 0; i < len; ++i)
            {

                if (ArxImports.acedSSName(
                    ref sset, i, out ArxImports.Ads_name name) != PromptStatus.OK)
                    continue;


                if (ArxImports.acdbGetObjectId(
                    out ObjectId id, ref name) != ErrorStatus.OK)
                    continue;

                ids.Add(id);
            }

            ArxImports.acedSSFree(ref sset);

            return ids;
        }

        Editor AdnEditor;

        [CommandMethod("PointMonitorSelection")]
        public void PointMonitorSelection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                AdnEditor = doc.Editor;

                AdnEditor.PointMonitor +=
                    FindUsingPointMonitor;

                PromptEntityOptions peo = new PromptEntityOptions(
                    "Select an entity...");

                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                    return;

                ObjectId id = per.ObjectId;

                ed.WriteMessage("\n - Selected " +
                    " Entity: " + id.ObjectClass.Name +
                    " Id: " + id.ToString());
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nException: " + ex.Message);
            }
            finally
            {
                AdnEditor.PointMonitor -=
                    FindUsingPointMonitor;
            }
        }

        void FindUsingPointMonitor(object sender, PointMonitorEventArgs e)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Not working when running editor selection
            //foreach (var subId in e.Context.GetPickedEntities())
            //{
            //    foreach (var id in subId.GetObjectIds())
            //    {
            //        ed.WriteMessage("\n - " +
            //            " Entity: " + id.ObjectClass.Name +
            //            " Id: " + id.ToString());
            //    }
            //}

            var ids = FindAtPoint(e.Context.RawPoint);

            foreach (var id in ids)
            {
                ed.WriteMessage("\n + " +
                    " Entity: " + id.ObjectClass.Name +
                    " Id: " + id.ToString());
            }
        }
    }
}
