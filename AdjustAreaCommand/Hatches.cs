using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;

[assembly: CommandClass(
  typeof(AdjustAreaCommand.Commands)
)]

namespace AdjustAreaCommand
{
    public class Commands
    {
        static int _index = 1;

        [CommandMethod("TBH")]
        public void TraceBoundaryAndHatch()
        {
            Document doc =
              Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Select a seed point for our boundary

            PromptPointResult ppr =
              ed.GetPoint("\nSelect internal point: ");

            if (ppr.Status != PromptStatus.OK)
                return;

            // Get the objects making up our boundary

            DBObjectCollection objs =
              ed.TraceBoundary(ppr.Value, false);

            if (objs.Count > 0)
            {
                Transaction tr =
                  doc.TransactionManager.StartTransaction();
                using (tr)
                {
                    // We'll add the objects to the model space

                    BlockTable bt =
                      (BlockTable)tr.GetObject(
                        doc.Database.BlockTableId,
                        OpenMode.ForRead
                      );

                    BlockTableRecord btr =
                      (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite
                      );

                    // Add our boundary objects to the drawing and
                    // collect their ObjectIds for later use

                    ObjectIdCollection ids = new ObjectIdCollection();
                    foreach (DBObject obj in objs)
                    {
                        Entity ent = obj as Entity;
                        if (ent != null)
                        {
                            // Set our boundary objects to be of
                            // our auto-incremented colour index

                            ent.ColorIndex = _index;

                            // Set our transparency to 50% (=127)
                            // Alpha value is Truncate(255 * (100-n)/100)

                            ent.Transparency = new Transparency(127);

                            // Add each boundary object to the modelspace
                            // and add its ID to a collection

                            ids.Add(btr.AppendEntity(ent));
                            tr.AddNewlyCreatedDBObject(ent, true);
                        }
                    }

                    // Create our hatch

                    Hatch hat = new Hatch();

                    // Solid fill of our auto-incremented colour index

                    hat.SetHatchPattern(
                      HatchPatternType.PreDefined,
                      "SOLID"
                    );
                    hat.ColorIndex = _index++;

                    // Set our transparency to 50% (=127)
                    // Alpha value is Truncate(255 * (100-n)/100)

                    hat.Transparency = new Transparency(127);

                    // Add the hatch to the modelspace & transaction

                    ObjectId hatId = btr.AppendEntity(hat);
                    tr.AddNewlyCreatedDBObject(hat, true);

                    // Add the hatch loops and complete the hatch

                    hat.Associative = true;
                    hat.AppendLoop(
                      HatchLoopTypes.Default,
                      ids
                    );

                    hat.EvaluateHatch(true);

                    // Commit the transaction

                    tr.Commit();
                }
            }
        }
    }
}
