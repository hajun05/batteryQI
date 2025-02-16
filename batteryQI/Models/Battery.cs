using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Google.Protobuf.WellKnownTypes;
using System.Windows.Forms;
using System.Drawing;
//using SkiaSharp; // 아마 ScottPlot5 쓸데 있던 것 같은데 왜 여기에 있고, 남아있는지는 모르겠음
using System.IO;
//using Microsoft.ML.O

// WPF 애플리케이션에서 사용되는 배터리 관련 모델 클래스를 정의. 배터리의 여러 속성을 관리하고, 이미지 처리(ONNX 모델을 이용한 AI 기반 이미지 분석)를 수행
namespace batteryQI.Models
{
    // ObservableObject : CommunityToolkit.Mvvm 패키지에서 제공, 프로퍼티 데이터 변경시 자동 알림 전달, UI에 반영
    internal class Battery : ObservableObject
    {
        // 배터리 가용 변수 선언(변동사항 있음. Id나 date 데이터 등은 DB에 기입할 때 필요하지만 관리자가 건들이는 부분은 없음
        private string _usage = ""; // 사용처, 직접 기입
        private string _batteryType = ""; // 배터리 타입, 직접 기입
        private string _manufacName = ""; // 제조사명, 직접 기입
        private string _batteryShape = ""; // 배터리 모양, 직접 기입
        private string _shootPlace = "line1"; // 촬영 장소, 자동 기입(일단은)
        private string? _imagePath = ""; // 파일 로드 경로 저장 -> 이미지 로드
        private string _defectStat; // 불량 유무, 직접 기입(불량, 정상 선택으로)
        private string _defectName; // 불량 유형, 직접 기입
        private string _batteryId; // 배터리 아이디
        // -------------

        private string modelPath = @".\weight\deeplab_model5.onnx"; // 이미지 처리 모델(ONNX) 로드
        //ONNX : 서로 다른 프레임워크 환경(PyTorch, etc..)에서 만들어진 딥러닝 모델들이 서로 호환되도록 만들어진 호환 플랫폼, 실질적으로 표준 AI 모델로 인정

        // 배터리 객체도 싱글톤으로 수행
        // 사유: 여러 개의 배터리 객체를 동시에 관리하는 것이 아닌 순차적으로 배터리 객체를 활용하기 때문
        static Battery staticBattery;
        private Battery() { } // 생성자 접근 제어 변경
        public static Battery Instance()
        {
            if (staticBattery == null)
                staticBattery = new Battery();
            return staticBattery;
        }
        // ----------------
        // ViewModel에서 사용할 프로퍼티 목록. ObservableObject의 SetProperty 메소드를 통해 자동 UI 반영
        public string ShootDate // 현재 날짜와 시간을 문자열로 반환하여 시간을 제공
        {
            get { return DateTime.Now.ToString("yyyy-MM-dd H:mm:ss"); }
        }
        public string Usage
        {
            get { return _usage; }
            set { SetProperty(ref _usage, value); }
        }
        public string BatteryType
        {
            get { return _batteryType; }
            set { SetProperty(ref _batteryType, value); }
        }
        public string ManufacName
        {
            get { return _manufacName; }
            set { SetProperty(ref _manufacName, value); }
        }
        public string ShootPlace
        {
            get { return _shootPlace; }
            set { SetProperty(ref _shootPlace, value); }
        }
        public string BatteryShape
        {
            get { return _batteryShape; }
            set { SetProperty(ref _batteryShape, value); }
        }
        public string ImagePath
        {
            get { return _imagePath; }
            set { SetProperty(ref _imagePath, value); }
        }
        public string DefectStat
        {
            get { return _defectStat; }
            set { SetProperty(ref _defectStat, value); }
        }
        public string DefectName
        {
            get { return _defectName; }
            set { SetProperty(ref _defectName, value); }
        }
        public string BatteryID
        {
            get { return _batteryId; }
            set { SetProperty(ref _batteryId, value); }
        }
        public BitmapImage BatteryBitmapImage // WPF(XAML)에서 사용되는 BitmapImage 형식, 이미지 처리 결과를 저장하기 위해 사용
        {
            get; set; 
        }

