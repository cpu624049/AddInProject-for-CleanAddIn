using Autodesk.Revit.UI;


public class ApplyGraphicsOverrideHandler : IExternalEventHandler
{

    public bool IsResetColor { get; set; } = false;
    public int TransparencyValue { get; set; } = 0; // 기본값 0 (불투명)


    public Document Doc { get; set; }
    public Autodesk.Revit.DB.View View { get; set; }
    public List<ElementId> TargetElementIds { get; set; } = new List<ElementId>();
    public Autodesk.Revit.DB.Color ColorToApply { get; set; }



    public void Execute(UIApplication app)
    {
        using (Transaction tx = new Transaction(Doc, "Apply Graphics Override"))
        {
            tx.Start();

            if (IsResetColor)
            {
                foreach (ElementId id in TargetElementIds)
                {
                    View.SetElementOverrides(id, new OverrideGraphicSettings());
                }

                tx.Commit();
                return;
            }

            FillPatternElement solidFill = new FilteredElementCollector(Doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(f => f.GetFillPattern().IsSolidFill);

            if (solidFill == null)
            {
                tx.RollBack();
                return;
            }

            OverrideGraphicSettings ogs = new OverrideGraphicSettings();

            // 색상 설정 있을 때만 적용
            if (ColorToApply != null)
            {
                ogs.SetSurfaceForegroundPatternColor(ColorToApply);
                ogs.SetSurfaceForegroundPatternId(solidFill.Id);
            }

            // ✅ 투명도 설정 (최소 20 보장)
            int clampedTransparency = Math.Max(0, TransparencyValue);
            ogs.SetSurfaceTransparency(clampedTransparency);

            foreach (ElementId id in TargetElementIds)
            {
                View.SetElementOverrides(id, ogs);
            }

            tx.Commit();
        }
    }







    public string GetName() => "Apply Graphics Override Handler";
}
