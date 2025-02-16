using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using batteryQI.ViewModels.Bases;
using batteryQI.Models;
using ScottPlot;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Diagnostics;
using ScottPlot.Ticks;
using System.Collections;
using MySqlX.XDevAPI.Common;
using System.Configuration;
using ScottPlot.Renderable;
using ScottPlot.Drawing.Colormaps;
using System.Drawing;

// MVVM 패턴에 기반하여 차트 데이터 생성, ScottPlot을 이용해 다양한 형태의 차트를 구성.
namespace batteryQI.ViewModels
{
    // 기본 차트 데이터. 파이, 시간대, 그룹 바 등 여러 파생 차트들의 기반 역할
    // CommunityToolKit.Mvvm의 기능을 사용하지 않아 partial 수식 생략
    public class ChartViewModel
    {
        protected DBlink _dblink = DBlink.Instance(); // DB 연결 사용
        public string[] Labels { get; protected set; } // 그룹 또는 범주에 대한 레이블
        public double[] Values { get; protected set; } // 각 레이블에 해당하는 값(ex. 개수)
        public DateTime[] TimeStamps { get; protected set; } // 타임스탬프 데이터(ex. 시간대별 데이터 집계)

        // 기본 생성자. 
        public ChartViewModel() 
        {
            // CountQuery 메소드로 "batteryInfo" 테이블에서 "defectName"을 그룹 기준으로 데이터를 집계
            var chartData = _dblink.CountQuery("batteryInfo", "defectName");
            // Nullable을 활용해 chartData가 null일경우 빈 배열, 아니면 "defectName" 유형에 속하는 배터리 개수 저장
            Values = chartData?.counts.ToArray() ?? Array.Empty<double>();
            // DB에 저장된 배터리 불량 유형 저장 리스트 저장.
            // Array.ConvertAll<입력 형식, 반환 형식>(변환할 배열, 변환할 형식) : 배열의 전체 요소들에 형식 변환 적용
            Labels = Array.ConvertAll<object, string>(chartData.defectGroups.ToArray(), x => x?.ToString() ?? string.Empty);

            //cs8602경고(null 역참조(참조한 값 접근) 위험)에 대한 해결책1, 2
            //Labels = Array.ConvertAll<object, string>(chartData.defectGroups.ToArray(), x => (x ?? "").ToString() ?? string.Empty);
            //Labels = Array.ConvertAll<object, string>(chartData.defectGroups.ToArray(), x => ((object)x)?.ToString() ?? string.Empty);
        }

        // 생성자 오버로딩. 데이터를 집계할 테이블과 그룹 기준 지정 가능.
        public ChartViewModel(string table, string groupingCriteria, string XAxis = "label")
        {
            // XAxis 값에 따라 쿼리 분기 : "groupbar" 인 경우 GroupCountQuery메소드, 그 외는 CountQuery메소드로 데이터 집계
            if (XAxis == "groupbar") 
            {
                // List<(string, string, int)> 형태의 튜플형 결과 반환, CountQuery와 달리 결과를 분해하여 프로퍼티 초기화 어려움
                // 기반 클래스 대신 파생 클래스(ex. DefectTypeChartByCategoryViewModel)에서 이 복합 데이터 구조를 별도로 처리
                var chartData = _dblink.GroupCountQuery(table, groupingCriteria, XAxis);
            }
            else 
            {
                var chartData = _dblink.CountQuery(table, groupingCriteria, XAxis);
                Values = chartData?.counts.ToArray() ?? Array.Empty<double>();

                if (XAxis == "label") // defectGroups를 문자열 배열(Labels)로 변환.
                {
                    Labels = Array.ConvertAll<object, string>(chartData.defectGroups.ToArray(), x => x?.ToString() ?? string.Empty);
                }
                else if (XAxis == "timestamp") // defectGroups를 DateTime 배열(TimeStamps)로 변환하여 시간 정보를 저장
                {
                    TimeStamps = Array.ConvertAll(chartData.defectGroups.ToArray(), x =>
                    {
                        if (x != null && DateTime.TryParse(x.ToString(), out DateTime result))
                        {
                            return result;
                        }
                        throw new FormatException("값이 DateTime형태로 Parsing할 수 없습니다");
                    });
                }
            }
        }

