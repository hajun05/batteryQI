using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using batteryQI.ViewModels.Bases;
using batteryQI.Views;
using CommunityToolkit.Mvvm.Input;
using Mysqlx;

namespace batteryQI.ViewModels
{
    // InspectViewModel 클래스의 이벤트 핸들러들을 모아둔 부분.
    // UI의 버튼 클릭 등 사용자 액션에 따라 실행되는 메서드들을 정의. 이미지 선택, 검사, 결과 처리 등 여러 기능을 담당.
    internal partial class InspectViewModel : ViewModelBases
    {
        // 이미지 파일 선택 다이얼로그를 열어 사용자가 이미지를 선택.
        // 그 경로를 기반으로 후속 이미지 처리 메소드(Battery.imgProcessing)에 전달
        [RelayCommand] // RelayCommand 어트리뷰트: CommunityToolkit.MVVM가 해당 어트리뷰트가 수식하는 메서드들을 커맨드(Command)로 자동 변환
        private void ImageSelectButton_Click()
        {
            // OpenFileDialog 객체를 생성, 파일 필터를 "Image Files|*.jpg;*.png;"로 지정해 JPG와 PNG 파일만 선택하도록 제한
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.png;";

            // 사용자가 파일 선택 후 확인(OK)을 누름
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _battery.ImagePath = openFileDialog.FileName; // 선택된 파일의 경로를 배터리 객체의 ImagePath 속성에 저장
            }
            // 배터리 객체 자체는 하나의 배터리 객체로써 계속 재활용, 내부 데이터만 업데이트
        }

