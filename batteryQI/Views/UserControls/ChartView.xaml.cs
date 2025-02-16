using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ScottPlot.WPF;
using ScottPlot;
using batteryQI.ViewModels;

namespace batteryQI.Views.UserControls
{
    /// <summary>
    /// Interaction logic for ChartPage.xaml
    /// </summary>
    public partial class ChartView : UserControl
    {
        public ChartView()
        {
            InitializeComponent();
            // 직접 코드-비하인드에서 새 ViewModel 인스턴스를 생성하여 DataContext에 할당하는 방식
            // 간단하지만, 뷰와 뷰모델 간의 결합도 상승, 단위 테스트 및 유지보수 어려움, 의존성 주입 제한 등 단점 존재 
            // ViewModelLocator 혹은 DI 컨테이너를 통해 MVVM 디자인 패턴의 핵심인 느슨한 결합(loose coupling)을 보다 효과적으로 구현
            this.DataContext = new TabControlViewModel();
        }
    }
}
