using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using batteryQI.Views;
using System.Windows.Media.Imaging;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Forms;
using batteryQI.Models;
using batteryQI.Views.UserControls;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using batteryQI.ViewModels.Bases;
using System.Data.Common;

namespace batteryQI.ViewModels
{
    // 이미지 검사와 관련된 이벤트를 관리하는 ViewModel, MVVM 패턴을 기반으로 UI와 데이터 로직을 연결하는 역할
    // 코드가 방대해 InspectViewModel(DB 데이터 가져오기), ""Ilist(필드와 프로퍼티 목록), ""Event(이벤트 처리) 세 파일로 분리해서 구현 
    internal partial class InspectViewModel : ViewModelBases
    {
        // DB에서 제조사(manufacture) 정보를 읽어와 제조사 이름과 ID를 저장
        private void getManafactureNameID()
        {
            // 제조사 리스트<제조사<제조사명, ID>> 초기화
            List<Dictionary<string, object>> ManufactureList_Raw = _dblink.Select("SELECT * FROM manufacture;"); // 데이터 가져오기
            ManufacDict.Clear(); // 기존에 저장된 제조사 정보가 있다면 새 정보와 충돌하지 않도록 초기화
            
            for(int i = 0; i < ManufactureList_Raw.Count; i++) // 반복문을 통한 데이터 추출
            {
                string Name = "";
                string ID = "";

                // 제조사 리스트에서 한 제조사에 대한 1행의 정보를 제조사명 열과 제조사 ID 열 별로 분할, 저장
                foreach(KeyValuePair<string, object> items in ManufactureList_Raw[i])
                {
                    // 제조사 이름 key, 제조사 id value
                    if(items.Key == "manufacName")
                    {
                        Name = items.Value.ToString();
                    }
                    else if(items.Key == "manufacId")
                    {
                        ID = items.Value.ToString(); 
                    }
                }
                if (!ManufacDict.ContainsKey(Name)) // 제조사 정보 중복 방지
                {
                    ManufacDict.Add(Name, ID);
                }
                _manufacList.Add(Name);
            }
        }

        // DB에서 batteryInfo 테이블의 '다음'(아직 DB에 추가되지 않은 행) AUTO_INCREMENT 값을 조회하여 반환
        // AUTO_INCREMENT : AUTO_INCREMENT를 설정한 컬럼(열)은 새 레코드(행)가 삽입될 때마다 해당 행에 1씩 증가한 고유 번호가 자동으로 할당
        // 각 행에 대해 고유하고 자동 증가하는 식별자를 제공, 각 행을 식별하는데 중요한 역할
        private string GetNextAutoIncrementId()
        {
            try
            {
                // AUTO_INCREMENT 값을 가져올 테이블(batteryInfo)에 접근, 값을 가져오는 쿼리문
                string query = @"
                    SELECT AUTO_INCREMENT
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = 'batteryQI' 
                    AND TABLE_NAME = 'batteryInfo';
                ";
                // INFORMATION_SCHEMA : MySQL서버가 운영하는 모든 다른 데이터베이스에 대한 정보를 저장하는 장소

                var result = _dblink.Select(query); // 데이터베이스 연결 및 쿼리 실행

                if (result.Count > 0) // DB에서 AUTO_INCREMENT를 가져온 경우. result.Count == 1
                {
                    return result[0]["AUTO_INCREMENT"].ToString(); // AUTO_INCREMENT 값을 문자열로 반환
                }
                else // 데이터가 없는 경우
                {
                    return "알 수 없음";
                }
            }
            catch (Exception ex) // 데이터베이스 접근이나 쿼리 실행 중 발생 예외 처리
            {
                System.Windows.MessageBox.Show($"Battery ID 가져오기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return "오류";
            }
        }
    }
}
