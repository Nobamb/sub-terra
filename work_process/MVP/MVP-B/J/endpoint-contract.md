# Phase J 자체 endpoint 계약

Unity 클라이언트는 제공자 API를 직접 호출하지 않고, 프로젝트가 운영하는 HTTPS endpoint에만 `POST` 요청을 보낸다. 제공자 API 키와 인증 비밀은 서버 환경에서만 관리한다.

## 요청

`Content-Type: application/json`

```json
{
  "actionKey": "LeaveGasZone",
  "reasonKeys": ["gas_risk"],
  "facts": [
    { "key": "gas_risk", "value": 0.8, "unit": "ratio" }
  ],
  "language": "ko"
}
```

허용 필드는 Phase I이 확정한 행동 키, 근거 키, 그 근거의 표시 수치/단위와 언어뿐이다. 세이브 데이터, 로컬 경로, 환경변수, 사용자 식별자, 다른 행동 후보와 API 비밀은 보내지 않는다.

## 응답

성공은 HTTP 2xx와 다음 JSON을 함께 만족해야 한다.

```json
{
  "dialogue": "가스 농도가 위험 수준입니다. 즉시 구역을 벗어나세요."
}
```

`dialogue`는 설정된 최대 길이 이하의 일반 텍스트여야 한다. 빈 문자열, 제어 문자, HTML/Markdown 표식, 잘못된 JSON은 실패로 취급한다. Unity는 이 문자열을 대사로만 표시하며 행동 키나 State를 응답에서 읽지 않는다.

## 실패와 운영

- 시간 초과, 취소, 오프라인, 4xx/5xx, 429, 잘못된 응답은 Unity의 동일 Phase I 분석 결과로 템플릿 대사를 사용한다.
- 서버는 요청 크기 제한, 인증, 제공자 호출 제한과 비밀 관리를 담당한다.
- Unity Release 기본값은 비활성이며, 활성화하더라도 HTTPS endpoint 주소만 배포물에 포함할 수 있다.
- 서버와 클라이언트 로그에는 요청/응답 원문, 세이브 데이터와 비밀값을 남기지 않는다.
