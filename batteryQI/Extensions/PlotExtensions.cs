using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using batteryQI.ViewModels; // 확장할 대상. ChartView.xaml에 WpfPlot local:PlotExtensions.PlotData="{Binding Content}" 를 통해 확장
using System.Windows;
using ScottPlot; // WPF 그래프 네임스페이스

// WPF의 ScottPlot 그래프를 MVVM 패턴에서 데이터와 바인딩하기 위한 '의존(첨부) 프로퍼티'를 정의하는 역할.
namespace batteryQI.Extensions
{
    // 의존(Dependency) 프로퍼티 : 프로퍼티의 값이 변경되었을 때 자동으로 어떤 일을 처리할 수 있게 해주는 WPF의 특수한 프로퍼티.
    // 첨부(Attached) 프로퍼티 : 해당 프로퍼티를 정의한 클래스 이외의 다른 컨트롤에서도 프로퍼티 사용 가능. 일반적인 의존 프로퍼티보다 유연.
    // xaml코드에서 Button이나 TextBox에서 Grid.Row와 Grid.Column같은 프로퍼티를 사용할 수 있는것도 해당 프로퍼티가  Grid 컨트롤이 정의한 첨부 프로퍼티이기 때문
    // 첨부 프로퍼티는 특정 클래스(여기서는 PlotExtensions)에서 정의되지만, 그 속성을 어떤 다른 컨트롤(예, WpfPlot)에 "붙여서" 사용할 수 있게 하는 기능
    public static class PlotExtensions // 일반 의존(Dependency) 프로퍼티일 경우 WpfPlot 클래스 상속
    {
        // 첨부 프로퍼티 등록. 의존 프로퍼티의 경우에는 Register 메소드 사용.
        // WPF의 모든 DependencyObject를 상속받는 객체(예: WpfPlot)들이 PlotData 프로퍼티를 본래 지니고 있던 프로퍼티처럼 사용 가능
        public static readonly DependencyProperty PlotDataProperty = DependencyProperty.RegisterAttached
            (
                "PlotData", // 해당 프로퍼티 명을 첨부 프로퍼티 형태로 선언. 이 클래스에 해당 명칭의 프로퍼티가 선언된 것으로 간주
                typeof(object), // 첨부 프로퍼티 타입. 형식에 구예받지 않게 object형 사용
                typeof(PlotExtensions), // 첨부 프로퍼티가 정의될 클래스 명시. 첨부 프로퍼티이기 때문에 WPF의 다른 컨트롤에도 적용 가능
                new PropertyMetadata(null, OnPlotDataChanged) // 첨부 프로퍼티의 기본값과 콜백 메소드(이벤트) 설정
            ); 
        // 해당 첨부 프로퍼티의 값이 변경될 경우 자동으로 변경 통보, 등록된 콜백 메소드가 실행.

        // WPF XAML에서 PlotData 속성을 설정하고 가져오는 메서드.
        // DependencyObject(WPF UI 요소)에서 PlotData 값을 저장하거나 가져옴. 프로퍼티의 Get, Set 역할
        public static void SetPlotData(DependencyObject element, object value)
        {
            element.SetValue(PlotDataProperty, value);
        }
        public static object GetPlotData(DependencyObject element)
        {
            return element.GetValue(PlotDataProperty);
        }

        // 첨부 프로퍼티의 데이터 변경 감지 메소드.
        // 
        private static void OnPlotDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WpfPlot wpfPlot) // DependencyObject d가 WpfPlot(ScottPlot의 WPF 컨트롤)이라면 플롯(막대 그래프 등 데이터의 시각화)를 초기화
            {
                var data = e.NewValue; // 프로퍼티의 데이터가 변경된 이벤트 데이터 가져오기
                wpfPlot.Reset(); // 플롯 초기화. 
                //wpfPlot.Plot.Clear(); // 플롯에서 모든 플롯 가능 항목을 제거.
                //wpfPlot.Render();
                //wpfPlot.Plot.AxisAuto();

                // 첨부 프로퍼티가 어느 ViewModel에서 변경되었느냐에 따라 적절한 메소드를 호출.
                if (data is DefectTypePieViewModel DefectTypePieData)
                {
                    DefectTypePieData.ConfigureChart(wpfPlot.Plot);
                }
                else if (data is HourlyDefectChartViewModel HourlyDefectChartData)
                {
                    HourlyDefectChartData.ConfigureChart(wpfPlot.Plot);
                }
                else if (data is DefectTypeChartByCategoryViewModel DefectTypeChartByCategoryData)
                {
                    DefectTypeChartByCategoryData.ConfigureChart(wpfPlot.Plot);
                }

                wpfPlot.Refresh(); // 플롯(그래프) 갱신
            }
        }
    }

}
