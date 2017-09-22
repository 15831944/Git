using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcGi = Autodesk.AutoCAD.GraphicsInterface;

[assembly: ExtensionApplication(
  typeof(AdjustAreaCommand.CustomOSnapApp))
]

namespace AdjustAreaCommand
{
    public class CustomOSnapApp : IExtensionApplication
    {
        private QuarterOsnapInfo _info = new QuarterOsnapInfo();
        private QuarterGlyph _glyph = new QuarterGlyph();
        private CustomObjectSnapMode _mode;
        public void Initialize()
        {
            _mode = new CustomObjectSnapMode("Quarter", "Quarter", "Quarter of length",
                _glyph);
            _mode.ApplyToEntityType(RXObject.GetClass(typeof(Polyline)),
                new AddObjectSnapInfo(_info.SnapInfoPolyline));
            _mode.ApplyToEntityType(RXObject.GetClass(typeof(Curve)),
                new AddObjectSnapInfo(_info.SnapInfoCurve));
            _mode.ApplyToEntityType(RXObject.GetClass(typeof(Entity)),
                new AddObjectSnapInfo(_info.SnapInfoEntity));
            CustomObjectSnapMode.Activate("_Quarter");
        }

        public void Terminate()
        {
            CustomObjectSnapMode.Deactivate("_Quarter");
        }
    }

    public class QuarterGlyph : AcGi.Glyph
    {
        private Point3d _pt;
        public override void SetLocation(Point3d point)
        {
            _pt = point;
        }

        protected override void SubViewportDraw(AcGi.ViewportDraw vd)
        {
            int glyphSize = CustomObjectSnapMode.GlyphSize;
            var glyphPixels = vd.Viewport.GetNumPixelsInUnitSquare(_pt);

            double glyphHeight = (glyphSize / glyphPixels.Y) * 1.1;
            string text = "¼";

            var dist = -glyphHeight / 2.0;
            var offset = new Vector3d(dist, dist, 0);
            var e2w = vd.Viewport.EyeToWorldTransform;
            var dir = Vector3d.XAxis.TransformBy(e2w);
            var pt = (_pt + offset).TransformBy(e2w);

            var style = new AcGi.TextStyle();
            var fd = new AcGi.FontDescriptor("txt.shx", false, false, 0, 0);
            style.Font = fd;
            style.TextSize = glyphHeight;

            vd.Geometry.Text(pt, vd.Viewport.ViewDirection, dir, text, false, style);
        }
    }

    public class QuarterOsnapInfo
    {
        public void SnapInfoEntity(ObjectSnapContext context, ObjectSnapInfo result)
        {
            // Nothing
        }

        public void SnapInfoCurve(ObjectSnapContext context, ObjectSnapInfo result)
        {
            var cv = context.PickedObject as Curve;
            if (cv == null)
                return;

            double startParam = cv.StartParam;
            double endParam = cv.EndParam;

            if (startParam == endParam)
                return;

            double param = startParam + ((endParam - startParam) * 0.25);
            var pt = cv.GetPointAtParameter(param);
            result.SnapPoints.Add(pt);

            param = startParam + ((endParam - startParam) * 0.75);
            pt = cv.GetPointAtParameter(param);
            result.SnapPoints.Add(pt);

            if (cv.Closed)
            {
                pt = cv.StartPoint;
                result.SnapPoints.Add(pt);
            }
        }

        public void SnapInfoPolyline(ObjectSnapContext context, ObjectSnapInfo result)
        {
            var pl = context.PickedObject as Polyline;
            if (pl == null)
                return;

            double plStartParam = pl.StartParam;
            double plEndParam = pl.EndParam;

            double startParam = plStartParam;
            double endParam = startParam + 1.0;

            while (endParam <= plEndParam)
            {
                double param = startParam + ((endParam - startParam) * 0.25);
                var pt = pl.GetPointAtParameter(param);
                result.SnapPoints.Add(pt);

                param = startParam + ((endParam - startParam) * 0.75);
                pt = pl.GetPointAtParameter(param);
                result.SnapPoints.Add(pt);

                startParam = endParam;
                endParam += 1.0;
            }
        }
    }
}