        // 이미지 검사 수행 전에 필요한 입력 값들이 모두 채워졌는지 검증하고 이미지 처리 및 결과 창 띄움
        [RelayCommand] private void ImageInspectionButton_Click()
        {
            // 콤보박스에서 선택한 값들을 검증. 콤보박스에서 값 선택시 자동적으로 배터리 프로퍼티가 해당 값으로 초기화.
            // 콤보박스 값들 중 선택하지 않은 값 존재시 어느 값을 선택하지 않았는지 표기
            List<string> emptyFields = new List<string>();
            if (string.IsNullOrEmpty(battery.ImagePath)) emptyFields.Add("이미지");
            if (string.IsNullOrEmpty(battery.ManufacName)) emptyFields.Add("제조사명");
            if (string.IsNullOrEmpty(battery.BatteryShape)) emptyFields.Add("배터리 형태");
            if (string.IsNullOrEmpty(battery.BatteryType)) emptyFields.Add("배터리 타입");
            if (string.IsNullOrEmpty(battery.Usage)) emptyFields.Add("사용 용도");

            // 여러 필수 항목(이미지, 제조사명, 배터리 형태, 타입, 사용 용도)이 입력되어 있는지 확인
            //if (battery.ImagePath != "" && battery.ManufacName != "" && battery.BatteryShape != "" && battery.BatteryType != "" && battery.Usage != "")
            if (emptyFields.Count == 0) // 보다 간략화한 분기 조건.
            {
                battery.imgProcessing(); // ONNX 이미지 처리 작업 수행

                // 정상 불량 판단 페이지 출력, 검사 결과 출력
                var inspectionImage = new InspectionImage();
                inspectionImage.ShowDialog();
            }
            else
            {
                // 누락된 배터리 필수 항목 목록을 한 문자열로 통합
                string emptyFieldsMessage = string.Join(", ", emptyFields);
                // 누락된 정보를 사용자에게 메시지로 안내
                System.Windows.MessageBox.Show(
                    $"다음 정보를 기입해주세요: {emptyFieldsMessage}",
                    "입력 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // -------------------------------------- Inspection 결과 화면 이벤트 처리
        // 검사 결과가 정상인 경우의 처리를 담당. 정상 판정을 내렸을 때, 관련 정보를 사용자에게 전달하고 검사를 마무리
        [RelayCommand] private void NomalButton_Click(System.Windows.Window window)
        {
            // 배터리 객체의 DefectStat를 "정상", DefectName을 "Normal"로 설정
            battery.DefectStat = "정상";
            battery.DefectName = "Normal";

            // ErrorInfoView 창을 띄워 검사 결과 정보 출력
            var errorInfoView = new ErrorInfoView();
            errorInfoView.Show();

            window?.Close(); // 현재 창 닫기
        }

        // 검사 결과가 불량인 경우의 처리를 담당. 불량 판정 시, 불량 사유를 선택하도록 UI를 전환하여 사용자가 상세한 오류 정보를 입력할 수 있도록 유도
        [RelayCommand] private void ErrorButton_Click()
        {
            battery.DefectStat = "불량"; // 배터리 객체의 DefectStat를 "불량"으로 설정
            
            // 첫 번째 UserControl(에러 검사 창)은 숨기고, 두 번째 UserControl(불량 사유 선택 창)은 보이게 전환
            ErrorInspectionVisibility = Visibility.Collapsed;
            ErrorReasonVisibility = Visibility.Visible;
        }

        // ------------------------
        // ErrorInfo.xaml 이벤트 핸들링. 데이터 가용성(필요할 때 데이터에 접근 할 수 있는 정도)을 위해서 여기서 코딩

        // 불량 사유 입력이 필수임을 확인. 불량 사유를 선택한 후, 선택을 검증하고 다음 페이지로 이동
        [RelayCommand] private void confirmErrorReasonSelectButton_Click(System.Windows.Window window)
        {
            // 선택한 값이 null이거나 "불량 유형을 선택하세요"로 남아 있을 경우, 경고 메시지 박스를 띄워 사용자에게 입력을 요구
            if (string.IsNullOrEmpty(battery.DefectName) || battery.DefectName == "불량 유형을 선택하세요")
            {
                System.Windows.MessageBox.Show(
                    "불량 유형을 선택해주세요.",
                    "입력 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return; // 동작 중단
            }

            //정상적으로 불량 사유가 선택. ErrorInfoView 창을 열기
            var errorInfoView = new ErrorInfoView();
            errorInfoView.Show();

            // 현재 창 닫기
            window?.Close();
        }

        // 최종적으로 입력된 검사 정보를 DB에 저장, 검사 완료 후 초기화 및 다음 검사를 준비
        [RelayCommand] private void confirmErrorInfoCheckButton_Click(System.Windows.Window window)
        {
            // DB 정보 인서트
            if (_dblink.ConnectOk()) // 데이터베이스 연결이 정상인지 확인
            {
                int defectState = -1; // DB 배터리 데이터에서 불량 유무를 판단할 값. 문자열보다 정수형 값이 구분 편리
                if (battery.DefectStat == "정상")
                    defectState = 1;
                else
                    defectState = 0;

                // SQL INSERT 문을 실행하여 batteryInfo 테이블에 검사 정보를 저장
                if (_dblink.Insert($"INSERT INTO batteryInfo (shootDate, usageName, batteryType, manufacId, batteryShape, shootPlace, imagePath, managerNum, defectStat, defectName)" +
                    $"VALUES( '{_battery.ShootDate}', '{_battery.Usage}', '{_battery.BatteryType}', {ManufacDict[battery.ManufacName]}, '{_battery.BatteryShape}', 'CodingOn', NULL, {_manager.ManagerNum}, {defectState}, '{_battery.DefectName}');"))
                {
                    // 저장 성공 시, ManagerViewModel을 통해 전체 검사 건수를 업데이트
                    // 새 인스턴스의 선언, 기존에 사용중인 인스턴스와 동기화되지 않아 MVVM 패턴 위배 우려
                    ManagerViewModel managerViewModel = new ManagerViewModel(); 
                    _manager.TotalInspectNum = managerViewModel.completeAmount();
                    
                    System.Windows.MessageBox.Show("완료!");

                    // 데이터 초기화
                    _battery.Usage = "";
                    _battery.BatteryType = "";
                    _battery.ManufacName = "";
                    _battery.BatteryShape = "";
                    _battery.DefectName = "";
                    _battery.ImagePath = "";
                    _battery.BatteryBitmapImage = null; // bitmap 이미지 초기화
                }
                else
                    System.Windows.MessageBox.Show("실패");
            }
            window?.Close(); // 데이터 info 창 닫기
        }
    }
}
