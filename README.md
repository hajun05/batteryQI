# 제조업 (2차 전지) 품질 관리 SW
## batteryQI

Team : SF_Zone

Members : <a href = "https://github.com/hyeyeoung">김혜영</a>, <a href = "https://github.com/kyeob1107">곽승엽</a>, <a href = "https://github.com/Polar-Bear-Poby">김  건</a>, <a href = "https://github.com/hajun05">오하준</a>, <a href = "https://github.com/BWhale1010">조바울</a>

---
## 시스템 소개

2차 전지 품질 관리 소프트웨어는 배터리 생산 품질 검사를 혁신적으로 지원하는 AI 기반 솔루션입니다. 이 도구는 관리자 육안 검사와 AI 분석을 결합하여 품질 검사의 정확도와 효율성을 획기적으로 향상시킵니다. AI 기술은 검사 데이터를 정밀하게 분석하고, 관리자의 판단을 보조하여 잠재적인 결함을 선제적으로 식별합니다. 또한, 검사 결과를 기반으로 다양한 통계와 인사이트를 제공하여 생산 품질 관리의 전반적인 수준을 향상시킬 수 있도록 지원합니다.


### 1. 로그인 페이지

<img src = "./img/login.gif"/>

1. DB와 연결하여 로그인 기능 구현

### 2. 불량 검사
* 정상 배터리 이미지 업로드 시
<img src = "./img/nomalimg.gif"/>

* 불량 배터리 이미지 업로드 시
<img src = "./img/badimg.gif"/>

**시스템 흐름**

1. 검사할 배터리 이미지를 업로드한다.
2. 배터리 정보 기입
3. 이미지 검사 실행
4. 분석 결과 창에서 불량인지 정상인지 선택
5. 만약 불량이라면 어떤 불량인지 선택
6. 최종 배터리 정보 확인 후 DB에 입력

### 3. 통계 확인 페이지

<img src = "./img/statictics.gif"/>

아래와 같은 정보를 제공합니다.

1. 시간대별 불량수
2. 불량 유형 통계
3. 기준별 불량 유형 통계

### 4. 관리자 페이지

<img src = "./img/editamount.gif"/>

1. 관리자 할당량 수정 기능

<img src = "./img/addmanu.gif"/>

2. 제조사 리스트 추가 기능

##### AI 및 데이터 제공
<a href = "https://www.aihub.or.kr/aihubdata/data/view.do?currMenu=115&topMenu=100&aihubDataSe=data&dataSetSn=71687"> AI hub</a>

---
## 시스템 구조소개
- 클래스 간의 관계   
<img src = "./img/batteryQI-클래스간 관계.drawio.svg"/>

- Model 클래스 다이어그램   
<img src = "./img/batteryQI-Model.drawio.svg"/>

- ViewModel 클래스 다이어그램   
<img src = "./img/batteryQI-ViewModel.drawio.svg"/>

- View 클래스 다이어그램   
<img src = "./img/batteryQI-View.drawio.svg"/>
