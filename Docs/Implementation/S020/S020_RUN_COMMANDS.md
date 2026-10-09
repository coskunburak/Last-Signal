# S020 kullanıcı çalıştırma komutları

**Ajan bu komutların hiçbirini çalıştırmadı.** Unity import/compile, EditMode, PlayMode, build ve manuel kabul NOT_RUN. Kurulu binary ve proje dosyasındaki sürüm `6000.5.0f1` olarak diskten kontrol edildi.

Önce Unity’de projeyi normal açıp yeni asset importlarının bitmesini bekleyin. Compile hatası varsa Console çıktısını gönderin; teste geçmeyin. Kaydedilmemiş kullanıcı sahnelerini koruyun, ardından Editor’ü normal kapatın. Açık Editor aynı projede batchmode’u engeller. Runner açık işlem/dosya kilidinde durur; kilit dosyasını silmez. İlk importun `.meta` normalizasyonu testten önce tamamlanmalıdır. Runner kaynak değişimini başarı saymaz; beklenmeyen değişiklik varsa kanıtı gönderin, otomatik geri alma yapmayın.

Her komut **yalnız tek kapıyı** çalıştırır. Önce hedefli EditMode; başarılıysa hedefli PlayMode; sonra iki regresyon; tümü başarılıysa Development build ve manuel kabul. Hata olduğunda sonraki komutu çalıştırmayın. Her koşu yeni `Evidence/Manual-YYYYMMDD-HHMMSS-<gate>-<suffix>/` dizini üretir (İstanbul saati). XML adı daima `tests.xml`, log `unity.log`; dizin stdout’ta `EVIDENCE=` ile yazılır. Unity exit kodu `process-result.json` içinde aynen kaydedilir; sıfır dışı kod runner’dan aktarılır. Unity 0 olsa da eksik/başarısız test veya değişmiş kaynak runner başarısızlığıdır. XML’de pass sayısı üretilmiş sonuçtan okunur.

## 1. Hedefli EditMode

```bash
python3 '/Users/burakcoskun/Last Signal/Tools/s020-verify.py' s020-edit --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

Assembly `LastSignal.EditModeTests`; filtre `LastSignal.Tests.S020CatalogTests`.

## 2. Hedefli PlayMode

```bash
python3 '/Users/burakcoskun/Last Signal/Tools/s020-verify.py' s020-play --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

Assembly `LastSignal.PlayModeTests`; filtre `LastSignal.Tests.S020CatalogPlayTests`.

## 3. Full EditMode regresyon

```bash
python3 '/Users/burakcoskun/Last Signal/Tools/s020-verify.py' regression-edit --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

Assembly’ler `LastSignal.EditModeTests;LastSignal.Art.EditorTests`. Eski testlerin geçmiş kanıt yazımları mevcut S015 preservation mekanizmasıyla korunur.

## 4. Full PlayMode regresyon

```bash
python3 '/Users/burakcoskun/Last Signal/Tools/s020-verify.py' regression-play --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

Assembly `LastSignal.PlayModeTests`. Özellikle S018 survival, S010 crafting, S019 validators, inventory/loot/persistence testleri gerekir.

## 5. macOS Development build

```bash
python3 '/Users/burakcoskun/Last Signal/Tools/s020-verify.py' development-build --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

Yeni açık entry `LastSignal.Art.Editor.S020Build.DevelopmentMac`, mevcut `S013ProductionAuthoring.Build(output)` metoduna delegasyon yapar. Sahne gerçek `Assets/LastSignal/Scenes/Production/S013Cabin.unity`; hedef StandaloneOSX, Development. `LASTSIGNAL_S020_BUILD_OUTPUT` runner tarafından benzersiz `Builds/S020/<run>/` dizinine atanır. Çıktı `LastSignal.app`, `build.txt`, `warnings.txt`. macOS build support yoksa kurulmuş gibi gösterilmez; build BLOCKED kanıtını gönderin. Build çalıştırılmış oyun/kabul değildir.

## 6. Son koşu sonucu ve hata loglarını inceleme

Aşağıdaki komut yalnız dosya okur; Unity çağırmaz:

```bash
python3 - <<'PY'
from pathlib import Path
import re
import xml.etree.ElementTree as ET
root = Path('/Users/burakcoskun/Last Signal/Docs/Implementation/S020/Evidence')
runs = sorted((p for p in root.glob('Manual-*') if p.is_dir()), key=lambda p: p.name)
if not runs:
    raise SystemExit('Henüz kullanıcı koşusu yok.')
run = runs[-1]
print('EVIDENCE=' + str(run))
for name in ('process-result.json', 'result.json', 'source-preservation.json'):
    p = run / name
    print(name + ': ' + (p.read_text() if p.exists() else 'MISSING'))
xml = run / 'tests.xml'
if xml.exists():
    doc = ET.parse(xml).getroot()
    print('NUnit:', doc.attrib)
    for case in doc.iter('test-case'):
        if case.get('result') != 'Passed':
            print(case.get('fullname'), case.get('result'), case.findtext('failure/message', ''))
log = run / 'unity.log'
if log.exists():
    lines = [line for line in log.read_text(errors='replace').splitlines()
             if re.search(r'error CS\d+|Exception|Assertion|FAILED|Error:|BuildFailed|Aborting batchmode', line)]
    print('\n'.join(lines[-100:]) or 'Aranan hata kalıbı yok; bu tek başına PASS değildir.')
PY
```

Dönen `EVIDENCE` klasöründeki `tests.xml`, `unity.log`, `process-result.json`, `result.json` ve `source-preservation.json` dosyalarını gönderin. Önceki başarısız koşuları silmeyin. Failure-repair loop sırasında ajan sadece kanıtı inceler/düzeltir ve yeni hedefli komutu verir; testi yine kullanıcı çalıştırır.
