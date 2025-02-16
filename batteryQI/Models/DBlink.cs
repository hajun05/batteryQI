using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Google.Protobuf;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;

// MySQL 데이터베이스와의 연결 및 데이터 처리를 담당. 주로 데이터베이스 연결, 쿼리 실행, 결과 반환 등 데이터 액세스 계층(DAL) 역할을 수행
// DAL : Spring 웹 계층 구조의 일부. 웹 애플리케이션(여기선 해당 프로젝트 자체)와 DB간 상호작용 처리 계층
namespace batteryQI.Models
{
    #region DB select count(CountQuery 메소드. 정상과 불량(오염, 파손 등) 유형과 그에 해당하는 개수 조회, 결과 저장 용도
    public class CountResult
    {
        public List<object> defectGroups { get; set; } // DB에 저장된 배터리 불량 유형 저장 리스트
        public List<double> counts { get; set; } // 각 불량 유형에 속하는 배터리 개수를 저장하는 리스트

        public CountResult()
        {
            defectGroups = new List<object>();
            counts = new List<double>();
        }
    }
    #endregion

    #region MySQL 데이터베이스와의 연결 및 쿼리 실행을 위한 모든 기능을 제공
    public class DBlink : ObservableObject
    {
        // DB TCP/IP을 위한 필드 목록. WPF Binding이 불필요하여 프로퍼티 생략
        // TCP/IP : 인터넷 프로토콜 스위트(Internet Protocol Suite)는 인터넷에서 컴퓨터들이 서로 정보를 주고받는 데 쓰이는 통신규약(프로토콜). 
        private string _server = ""; // _server : ip 주소
        private string _port = ""; // _port : 포트번호. 클라이언트 프로그램이 네트워크 상의 특정 서버 프로그램을 지정하는 방법
        private string _dbName = ""; // _dbName : 연결 스키마
        private string _dbId = ""; // _dbId : 접속아이디
        private string _dbPw = ""; // _dbpw : 접속패스워드
        MySqlConnection connection; // DB connection 수행 객체

        // 싱글톤 패턴. DBlink 인스턴스를 한 번만 생성하여 재사용하도록 설계
        static DBlink staticDBlink; // DB 연결 객체, 클래스의 정적 인스턴스
        private DBlink() { } // 생성자 접근 제어 변경. 외부에서 직접 객체를 생성 방지
        public static DBlink Instance()
        {
            if(staticDBlink == null)
            {
                staticDBlink = new DBlink();
            }
            return staticDBlink;
        }
        // ----------- 메소드 목록

        // 데이터베이스 연결 설정
        private void setDBLink()
        {
            string relativePath = @".\Models\DB.txt"; // DB 서버에 대한 정보가 저장된 DB.txt 파일 (상대적)위치
            string fullPath = Path.Combine(Environment.CurrentDirectory, relativePath); // DB.txt 파일 접근 경로
            StreamReader sr = new StreamReader(fullPath); // DB.txt 파일 내용 읽기
            string[] Data = sr.ReadToEnd().Split("\n"); // 읽은 내용을 나누기
            _server = Data[0];
            _port = Data[1]; // 
            _dbName = Data[2];
            _dbId = Data[3];
            _dbPw = Data[4];
        }

        // 데이터베이스 연결 및 연결 확인
        public void Connect()
        {
            this.setDBLink(); // DB 연결 정보를 설정
            string myConnection =  // DB 연결에 사용할 사용할 명령문
                "Server="+_server + ";Port=" + _port + ";Database=" + _dbName + ";User Id = " + _dbId + ";Password = " + _dbPw + ";CharSet=utf8;";
            try
            {
                connection = new MySqlConnection(myConnection); // DB 연결 시도
                connection.Open(); // DB 오픈
            }
            catch(Exception E)
            {
                MessageBox.Show(E.ToString());
            }
        }

        // 데이터베이스 연결이 유지되고 있는지 확인
        public bool ConnectOk()
        {
            try
            {
                if (connection.Ping()) // DB 서버에 신호를 보내 연결 상태를 확인
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        // DB에 단일 행 데이터 삽입(insert)
        public bool Insert(string sql) // 매개변수로 전달받은 SQL 쿼리문
        {
            // 호출하는 쪽에서 작성한 SQL문이 가리키는 데이터를 DB의 특정 테이블에 삽입하는 역할
            // 일반적으로 1행씩 데이터 삽입(ex. 새로운 배터리 정보)
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, this.connection); // DB 조작(command)을 수행할 객체. SQL을 수행할 조작으로 등록
                if (cmd.ExecuteNonQuery() == 1) // ExecuteNonQuery() : SQL 실행, 실행에 영향을 받은 행 갯수 반환
                    return true;
                else
                    return false;
            }
            catch
            {
                return false; // db insert 에러
            }
        }

