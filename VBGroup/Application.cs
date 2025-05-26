using Nice3point.Revit.Toolkit.External;
using VBGroup.Commands;
using VBGroup.Commands._01_SonMultiDuplicateView;
using VBGroup.Commands._02_SonMultiExportNWC;
using VBGroup.Commands._03_SonDetailFilter;

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

        /// <summary>
        /// 리본, 패널, 버튼 생성 메서드입니다.
        /// </summary>
        private void CreateRibbon()
        {
            // 패널 생성 방법
            // var 패널이름 = Application.CreatePanel("패널 이름", "리본 탭 명");
            var panel1 = Application.CreatePanel("Clean 작업 테스트", "VBGroup");

            // 애드인 버튼 생성 방법
            // 패널이름.AddPushButton<커맨드명>("애드인 버튼명") // 커맨드명, 애드인 버튼명 설정
            //    .SetImage("/VBGroup;component/Resources/Icons/아이콘명") // 16픽셀 아이콘 경로
            //    .SetLargeImage("/VBGroup;component/Resources/Icons/A0_32x32.png") // 32픽셀 아이콘 경로
            //    .SetToolTip("이 애드인의 기능은 무엇입니다."); // 툴팁 설정

            panel1.AddPushButton<FormConnect20>("Multi Duplicate View Test")
                .SetImage("/VBGroup;component/Resources/Icons/A9_16x16.png")
                .SetLargeImage("/VBGroup;component/Resources/Icons/A9_32x32.png")
                .SetToolTip("다중 뷰 복제 테스트중입니다.");

            panel1.AddPushButton<FormConnect8>("Multi Export NWC Test")
                .SetImage("/VBGRoup;component/Resources/Icons/A10_16x16.png")
                .SetLargeImage("/VBGRoup;component/Resources/Icons/A10_32x32.png")
                .SetToolTip("NWC 내보내기 테스트중입니다.");

            panel1.AddPushButton<FormConnect3>("Detail Filter Test")
                .SetImage("/VBGroup;component/Resources/Icons/A12_16x16.png")
                .SetLargeImage("/VBGroup;component/Resources/Icons/A12_32x32.png")
                .SetToolTip("디테일 필터 테스트중입니다.");

            //var panel2 = Application.CreatePanel("Links", "VBGroup");

            //panel2.AddPushButton<CommandTest2>("3번 기능")
            //    .SetImage("/VBGroup;component/Resources/Icons/A2_16x16.png")
            //    .SetLargeImage("/VBGroup;component/Resources/Icons/A2_32x32.png")
            //    .SetToolTip("Line 실행 기능 111111");

            //panel2.AddPushButton<CommandTest3>("4번 기능")
            //    .SetImage("/VBGroup;component/Resources/Icons/A3_16x16.png")
            //    .SetLargeImage("/VBGroup;component/Resources/Icons/A3_32x32.png")
            //    .SetToolTip("Line 실행 기능 222222");


        }
    }
}