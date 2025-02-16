using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Data.Common;
using batteryQI.Models;
using System.Windows.Forms;
using Mysqlx.Crud;
using batteryQI.ViewModels.Bases;
using System.Windows;
using System.Data;
using System.Collections.ObjectModel;

namespace batteryQI.ViewModels
{
    // 관리자 페이지에서 필요한 데이터 관리 및 업데이트, 그리고 DB와의 상호작용을 담당
    internal partial class ManagerViewModel : ViewModelBases
    {
        private Manager _manager; // 로그인한 관리자의 정보를 관리
        private string _manufacName = ""; // 새 제조사 항목 추가시 사용
        private IDictionary<string, string> _manufacDict // DB에서 가져온 제조사 정보 저장(미사용)
            = new Dictionary<string, string>(); 
        private ObservableCollection<KeyValuePair<string, string>> _manufacCollection // ObservableCollection을 사용하면 컬렉션 변경 시 UI가 자동 갱신
            = new ObservableCollection<KeyValuePair<string, string>>();
        private int _newAmount; // 새 작업량 설정시 사용
        public IDictionary<string, string> ManufacDict
        {
            get => _manufacDict;
        }
        public string ManufacName
        {
            get => _manufacName;
            set => SetProperty(ref _manufacName, value);
        }
        public Manager Manager
        {
            get => _manager;
            set => SetProperty(ref _manager, value);
        }
        public ObservableCollection<KeyValuePair<string, string>> ManufacCollection
        {
            get => _manufacCollection;
            set => SetProperty(ref _manufacCollection, value);
        }
        public int NewAmount
        {
            get => _newAmount;
            set => SetProperty(ref _newAmount, value);
        }
        public ManagerViewModel()
        {
            _manager = Manager.Instance();

            _manager.TotalInspectNum = completeAmount();

            getManafactureNameID();
            _newAmount = _manager.WorkAmount;
        }

        // DB에서 manufacture 테이블의 모든 데이터를 가져온 후, 각 행의 제조사 이름과 ID를 추출하여 _manufacCollection에 추가
        private void getManafactureNameID()
        {
            _manufacCollection.Clear(); // 기존 데이터 제거

            List<Dictionary<string, object>> ManufactureList_Raw = _dblink.Select("SELECT * FROM manufacture order by manufacId ASC;");
            foreach (var row in ManufactureList_Raw)
            {
                string name = row["manufacName"].ToString();
                string id = row["manufacId"].ToString();

                // ObservableCollection 활용. UI에 바인딩된 콤보박스나 리스트가 자동으로 갱신
                _manufacCollection.Add(new KeyValuePair<string, string>(name, id));
            }
        }

        // 새 제조사 추가. 사용자가 입력한 제조사명을 DB에 등록
        [RelayCommand] private void ManufactInsert()
        {
            try
            {
                if (_dblink.ConnectOk()) // DB 연결이 정상인지 확인
                {
                    if(ManufacName != "")
                    {
                        _dblink.Insert($"INSERT INTO manufacture (manufacId, manufacName) VALUES(0, '{ManufacName}');");
                        _manufacDict.Clear();
                        getManafactureNameID();
                        System.Windows.MessageBox.Show("완료");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("제조사를 입력해주세요", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("입력 오류", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 월 검사 할당량 수정. 사용자가 수정한 월 검사 할당량(_newAmount)을 DB에 업데이트
        [RelayCommand] private void amountSaveButton_Click()
        {
            if (_dblink.ConnectOk())
            {
                if (System.Windows.MessageBox.Show($"할당량을 {_newAmount}로 변경할까요?", "warning", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _manager.WorkAmount = _newAmount;
                    _dblink.Update($"UPDATE manager SET workAmount={_manager.WorkAmount} WHERE managerId='{_manager.ManagerID}';");
                    System.Windows.MessageBox.Show($"할당량을 {_manager.WorkAmount}로 수정 완료!");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("DB 연결 오류", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 검사 완료 건수 조회. 현재 월의 검사 완료 건수를 DB에서 조회해 문자열 형태로 반환
        public string completeAmount()
        {
            try
            {
                // 분석 완료 개수 가져오기. 월 할당량이기 때문에 현재 년도, 월의 데이터만 쿼리.
                string query = @$"
                        SELECT COUNT(*) 
                        FROM batteryInfo
                        WHERE DATE_FORMAT(shootDate, '%Y-%m') = DATE_FORMAT(NOW(), '%Y-%m')
                        AND ManagerNum = {_manager.ManagerNum};
                        ";

                // 데이터베이스 연결 및 쿼리 실행
                var result = _dblink.Select(query);

                // 데이터가 있는 경우
                if (result != null && result.Count > 0)
                {
                    // 첫 번째 결과를 문자열로 변환
                    return result[0]["COUNT(*)"]?.ToString() ?? "0";
                }
                else
                {
                    return "0"; // 데이터가 없는 경우
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 처리
                System.Windows.MessageBox.Show($"작업량 데이터 가져오기 실패", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return "Error";
            }
        }
    }
}
