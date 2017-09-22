// (C) Copyright 2002-2005 by Autodesk, Inc. 
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted, 
// provided that the above copyright notice appears in all copies and 
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting 
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
//
// Use, duplication, or disclosure by the U.S. Government is subject to 
// restrictions set forth in FAR 52.227-19 (Commercial Computer
// Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii)
// (Rights in Technical Data and Computer Software), as applicable.
//

using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GI = Autodesk.AutoCAD.GraphicsInterface;

[assembly: CommandClass(typeof(TransientSampleNet.TransientSample))]

namespace TransientSampleNet
{
    /// <summary>
    /// Summary description for ADSCommands.
    /// </summary>
    public class TransientSample : IExtensionApplication
    {
        static DBObjectCollection TGpoints = new DBObjectCollection();
        static ObjectId m_currentId = ObjectId.Null;
        static bool m_bAddTG = true;
        static int pointmode = 0;

        public TransientSample()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        //This function gets the 5 points on the passed line and adds those points
        //to passed point array (pointList)
        static public void BreakLine(ref Line line, ref Point3dCollection pointList)
        {
            LineSegment3d lineseg = new LineSegment3d(line.StartPoint, line.EndPoint);
            var pointArray = lineseg.GetSamplePoints(5);

            bool bReverse = false;
            //Whether to bReverse the reading of points from line, this case is valid
            //only if this function is called for line segment of polyline.
            if (pointList.Count > 0)
            {
                Point3d currentPoint = pointList[pointList.Count - 1];

                if (currentPoint != line.StartPoint)
                {
                    bReverse = true;
                }

            }
            int nLength = pointArray.Length;
            int nIndex = 0;

            if (bReverse == false)
            {
                while (nIndex < nLength)
                {
                    if (pointList.Contains(pointArray[nIndex].Point) == false)
                    {
                        pointList.Add(pointArray[nIndex].Point);

                        DBPoint point = new DBPoint(pointArray[nIndex].Point);
                        point.ColorIndex = 1;
                        point.SetDatabaseDefaults();
                        TGpoints.Add(point);
                    }
                    nIndex++;
                }
            }
            else
            {
                nIndex = nLength;
                while (nIndex > 0)
                {
                    nIndex = nIndex - 1;
                    if (pointList.Contains(pointArray[nIndex].Point) == false)
                    {
                        pointList.Add(pointArray[nIndex].Point);

                        DBPoint point = new DBPoint(pointArray[nIndex].Point);
                        point.ColorIndex = 1;
                        point.SetDatabaseDefaults();
                        TGpoints.Add(point);
                    }
                }
            }

        }

        //This function gets the 5 points on the passed arc and adds those points
        //to passed point array (pointList)
        static public void BreakArc(ref Arc arc, ref Point3dCollection pointList)
        {
            CircularArc3d arcseg = new CircularArc3d(arc.Center, arc.Normal, arc.Normal.GetPerpendicularVector(),
                    arc.Radius, arc.StartAngle, arc.EndAngle);

            var pointArray = arcseg.GetSamplePoints(5);

            bool bReverse = false;
            //Whether to bReverse the reading of points from line, this case is valid
            //only if this function is called for line segment of polyline.
            if (pointList.Count > 0)
            {
                Point3d currentPoint = pointList[pointList.Count - 1];

                if (currentPoint != pointArray[0].Point)
                {
                    bReverse = true;
                }

            }
            int nLength = pointArray.Length;
            int nIndex = 0;

            if (bReverse == false)
            {
                while (nIndex < nLength)
                {
                    if (pointList.Contains(pointArray[nIndex].Point) == false)
                    {
                        pointList.Add(pointArray[nIndex].Point);

                        DBPoint point = new DBPoint(pointArray[nIndex].Point);
                        point.ColorIndex = 1;
                        point.SetDatabaseDefaults();
                        TGpoints.Add(point);
                    }
                    nIndex++;
                }
            }
            else
            {
                nIndex = nLength;
                while (nIndex > 0)
                {
                    nIndex = nIndex - 1;
                    if (pointList.Contains(pointArray[nIndex].Point) == false)
                    {
                        pointList.Add(pointArray[nIndex].Point);

                        DBPoint point = new DBPoint(pointArray[nIndex].Point);
                        point.ColorIndex = 1;
                        point.SetDatabaseDefaults();
                        TGpoints.Add(point);
                    }
                }
            }

        }