        // 차트 구성 메소드. 파생 클래스에서 오버리이딩
        public void ConfigureChart(Plot plot)
        {
            // 데이터 배열(Values, Labels, TimeStamps 또는 그룹화된 데이터)에 기반
            // ScottPlot의 다양한 차트 요소(파이, 바, scatter 등)를 추가하고, 스타일과 레이블, 축, 범례 등을 설정
        }
    }

    // -------------------- 파생 클래스들
    // 불량 유형을 파이 차트(원그래프) 형태로 구성하여 관리 
    public class DefectTypePieViewModel : ChartViewModel
    {
        // 기반 클래스(ChartViewModel)의 생성자가 선 호출되는 파생 클래스 생성자
        public DefectTypePieViewModel() { } // 자동적으로 기반 클래스의 기본 생성자 선호출
        public DefectTypePieViewModel(string table, string groupingCriteria) : base(table, groupingCriteria) { }

        // 파이 차트 그리는 방식 결정 메소드(오버라이딩)
        public void ConfigureChart(Plot plot)
        {
            #region 4.1.74
            // 데이터 검증. 각 Labels별 데이터 갯수(Values) 그래프 생성, 각 배열의 길이 일치 필수
            if (Values.Length == Labels.Length)
            {
                // 파이 차트 생성
                var pie = plot.AddPie(Values); // Values 배열에 담긴 데이터(ex. 각 불량 유형의 개수)를 이용해 파이 차트를 추가
                double total = Values.Sum(); // 전체 합계를 계산하여 각 슬라이스의 비율(퍼센트)을 구하는 데 사용
                pie.Size = .7; // 파이 차트의 크기를 70%로 설정(기본은 90%)

                // 라벨 텍스트에 슬라이스명 추가. 라벨 텍스트끼리 겹침이 너무 많아 주석처리됨
                //pie.SliceLabels = Enumerable.Range(0, Values.Length)
                //                   .Select(i => $"{Labels[i]}\r\n({Values[i] / total:P1})").ToArray();

                // 안쪽에 label 표시용
                //pie.SliceLabelPosition = 0.4; // 값이 클 수록 바깥쪽에 가깝게 표시

                // 각 라벨들의 속성 초기화
                pie.SliceLabelPosition = 0.6; // 라벨 위치. 슬라이스 라벨을 슬라이스 경계에서 어느 정도 떨어진 위치(0~1 사이의 값)로 지정
                pie.SliceLabelColors = pie.SliceFillColors; // 라벨 색상. 슬라이스의 채우기 색상과 동일하게 라벨 색상을 설정
                pie.SliceLabels = Enumerable.Range(0, Values.Length) // 라벨 텍스트 1. 각 슬라이스에 대해 해당 값의 비율을 퍼센트 형식(P1)으로 표시
                                  .Select(i => $"{Values[i] / total:P1}").ToArray();
                //pie.ShowPercentages = true; // 라벨 텍스트 2. 1. 버전의 퍼센트 계산을 자동적으로 처리
                //pie.ShowValues = true; // 라벨 텍스트 3. 각 슬라이스에 대한 값 자체 출력

                // 모든 라벨 형식 공통 항목
                pie.ShowLabels = true;
                pie.SliceFont.Size = 20; // label 글자 크기
                pie.LegendLabels = Enumerable.Range(0, Values.Length) // 범례(그래프 옆 설명란) 설정. 범례에 각 슬라이스의 레이블과 실제 값(개수)를 표시하도록 설정
                                   .Select(i => $"{Labels[i]} ({Values[i]})").ToArray();
                plot.Legend(); // 범례를 Plot에 추가
            }
            else
            {
                throw new ArgumentException("(Userdefined)Values와 Labels의 길이가 일치하지 않습니다.");
            }
            #endregion
        }
    }

