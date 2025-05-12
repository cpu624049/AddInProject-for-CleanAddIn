using Nice3point.Revit.Toolkit.External;
using VBGroup.Commands;

namespace VBGroup
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateRibbon();
        }

        private void CreateRibbon()
        {
            var panel1 = Application.CreatePanel("Commands", "VBGroup");

            panel1.AddPushButton<StartupCommand>("1번 기능")
                .SetImage("/VBGroup;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/VBGroup;component/Resources/Icons/RibbonIcon32.png")
                .SetToolTip("Command 실행 기능 111111");

            panel1.AddPushButton<CommandTest1>("2번 기능")
                .SetImage("/VBGRoup;component/Resources/Icons/A1_16x16.png")
                .SetLargeImage("/VBGRoup;component/Resources/Icons/A1_32x32.png")
                .SetToolTip("Command 실행 기능 222222");

            var panel2 = Application.CreatePanel("Links", "VBGroup");

            panel2.AddPushButton<CommandTest2>("3번 기능")
                .SetImage("/VBGroup;component/Resources/Icons/A2_16x16.png")
                .SetLargeImage("/VBGroup;component/Resources/Icons/A2_32x32.png")
                .SetToolTip("Line 실행 기능 111111");
            
            panel2.AddPushButton<CommandTest3>("4번 기능")
                .SetImage("/VBGroup;component/Resources/Icons/A3_16x16.png")
                .SetLargeImage("/VBGroup;component/Resources/Icons/A3_32x32.png")
                .SetToolTip("Line 실행 기능 222222");
        }
    }
}