        static public void BreakPolyLine(ref Polyline pline, ref Point3dCollection pointList)
        {

            DBObjectCollection collection = new DBObjectCollection();
            pline.Explode(collection);

            System.Collections.IEnumerator Enumerator = collection.GetEnumerator();

            while (Enumerator.MoveNext() == true)
            {
                DBObject dbObject = (DBObject)Enumerator.Current;

                if (Line.GetClass(typeof(Line)) == dbObject.GetRXClass())
                {
                    Line line = (Line)dbObject;
                    BreakLine(ref line, ref pointList);
                    line.Dispose();
                }
                else if (Arc.GetClass(typeof(Arc)) == dbObject.GetRXClass())
                {
                    Arc arc = (Arc)dbObject;
                    BreakArc(ref arc, ref pointList);
                    arc.Dispose();
                }
            }

        }

        // Define Command "AsdkCmd1"
        [CommandMethod("startTGNet")]
        static public void test() // This method can have any name
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            pointmode = Application.DocumentManager.MdiActiveDocument.Database.Pdmode;
            Application.DocumentManager.MdiActiveDocument.Database.Pdmode = 34;
            ed.PointMonitor += new PointMonitorEventHandler(ed_PointMonitor);

        }

        //dispose all the DBpoints
        static void DisposePointArray()
        {
            int nLength = TGpoints.Count;
            if (nLength > 0)
            {
                int nIndex = 1;
                while (nIndex < nLength)
                {
                    TGpoints[nIndex].Dispose();
                    nIndex++;
                }

                //erase the parent
                TGpoints[0].Dispose();
            }

            TGpoints.Clear();
        }

        //This function updates the points and color....
        //this function demonstrates the use of UpdateChildTransient & UpdateTransient
        static void modifyTGPoints()
        {
            if (m_bAddTG == true)
            {
                //add the points as Transient Graphics... 
                if (TGpoints.Count > 0)
                {
                    AddTransientGraphics();
                    m_bAddTG = false;
                }
            }
            else if (TGpoints.Count > 0)
            {
                DBPoint point1 = (DBPoint)TGpoints[0];
                int nColorIndex = point1.ColorIndex;

                if (nColorIndex == 10)
                {
                    point1.ColorIndex = 1;
                }
                else
                {
                    point1.ColorIndex = nColorIndex + 1;
                }

                int nLength = TGpoints.Count;
                int nIndex = 1;

                while (nIndex < nLength)
                {
                    point1 = (DBPoint)TGpoints[nIndex];
                    nColorIndex = point1.ColorIndex;

                    if (nColorIndex == 10)
                    {
                        point1.ColorIndex = 1;
                    }
                    else
                    {
                        point1.ColorIndex = nColorIndex + 1;
                    }
                    //TransientManager.CurrentTransientManager.UpdateChildTransient(TGpoints[nIndex], TGpoints[0]);
                    nIndex++;

                    //if (nIndex == 10)
                    //    break;
                }
                IntegerCollection colloection = new IntegerCollection();
                GI.TransientManager.CurrentTransientManager.UpdateTransient(TGpoints[0], colloection);
            }
        }

        //currently no entity is under the cursor, so no need to show any points
        //erase the Transient Graphics
        //this function demonstrates the use of EraseChildTransient & EraseTransient
        static void eraseTGPoints()
        {
            //No entity is below the cursor and hence remove(erase) the transient graphics
            if (m_bAddTG == false)
            {
                int nLength = TGpoints.Count;

                if (nLength > 0)
                {
                    //int nIndex = 1;
                    //while (nIndex < nLength)
                    //{
                    //    TransientManager.CurrentTransientManager.EraseChildTransient(TGpoints[nIndex], TGpoints[0]);
                    //    nIndex++;
                    //}

                    //erase the parent
                    IntegerCollection colloection = new IntegerCollection();
                    GI.TransientManager.CurrentTransientManager.EraseTransient(TGpoints[0], colloection);
                    m_bAddTG = true;
                }
            }
        }