    // 시간대별 불량 수를 표현하기 위한 차트 ViewModel (바 차트, 꺾은선 그래프 등)
    public class HourlyDefectChartViewModel : ChartViewModel
    {
        // 기반 클래스(ChartViewModel)의 생성자가 선 호출되는 파생 클래스 생성자
        public HourlyDefectChartViewModel() { } // 자동적으로 기반 클래스의 기본 생성자 선호출
        // 시간대별 불량 수 차트 생성이기 때문에 XAxis == "timestamp" 지정
        public HourlyDefectChartViewModel(string table, string groupingCriteria, string XAxis) : base(table, groupingCriteria, XAxis) { }

        // 시간대별 불량 수 데이터 차트 그리는 방식 결정(오버라이딩)
        public void ConfigureChart(Plot plot)
        {
            #region 4.1.74
            // DB에 저장된 최소와 최대 시간대 간의 시간 차이를 구한 뒤, 1시간 단위로 포인트 수를 계산
            // -> x축 : 시간(최대 - 최소) / y축 : 불량수 그래프. 1시간 마다 불량 수 표기
            TimeSpan timeDifference = TimeStamps.Max() - TimeStamps.Min();
            int pointCount = (int)timeDifference.TotalHours + 1;

            // x축 포지션(시간) 배열 생성. 시작 시간부터 1시간 단위로 증가하는 x축 위치를 배열에 저장
            DateTime start = TimeStamps.Min();
            double[] positions = new double[pointCount];
            for (int i = 0; i < pointCount; i++)
                positions[i] = start.AddHours(i).ToOADate(); // DateTime을 OADate 메소드로 double형식(OLE 자동화 날짜)으로 변환

            // 데이터 보간. SQL로 조회한 시간대와 해당 시간대의 불량 수 데이터를 딕셔너리로 매핑.
            Dictionary<double, double> valuesDict = new Dictionary<double, double>();
            for (int i = 0; i < TimeStamps.Length; i++)
            {
                valuesDict[TimeStamps[i].ToOADate()] = Values[i];
            }

            // 실제 그래프 값(f(x) = y)을 나타내는 배열 선언, 초기화
            double[] ValuesCompletedHour = new double[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                // positions[i] 시간대에 불량 수 데이터가 존재하는 경우 불량 수를 대입
                if (valuesDict.TryGetValue(positions[i], out double value))
                {
                    ValuesCompletedHour[i] = value;
                }
                // positions[i] 시간대에 불량 수 데이터가 존재하지 않는 경우(누락된 시간대) 0을 대입
                else
                {
                    ValuesCompletedHour[i] = 0;
                }
            }

            // 그래프 형태와 x축 tick(눈금) 라벨 설정
            #region bar 그래프 버전1 (쿼리 결과 데이터에 없는 시간대, 누락된 시간대를 제외한 버전)
            //Debug.WriteLine("값은" + $"{String.Join(",", Values.Select(v => v.ToString()).ToArray())}");
            //var bar = plot.AddBar(Values, TimeStamps.Select(t => t.ToOADate()).ToArray()); // 누락된 시간대 제외
            //bar.BarWidth = (1.0 / TimeStamps.Length) * .8;

            //// adjust axis limits so there is no padding below the bar graph
            //plot.SetAxisLimits(yMin: 0);
            //plot.Layout(right: 20); //// add room for the far right date tick
            #endregion
            #region bar 그래프 버전2 (시간대의 값이 0인 경우도 추가한 버전)
            // display the bar plot using a time axis
            //var bar = plot.AddBar(ValuesCompletedHour, positions); // 누락된 시간대 포함

            // indicate each bar width should be 1/바갯수 of a day then shrink sligtly to add spacing between bars
            //bar.BarWidth = (1.0 / pointCount) * .8;
            #endregion
            #region 꺾은선 그래프(시그널 플롯) 버전1 (하루 == 24시간 간격 라벨 설정)
            //// AddSignal(ys, sampleRate) : sampleRate는 시간 간격
            //var signalPlot = plot.AddSignal(ValuesCompletedHour, 24.0); // 하루(24시간) 간격 라벨 배치
            //// Set start date
            //signalPlot.OffsetX = TimeStamps.Min().ToOADate();
            #endregion
            #region 꺾은선 그래프 버전2 (불연속 시간 간격용, 0인 시간대 포함)
            var scatter = plot.AddScatter(positions, ValuesCompletedHour, lineWidth: 2);
            #endregion
            #region 꺾은선 그래프 버전3 (불연속 시간 간격용, 0인 시간대 제외)
            //var scatter = plot.AddScatter(TimeStamps.Select(t => t.ToOADate()).ToArray(), Values, lineWidth: 5);
            #endregion
            // 누락된 시간대(0인 시간대)를 제외했다고 해서 그 구간 자체가 생략되는건 아님. f(x) = 0 표기 여부 설정의 의미

            // 그래프 설정
            plot.XAxis.DateTimeFormat(true); //x축 포멧을 DateTime으로 설정
            plot.XAxis.TickLabelStyle(fontSize: 12); // 라벨 글씨 크기 설정. 직접 간격을 설정하지 않은 경우 글씨 크기, x축 길이 등에 따라 자동 라벨 배치
            #endregion
        }
    }