        // DB의 특정 테이블의 데이터 값을 다른 값으로 수정(update)
        public void Update(string sql) // 매개변수로 전달받은 SQL 쿼리문
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, this.connection); // DB 조작(command)을 수행할 객체. SQL을 수행할 조작으로 등록
                cmd.ExecuteNonQuery(); // ExecuteNonQuery() : SQL 실행, 실행에 영향을 받은 행 갯수 반환. 업데이트는 행 갯수 계산 불필요
            }
            catch
            {
                MessageBox.Show("데이터가 반영되지 않았습니다!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // DB의 데이터 테이블들을 조회하고 그 결과를 딕셔너리 형식(키 : 열(컬럼, 필드, 속성) - 값 : 데이터)으로 반환(select)
        public List<Dictionary<string, object>> Select(string sql) // 매개변수로 전달받은 SQL 쿼리문
        {
            // 간단한 Select문 메소드, 불러오는 데이터가 크면 그냥 직접 Select을 하는 것을 추천
            // 결과 저장 List. 각 행(row)을 딕셔너리로 변환, 열 이름을 키로, 값을 데이터로 저장
            List<Dictionary<string, object>> resultList = new List<Dictionary<string, object>>(); 
            try
            {
                using(MySqlCommand cmd = new MySqlCommand(sql, connection)) // DB 조작(command)을 수행할 객체. SQL을 수행할 조작으로 등록
                {
                    using(MySqlDataReader reader = cmd.ExecuteReader()) // DExecuteReader : DB에서 데이터를 받아오는 쿼리문 수행. MySqlDataReader 객체로 반환
                    {
                        while (reader.Read()) // 한 레코드(record, 튜플이라고도함. 행렬의 행에 해당)씩 데이터 받아오기
                        {
                            // reader.Read()로 읽어온 한 레코드(행). 한 행을 구성하는 열 목록
                            Dictionary<string, object> row = new Dictionary<string, object>();
                            // 데이터 필드에 따른 길이
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i); // 열(필드) 이름 반환
                                object value = reader.GetValue(i); // 열(필드) 데이터 반환
                                row[columnName] = value;
                            }
                            resultList.Add(row);
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("데이터베이스 접속 오류", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return resultList;
        }

        // 지정된 테이블에서 특정 컬럼(결함 여부)을 기준으로 그룹화(결함 데이터만 추출), 각 그룹에 속하는 행(row)의 수를 구함
        public CountResult? CountQuery(string table, string groupingCriteria, string mode = "label")
        {
            CountResult result = new CountResult(); // 배터리 결함 유형(그룹)과 각 그룹에 속하는 행의 개수 저장

            // mode == "label"(default) : 특정 컬럼을 기준으로 테이블의 데이터를 그룹화, 각 그룹의 행의 개수 계산
            string query = @$"
                        SELECT
	                        {groupingCriteria},
	                        Count(*)
                        FROM
	                        {table}
                        GROUP BY
	                        {groupingCriteria};";

            // mode == "timestamp" : 특정 컬럼을 시간대별(분:초 제외)로 포맷(서식화), 포맷된 시간을 기준으로 테이블의 데이터를 그룹화. 시간대별 결함 배터리 행의 개수 계산
            if (mode == "timestamp")
            {
                query = @$"
                            SELECT
                             DATE_FORMAT({groupingCriteria}, '%Y-%m-%d %H:00:00') AS hour_interval,
                             COUNT(*) AS count
                            FROM
                             {table}
                            WHERE 
                                defectStat = 1
                            GROUP BY
                             hour_interval
                            ORDER BY
                             hour_interval;";
            }

            MySqlCommand cmd = new MySqlCommand(query, this.connection); // 구성된 쿼리 명령문 실행
            MySqlDataReader reader = cmd.ExecuteReader(); // 실행 결과 읽어오기

            while (reader.Read())
            {
                result.defectGroups.Add(reader[0]); // 각 행의 첫 번째 컬럼(그룹의 식별자 또는 포맷된 시간)을 defectGroups 리스트에
                result.counts.Add(reader.GetDouble(1)); // 두 번째 컬럼(해당 그룹의 행 개수)을 counts 리스트에 추가
            }
            reader.Close();

            return result; // 최종적으로 각 그룹(결함 유형)과 해당 개수가 담긴 CountResult 객체를 반환

        }

        // CountQuery와 유사, 저장할 구조가 달라서 따로 선언
        public List<(string, string, int)> GroupCountQuery(string table, string groupingCriteria, string mode = "label")
        {
            // 첫 번째와 두 번째 값은 그룹에 관련된 문자열 정보, 세 번째 값은 개수(ex. batteryType, defectName, 개수)
            // 해당 프로젝트에선 특정 batteryType, defectName별 개수를 탐색하여 시각화하는데 사용
            List<(string, string, int)>result = new List<(string, string, int)>();

            // CountQuery의 default 쿼리와 동일.
            // 일반적으로 해당 쿼리문은 두 개의 컬럼(그룹 기준과 개수)을 반환, 이후 3가지 데이터 가져올때 런타임에러 위험
            // groupingCriteria이 하나의 컬럼명이 아니라 여러 컬럼을 포함하는 복합 그룹 기준("batteryType, defectName")이기 때문에 에러 방지
            string query = @$"
                        SELECT
	                        {groupingCriteria},
	                        Count(*)
                        FROM
	                        {table}
                        GROUP BY
	                        {groupingCriteria};";
            
            MySqlCommand cmd = new MySqlCommand(query, this.connection); // 쿼리 실행
            MySqlDataReader reader = cmd.ExecuteReader(); // 실행 결과 읽어오기

            while (reader.Read())
            {
                // 읽어온 3가지 항목 데이터 가져오기.
                result.Add((reader.GetString(0), reader.GetString(1), reader.GetInt16(2)));
                // reader.GetString(n) : n번째 컬럼의 값 반환
            }
            reader.Close();

            return result;
        }

        // DB 연결 종료
        public void Disconnect()
        {
            connection.Close(); // 연결 해제
        }
    }
    #endregion
}
