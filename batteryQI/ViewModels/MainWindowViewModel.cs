using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using batteryQI.Models;
using batteryQI.ViewModels.Bases;
using CommunityToolkit.Mvvm.Input;
using batteryQI.Views.UserControls;
using batteryQI.Views;
using System.Data.Common;

namespace batteryQI.ViewModels
{
    // 데시보드(메인 창)에서 페이지 전환과 팀원 목록(장식) 그리고 종료 동작을 처리. UI의 콘텐츠를 동적으로 전환
    internal partial class MainWindowViewModel : ViewModelBases
    {
        // 현재 표시될 페이지(View)를 담는 필드, 프로퍼티
        // 데시보드 전체가 아니라 내부 Frame과 페이지를 Binding하여 화면 전환 구현
        private object _currentPage; 
        public object CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        // 메인 창을 닫기 위한 대리자(delegate)를 저장, View에서 this.Close() 메소드 할당
        // 창 닫기 Command에 CommandParameter로 window를 설정하지 않았기 때문
        public Action? CloseAction { get; set; }

        public MainWindowViewModel()
        {
            // 초기 화면 설정
            _currentPage = new DashboardView();
        }
        
        [RelayCommand]
        private void HomeButton()
        {
            CurrentPage = new DashboardView();
        }
        
        [RelayCommand]
        private void ChartButton()
        {
            CurrentPage = new ChartView();
        }
        
        [RelayCommand]
        private void ManagerButton()
        {
            CurrentPage = new ManagerView();
        }

        [RelayCommand]
        private void ExitButton()
        {
            _dblink.Disconnect(); // DB 연결 끊기
            CloseAction?.Invoke(); // 대리자에 저장된 메소드 실행
        }
    }
}