    // 여러 기준(예: 배터리 타입과 불량 유형)에 따른 불량 개수를 그룹화하여 바 차트로 표시
    public class DefectTypeChartByCategoryViewModel : ChartViewModel
    {
        // 그룹화된 데이터를 튜플 형태로 저장하는 리스트. 각 튜플은 배터리 타입과 불량 유형, 그리고 해당 그룹의 개수
        private List<(string BatteryType, string DefectName, int Count)> chartData_group = new List<(string, string, int)>();

        public DefectTypeChartByCategoryViewModel() { }
        public DefectTypeChartByCategoryViewModel(string table, string groupingCriteria) : base(table, groupingCriteria) { }
        public DefectTypeChartByCategoryViewModel(string table, string groupingCriteria, string XAxis) : base(table, groupingCriteria, XAxis)
        {
            // DBlink의 GroupCountQuery를 호출하여 데이터를 chartData_group에 저장. 여기서 groupingCriteria는 "배터리 타입, 불량 유형" 복합 그룹 기준명
            chartData_group = _dblink.GroupCountQuery(table, groupingCriteria, XAxis);
        }

        // (배터리 타입, 불량 유형)별 불량 수 데이터 차트 그리는 방식 결정(오버라이딩)
        public void ConfigureChart(Plot plot)
        {
            #region 단순 불량 유형에 따른 데이터 그룹화(단일 그룹화) - 레거시 코드
            //if (Values.Length == Labels.Length) // 예외 발생. XAxis = "groupbar"이기 때문에 Values 초기화 생략
            //{
            //    // 더 많은 조건 있을 경우 AddBarGroups 이용
            //    double[][] ValuesArray = Values.Select(d => new double[] { d }).ToArray();
            //    //Debug.WriteLine(ValuesArray.Select(d => d.ToString()));
            //    double[][] PostionArray = Enumerable.Range(1, Values.Length)
            //                                        .Select(i => new double[] { i }).ToArray();
            //    // 반복문을 통한 바 그래프 생성
            //    for (int i = 0; i < Values.Length; i++)
            //    {
            //        var bar = plot.AddBar(ValuesArray[i], PostionArray[i]);
            //        bar.Label = Labels[i];
            //    }
            //    // 그래프 축관련 설정
            //    plot.AxisAuto();
            //    plot.SetAxisLimits(yMin: 0);
            //    //plot.Frame(true);
            //    plot.XAxis2.Ticks(false);
            //    plot.YAxis2.Ticks(false);
            //    plot.XAxis.DateTimeFormat(false); //x축 포멧을 DateTime으로 설정
            //    plot.Legend(location: Alignment.UpperRight);
            //    // X축 레이블 설정
            //    plot.XAxis.ManualTickPositions(Enumerable.Range(0, Labels.Length).Select(i => (double)i).ToArray(), Labels);
            //    plot.XAxis.TickLabelStyle(rotation: 45);
            //}
            //else
            //{
            //    throw new ArgumentException("(Userdefined)Values와 Labels의 길이가 일치하지 않습니다.");
            //}
            #endregion

            #region 불량 유형뿐만 아니라 배터리 타입까지 기준으로 데이터 그룹화(복합 그룹화), 차트 생성
            #region 필요한 저장구조(복합 그룹화)로 변환해서 저장
            // 배터리 타입과 불량 유형명을 정렬하여 배열로 저장. 고유한 batteryType과 defectName 목록 생성.
            // 쿼리 순서 : 선택 -> 중복 제거 -> 정렬 -> 배열 변환
            var batteryTypes = chartData_group.Select(r => r.BatteryType).Distinct().OrderBy(bt => bt).ToArray();
            var defectNames = chartData_group.Select(r => r.DefectName).Distinct().OrderBy(dn => dn).ToArray();

            // 각 (배터리 타입, 불량 유형) 쌍에 해당하는 개수를 딕셔너리에 저장
            var counts = new Dictionary<(string, string), int>();
            foreach (var result in chartData_group)
            {
                counts[(result.BatteryType, result.DefectName)] = result.Count;
            }

            // AddBarGroups에 맞는 형식(한 항목당 여러 막대 그래프. 그래프들의 y값을 저장한 ys 매개변수 형식) 으로 데이터 준비
            double[][] valuesBySeries = new double[defectNames.Length][]; // [항목][막대]
            for (int i = 0; i < defectNames.Length; i++)
            {
                valuesBySeries[i] = batteryTypes.Select(bt =>
                    counts.TryGetValue((bt, defectNames[i]), out int count) ? count : 0
                ).Select(Convert.ToDouble).ToArray();
            }
            #endregion

            // ScottPlot의 AddBarGroups 메서드를 사용하여 그룹화된 바 차트를 생성
            var bars = plot.AddBarGroups(batteryTypes, defectNames, valuesBySeries, null);
            for (int i = 0; i < bars.Length; i++)
            {
                bars[i].ShowValuesAboveBars = true; // 각 바 위에 값을 표시하도록 설정
            }

            // x축 레이블 스타일, 범례 위치, 그리고 y축의 최소값을 설정하여 차트의 가독성과 외형을 조정
            plot.XAxis.TickLabelStyle(fontSize: 20, fontBold: true);
            plot.Legend(location: Alignment.UpperRight);
            plot.SetAxisLimits(yMin: 0);
            #endregion
        }
    }

