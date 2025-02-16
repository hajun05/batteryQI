using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using batteryQI.Models;
using System.Windows;
using batteryQI.ViewModels.Bases;

namespace batteryQI.ViewModels
{
    // InspectViewModel의 필드 및 프로퍼티 리스트
    internal partial class InspectViewModel : ViewModelBases
    {
        // combox 리스트
        // 컬렉션 클래스의 기반 인터페이스 I컬렉션 형식으로 업캐스팅. 유연성과 확장성 향상
        private IList<string> _manufacList = new List<string>(); // 제조사 이름을 저장. DB에서 제조사 데이터를 불러와서 콤보박스에 표시
        private IDictionary<string, string> ManufacDict = new Dictionary<string, string>(); // 제조사 이름과 해당 ID를 매핑(ID 미사용)
        private IList<string>? _batteryTypeList = new List<string>() { "Cell", "Module", "Pack" }; // "Cell", "Module", "Pack" 등 배터리 타입 정보 저장
        private IList<string>? _batteryShapeList = new List<string>() { "Pouch", "Cylinder" }; // "Pouch", "Cylinder" 등 배터리 모양 정보 저장
        private IList<string>? _usageList = new List<string>() { "Household", "Industrial" }; // "Household", "Industrial" 등 배터리 사용처 정보 저장
        private IList<string>? _defectList = new List<string>() // "Damage", "Pollution", "Damage and Pollution", "Etc.." 등 불량 유형 정보 저장
        { "Damage", "Pollution", "Damage and Pollution", "Etc.." }; 

        // 싱글톤 패턴으로 구현된 배터리, 관리자 객체
        private Battery _battery = Battery.Instance();
        private Manager _manager = Manager.Instance();

        // System.Windows.Visibility : Windows 창의 표시 여부를 결정하는 필드
        private Visibility _errorInspectionVisibility = Visibility.Visible; // 첫 번째 UserControl (ErrorInspection) Visibility 제어
        private Visibility _errorReasonVisibility = Visibility.Collapsed; // 두 번째 UserControl (ErrorReason) Visibility 제어

        // 일기 전용 프로퍼티. Set을 설정하지 않아 값 변경 불가, 자연히 UI에 변경 알림 X
        public IList<string>? ManufacList
        {
            get => _manufacList;
        }
        public IList<string>? BatteryTypeList
        {
            get => _batteryTypeList;
        }
        public IList<string>? BatteryShapeList
        {
            get => _batteryShapeList;
        }
        public IList<string>? UsageList
        {
            get => _usageList;
        }
        public IList<string>? DefectList
        {
            get => _defectList;
        }
        // 데이터 바인딩이나 값 변경 알림(INotifyPropertyChanged)이 가능
        public Battery battery
        {
            get => _battery;
            set => SetProperty(ref _battery, value);
        }
        public Visibility ErrorInspectionVisibility
        {
            get => _errorInspectionVisibility;
            set => SetProperty(ref _errorInspectionVisibility, value);
        }
        public Visibility ErrorReasonVisibility
        {
            get => _errorReasonVisibility;
            set => SetProperty(ref _errorReasonVisibility, value);
        }

        // 생성자. 데시보드, 검사창 등에 활용할 배터리 정보들을 DB에서 가져와 초기화
        public InspectViewModel()
        {
            // AUTO_INCREMENT로 배터리 ID 생성
            // 즉, DB에서 배터리 데이터가 선언된 순서대로 배터리의 ID 초기화
            _battery.BatteryID = GetNextAutoIncrementId();
            // DB에서 제조사 데이터를 가져와 _manufacList와 ManufacDict을 초기화
            getManafactureNameID();
        }

    }
}
