using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using batteryQI.Models;
using batteryQI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace batteryQI.ViewModels.Bases
{
    // 로그인 기능과 관련된 로직을 처리. 사용자가 로그인 창에서 입력한 정보를 토대로 DB와 상호작용하며 로그인 검증을 수행
    internal partial class LoginViewModel : ViewModelBases
    {
        // 로그인을 통해 관리자 초기화
        private Manager _manager = Manager.Instance();
        public Manager Manager
        {
            get => _manager;
            set => SetProperty(ref _manager, value);
        }

        public LoginViewModel()
        {
            // Manager 객체 생성
            _manager = Manager.Instance();
            // 로그인 창 열면서 DB 연결
            _dblink = DBlink.Instance();
            _dblink.Connect();
        }

        // 로그인 정보 검증과 처리 메소드
        // 다른 Command들과 달리 Windows.Window를 매개변수로 사용하지 않음
        // 다른 Command들은 UserControl이 Models의 프로퍼티와 Binding, UserControl에 직접 접근하지 않아도 해당 정보 획득 가능
        // 비밀번호는 보안상 Model에 프로퍼티 저장 X, 직접 UserControl에 접근해서 정보를 획득하고 검증해야함
        [RelayCommand] private void Login(PasswordBox pw) 
        {
            if (_dblink.ConnectOk())
            {
                // DB의 Manager 테이블에서 입력한 관리자 ID에 해당하는 데이터를 조회
                List<Dictionary<string, object>> login = _dblink.Select($"SELECT * FROM manager WHERE managerId='{Manager.ManagerID}';");
                
                // 관리자 ID && PW 일치 검증
                if (login.Count != 0 && (pw.Password == login[0]["managerPw"].ToString()))
                {
                    MessageBox.Show("로그인 완료", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    _manager.ManagerNum = (int)login[0]["managerNum"]; // 관리자 번호 저장
                    _manager.ManagerID = login[0]["managerId"].ToString(); // 관리자 아이디 저장
                    _manager.WorkAmount = (int)login[0]["workAmount"]; // DB에 저장된 작업량 가져옴

                    var mainWindow = new MainWindow();
                    mainWindow.Show();

                    // Windows.Window를 매개변수로 사용하지 않았기 때문에 직접 현재 로그인 창(첫 번째 창)에 접근
                    Application.Current.Windows[0]?.Close(); // 현재 창 닫기
                }
                else
                {
                    MessageBox.Show("아이디 및 비밀번호를 확인해 주세요", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