    // 실제로 표시될 View 화면에 띄워질 탭(창 내부 서브 페이지) 명칭(Header)과 내용(차트 ViewModel)을 저장하는 구조(Tabitem)
    public class TabItemViewModel
    {
        public string Header { get; set; } // 탭 명칭
        public ChartViewModel Content { get; set; } // 탭 내용. ViewModel 클래스의 인스턴스 할당(업캐스팅 활용)
    }

    // 여러 차트 ViewModel을 묶어 한번에 View와 Binding하기 위한 클래스.
    public partial class TabControlViewModel : ViewModelBases
    {
        // View에 Binding할 Tab 정보 프로퍼티. 
        public ObservableCollection<TabItemViewModel> Tabs { get; } = new ObservableCollection<TabItemViewModel>();

        // 3개의 탭 항목 추가, 초기화. 
        public TabControlViewModel() // 이부분 생성자 인자 들억가는 것 따로 구조체 같은 것으로 필드해서 만들면 더 좋을듯
        {
            Tabs.Add(new TabItemViewModel { Header = "시간대별 불량수", Content = new HourlyDefectChartViewModel("batteryInfo", "shootDate", "timestamp") });
            Tabs.Add(new TabItemViewModel { Header = "불량유형", Content = new DefectTypePieViewModel() });
            Tabs.Add(new TabItemViewModel { Header = "기준별 불량유형", Content = new DefectTypeChartByCategoryViewModel("batteryInfo", "batteryType, defectName", "groupbar") });
        }
    }
}
