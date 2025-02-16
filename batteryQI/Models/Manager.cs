using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;

// 해당 프로젝트를 실사용 중인 관리자의 정보를 저장하고, 작업(검사) 진행률을 계산하여 관리
namespace batteryQI.Models
{
    // 싱글톤 패턴
    internal class Manager : ObservableObject
    {
        private int _managerNum; // 담당자의 번호
        private string _managerID; // 담당자 아이디
        private string _managerPW; // 담당자 비번
        private int _workAmount; // 담당자 할당량
        private string _totalInspectNum; // 오늘 수행량 저장 변수
        private double _workProgress; // 검사 완료 비율

        // 싱글톤 패턴 적용
        static Manager manager; // singleton
        private Manager() { } // 생성자 접근 제어 변경
        public static Manager Instance()
        {
            if(manager == null)
            {
                manager = new Manager(); // Manager 객체 생성
            }
            return manager;
        }

        // 공개 프로퍼티 목록. SetProperty : 프로퍼티 값이 변경될 때 자동으로 PropertyChanged 이벤트를 발생, 자동 UI 갱신
        public string ManagerID
        {
            get { return _managerID; }
            set
            {
                SetProperty(ref _managerID, value);
            }
        }
        public int ManagerNum
        {
            get { return _managerNum; }
            set
            {
                SetProperty(ref _managerNum, value);
            }
        }
        public string ManagerPW
        {
            get { return _managerPW; }
            set
            {
                SetProperty(ref _managerPW, value);
            }
        }
        public int WorkAmount
        {
            get { return _workAmount; }
            set
            {
                SetProperty(ref _workAmount, value);
                UpdateWorkProgress(); // 할당량 변경 시 진행률 업데이트
            }
        }
        public string TotalInspectNum
        {
            get { return _totalInspectNum; }
            set
            {
                SetProperty(ref _totalInspectNum, value);
                UpdateWorkProgress(); // 검사 완료량 변경 시 진행률 업데이트
            }
        }
        public double WorkProgress
        {
            get => _workProgress;
            private set // 외부에서는 읽기만 가능. 
            {
                _workProgress = value;
                OnPropertyChanged(nameof(WorkProgress)); // 내부 로직을 통해서만 쓰기 실행, UI 갱신
            }
        }

        // 작업 진행률 계산 메소드. WorkAmount(할당량)와 TotalInspectNum(실제 검사 건수)을 바탕으로 작업 진행률(WorkProgress)을 계산
        private void UpdateWorkProgress()
        {
            try
            {
                // TotalInspectNum을 정수로 변환
                int totalInspectNum = int.TryParse(TotalInspectNum, out var parsedValue) ? parsedValue : 0;

                if (WorkAmount > 0)
                {
                    // 작업 진행률 %를 소수점 둘째 자리까지 반올림
                    WorkProgress = Math.Round((double)totalInspectNum / WorkAmount * 100, 2);
                }
                else
                {
                    WorkProgress = 0; // 할당량이 0일 경우 진행률은 0
                }
                if (WorkProgress > 100) WorkProgress = 100; // 최대치 제한
            }
            catch (Exception ex)
            {
                MessageBox.Show($"작업 진행률 계산 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