        // --------------------------
        // 멤버 메소드
        public void batteryInput() { } // DB에 insert관련? 미사용 메소드

        // ONNX 모델에 사용할 이미지 전처리 메소드. 모델에 이미지가 호환되도록 가공
        private DenseTensor<float> PreprocessImage()
        {
            // 지정된 이미지 파일 경로(_imagePath)에서 이미지를 로드
            Bitmap bitmap = new Bitmap(_imagePath);

            // ONNX 모델의 입력 크기에 맞게 리사이즈(RGB -> Float32 -> CHW)
            int targetWidth = 512; // ONNX 모델의 입력 크기(512 * 512)
            int targetHeight = 512;

            // ONNX 모델의 입력 데이터로 사용할 다차원 배열(텐서). 모델이 예측을 수행하기 위해 입력될 정규화된 픽셀 값들을 담기 위한 컨테이너
            // new[] { 배치 크기가 1(한 장의 이미지), 색상 채널이 3개(RGB), 모델이 요구하는 이미지 높이와 너비 }
            var inputTensor = new DenseTensor<float>(new[] { 1, 3, targetHeight, targetWidth });
            // 이미지 리사이즈 및 픽셀 데이터 채우기
            using (var resizedImage = new Bitmap(bitmap, new Size(targetWidth, targetHeight))) // 리사이즈된 이미지
            {
                // 리사이즈된 이미지의 각 픽셀들(512 * 512개)의 RGB 값을 추출
                for (int y = 0; y < targetHeight; y++)
                {
                    for (int x = 0; x < targetWidth; x++)
                    {
                        Color pixel = resizedImage.GetPixel(x, y); // x, y 좌표의 RGB값. 0~255
                        inputTensor[0, 0, y, x] = (pixel.R / 255.0f - 0.5f) / 0.5f; // Red [-1, 1] 정규화
                        inputTensor[0, 1, y, x] = (pixel.G / 255.0f - 0.5f) / 0.5f; // Green [-1, 1] 정규화
                        inputTensor[0, 2, y, x] = (pixel.B / 255.0f - 0.5f) / 0.5f; // Blue [-1, 1] 정규화
                    }
                }
            }
            return inputTensor; // 전처리된 이미지의 RGB값 반환. ONNX 모델의 입력으로 제공되어 예측 수행에 사용
        }