        static void AddTransientGraphics()
        {
            int nLength = TGpoints.Count;
            int nIndex = 1;
            IntegerCollection colloection = new IntegerCollection();

            GI.TransientManager.CurrentTransientManager.AddTransient(TGpoints[0],
                                GI.TransientDrawingMode.DirectShortTerm, 128, colloection);

            while (nIndex < nLength)
            {
                GI.TransientManager.CurrentTransientManager.AddChildTransient(TGpoints[nIndex], TGpoints[0]);
                nIndex++;
            }
        }

        //This function is called from point Monitor recator call back "ed_PointMonitor".
        //This function has a object id as a parameter. Depending of value of passed
        //object id, this function adds or modifies Transient Graphics
        static void AddOrModifyTGPoints(ObjectId id)
        {
            //Passed entity is same as entity to which point list is present in TGpoints
            if (m_currentId == id)
            {
                modifyTGPoints();
                return;
            }


            eraseTGPoints();

            Point3dCollection pointList = new Point3dCollection();
            IntegerCollection colloection = new IntegerCollection();

            Transaction trans = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();
            try
            {
                m_currentId = id;


                //memory given to point array is still present and user has moved the mouse over
                //a new entity, means we need to recreate the point array, so delete the current 
                //point arry 
                DisposePointArray();


                // now open the entity below cursor for read
                Entity ent = (Entity)trans.GetObject(id, OpenMode.ForRead);
                // if ok

                //prepare the point list
                if (Line.GetClass(typeof(Line)) == ent.GetRXClass())
                {
                    Line line = (Line)ent;
                    BreakLine(ref line, ref pointList);
                }
                else if (Arc.GetClass(typeof(Arc)) == ent.GetRXClass())
                {
                    Arc arc = (Arc)ent;
                    BreakArc(ref arc, ref pointList);
                }
                else if (Polyline.GetClass(typeof(Polyline)) == ent.GetRXClass())
                {
                    Polyline pline = (Polyline)ent;
                    BreakPolyLine(ref pline, ref pointList);
                }

                trans.Commit();
            }
            finally
            {
                // close everything up
                trans.Dispose();
            }

            //create DBPoint array... DBPoints are used as "Transient entity"
            if (pointList.Count != 0)
            {
                AddTransientGraphics();
            }

        }

        //point Monitor callback
        static void ed_PointMonitor(object sender, PointMonitorEventArgs e)
        {
            FullSubentityPath[] entPaths = e.Context.GetPickedEntities();

            if (entPaths.Length > 0)
            {
                //entity present below cursor, so show Transient points
                FullSubentityPath entPath = entPaths[0];
                AddOrModifyTGPoints(entPath.GetObjectIds()[0]);
            }
            else
            {
                //No entity present below cursor, so erase Transient points
                eraseTGPoints();
            }
        }

        [CommandMethod("endTGNet")]
        static public void endTGNet()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Application.DocumentManager.MdiActiveDocument.Database.Pdmode = pointmode;
            ed.PointMonitor -= new PointMonitorEventHandler(ed_PointMonitor);

            m_currentId = ObjectId.Null;


            int nCount = TGpoints.Count;
            int nIndex = 1;

            if (nCount > 0)
            {
                while (nIndex < nCount)
                {
                    GI.TransientManager.CurrentTransientManager.EraseChildTransient(TGpoints[nIndex], TGpoints[0]);
                    TGpoints[nIndex].Dispose();
                    nIndex++;
                }

                IntegerCollection colloection = new IntegerCollection();
                GI.TransientManager.CurrentTransientManager.EraseTransient(TGpoints[0], colloection);
                TGpoints[0].Dispose();
                TGpoints.Clear();
                m_bAddTG = true;
            }
        }



        void IExtensionApplication.Initialize()
        {
        }

        void IExtensionApplication.Terminate()
        {
            if (m_bAddTG == false)
            {
                //showing Transient points, so erase the Transient
                if (TGpoints.Count > 0)
                {
                    IntegerCollection colloection = new IntegerCollection();
                    GI.TransientManager.CurrentTransientManager.EraseTransient(TGpoints[0], colloection);
                    m_bAddTG = true;
                }
            }

            if (TGpoints.Count > 0)
            {
                //remove TG
                int nLength = TGpoints.Count;
                if (nLength > 0)
                {
                    int nIndex = 1;
                    while (nIndex < nLength)
                    {
                        TGpoints[nIndex].Dispose();
                        nIndex++;
                    }

                    //erase the parent
                    TGpoints[0].Dispose();
                }
            }
        }

    }
}