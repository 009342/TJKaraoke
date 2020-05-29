# TJ 반주기 오픈소스 구현체

TJ 반주기에 사용되는 포맷을 재생할 수 있게 만든 오픈소스 구현체입니다.

## 사용방법

TJKaraoke.exe [File Name] [Country Code] [MIDI Device ID = 0 (Default)]

[File Name]에서는 암호화되어있지 않고, 압축이 해제된 상태의 파일의 경로를 필요로 합니다.

[Country Code]는 국가별 상이한 국가 코드를 정수로 입력해야 합니다.

[MIDI Device ID = 0 (Default)]은 출력에 사용할 MIDI 디바이스의 ID를 입력해야 합니다. 기본값은 0입니다.

## ToDo

1. 이벤트 코드의 분석

2. MIDI파일 등을 역으로 조합하여 다시 파일로 만드는 것

3. 다른 국가의 언어 추가

## Disclaimer

1. 이 구현체는 **단순히 TJ 반주기에 사용되는 암호화가 해제되고 압축이 해제된 파일을 재생하는 구현체**에 지나지 않습니다.

2. 이 구현체의 소스코드는 [**본 라이선스**][LICENSE]에 의거하여 보호됩니다.

3. TJ는 TJ미디어(주)의 상표명입니다.

[LICENSE]: https://github.com/009342/TJKaraoke/blob/master/LICENSE
