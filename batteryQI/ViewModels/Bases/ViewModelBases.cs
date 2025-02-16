using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using batteryQI.Models;
using batteryQI.Views;
using System.Windows.Controls;

namespace batteryQI.ViewModels.Bases
{
    // ObservableObject 클래스, [RelayCommand] 등 CommunityToolKit.Mvvm에서 제공하는 기능 사용시 원할한 사용을 위해 자동 코드 파일 생성
    // CommunityToolKit.Mvvm가 자동적으로 생성한 코드와 맞추기 위해 기능을 사용한 클래스는 partial로 수식 필수
    public partial class ViewModelBases : ObservableObject
    {
        // DB 객체 프로퍼티 선언.
        protected DBlink _dblink;
        public ViewModelBases()
        {
            // 객체 연결
            _dblink = DBlink.Instance(); 
        }
    }
}