        // 모델 출력 후처리. ONNX 모델의 출력 텐서를 받아 매핑하여 최종 결과 이미지(Bitmap)를 생성하는 후처리 과정을 수행
        private Bitmap Postprocess(Tensor<float> output) // ONNX 모델의 출력 텐서를 매개변수로 입력
        {
            int numClasses = 4; // 클래스(범주) 수. 각 픽셀에 대해 모델은 4개의 범주에 대한 확률을 출력
            // 텐서의 차원(ONNX의 반환)은 보통[batch, channels, height, width]의 순서
            // 각 픽셀에 대해 어느 범주(클래스)에 해당할지에 대한 확률 값을 포함
            int height = output.Dimensions[2]; // 반환된 이미지의 높이
            int width = output.Dimensions[3]; // 반환된 이미지의 너비
            var colorMap = new int[,] // 범주별 색상 매핑
            {
                { 0, 0, 0 },     // 클래스 0(검정색)
                { 255, 0, 0 },   // 클래스 1(빨강색)
                { 0, 255, 0 },   // 클래스 2(초록색)
                { 0, 0, 255 }    // 클래스 3(파랑색)

            };

            // 후처리된 결과 이미지 선언. 각 픽셀에 대해 예측된 클래스에 대응하는 색상 색칠
            Bitmap resultBitmap = new Bitmap(width, height); 

            // 픽셀 단위 처리 및 범주 결정
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int maxClass = 0; // 모델이 예측한 확률이 가장 높은 클래스
                    float maxVal = float.MinValue;

                    // 각 픽셀에서 가장 높은 클래스 확률 선택
                    for (int c = 0; c < numClasses; c++)
                    {
                        float value = output[0, c, y, x]; // 모델이 반환한 텐서의 인덱스
                        if (value > maxVal)
                        {
                            maxVal = value;
                            maxClass = c;
                        }
                    }

                    // 클래스 색상 매핑. 가장 확률이 높은 범주의 각 RGB값 추출
                    Color classColor = Color.FromArgb(
                        colorMap[maxClass, 0],
                        colorMap[maxClass, 1],
                        colorMap[maxClass, 2]);

                    // 위치 (x, y)에 해당하는 픽셀에 선택한 색상을 적용
                    resultBitmap.SetPixel(x, y, classColor);
                }
            }
            return resultBitmap; // 최종적으로 각 픽셀이 예측된 클래스에 따른 색상으로 표현된 결과 이미지 반환
        }

        // Bitmap 변환. ONNX를 통해 생성, 후처리된 System.Drawing.Bitmap 객체를 WPF에서 주로 사용하는 System.Windows.Media.Imaging.BitmapImage 객체로 변환
        private BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            // MemoryStream 객체(임시 백업 저장소)를 생성하여 메모리 상에 임시로 데이터를 저장
            using (var memoryStream = new MemoryStream())
            {
                // 전달받은 bitmap 객체를 PNG 형식(무손실 압축)으로 스트림에 저장
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0; // 스트림에 데이터를 쓴 후, 읽기 시작점(위치)을 처음으로 복구

                BitmapImage bitmapImage = new BitmapImage(); // 변환될 데이터를 저장할 BitmapImage 객체 선언
                bitmapImage.BeginInit(); // BitmapImage 초기화가 시작되었음을 나타냄
                bitmapImage.StreamSource = memoryStream; // memoryStream을 할당하여 저장할 이미지 데이터를 지정. 읽기 시작점을 복구한 이유
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 스트림의 데이터를 즉시 로드, 스트림이 닫혀도 데이터를 유지
                bitmapImage.EndInit(); // BitmapImage 초기화가 끝났음을 나타냄

                return bitmapImage; // 초기화가 완료된 BitmapImage 객체를 반환, WPF에서 이미지 소스로 사용
            }
        }

        // 전체 이미지 처리 흐름. 위 메소드들을 포함하여 ONNX 모델을 사용하여 이미지 처리를 수행하는 전체 흐름을 제어
        // 이미지 전처리 -> ONNX 출력 후처리 -> Bitmap 변환
        public void imgProcessing()
        {
            // AI 처리 로직
            try
            {
                using var session = new InferenceSession(modelPath); // 이 객체는 ONNX 모델을 로드, 추론 수행

                if (!string.IsNullOrEmpty(_imagePath)) // 이미지 경로 유효성 확인
                {
                    var inputTensor = PreprocessImage(); // 이미지 전처리 수행, 전처리 완료된 텐서
                    var inputs = new List<NamedOnnxValue> // 전처리된 텐서를 ONNX 모델에 맞는 입력 포맷(NamedOnnxValue)으로 박싱
                    {
                        NamedOnnxValue.CreateFromTensor("input", inputTensor) // "input"은 모델 파일 내에서 정의된 입력 노드의 이름. 이름 확인 필요
                    };

                    using var results = session.Run(inputs); // 준비된 입력 데이터를 바탕으로 모델 추론을 실행, 결과값 저장
                    var output = results.First().AsTensor<float>(); // 반환된 결과(results) 중 첫 번째 출력값을 가져와 텐서(Tensor<float>)로 변환
                    // 각 픽셀에 대해 클래스별 예측 값(확률 등)이 포함

                    Bitmap result = Postprocess(output); // 출력 후처리
                    BatteryBitmapImage = BitmapToBitmapImage(result); // 결과 이미지 저장
                }
                else
                {
                    MessageBox.Show("이미지 경로가 비어 있습니다.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"오류 발생: {e.Message}\n{e.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
