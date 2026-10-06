# S013 ışık ve tekrar üretilebilir benchmark

URP17.5 / Unity6000.5.0f1. Mevcut PC renderer Forward+ olarak kalır. PC pipeline: renderScale1, MSAA1, ana shadow2048,4cascade,50m; additional atlas2048, tier256/512/1024. Sahnedeki sıcak iç point light için6cube shadow yüzünün atlası taşırması görüldü; sadece bu ışık tier256 olarak düzeltildi. Porch light shadows kapalı. Bu bir ölçülmüş FPS kazanımı iddiası değildir.

S013 sun/rain mevcut WorldTimePresentation tarafından yönetilir. S013EnvironmentPresentation saati/havayı gözler; simulation advance veya yeni weather authority oluşturmaz. Soğuk ambient/fog geceye göre değişir. Global tonemapping ACES, saturation-14; bloom/SSAO/SSR eklenmedi. Kamera SMAA Medium. Interior warm light2.2/range7, porch1.4/range7. Bu sayılar başlangıç standardıdır; standalone gece kabulü yapılmadan “final approved lighting” değildir.

| Sabit kamera | Kabin merkezine göre pozisyon | Hedef |
|---|---|---|
| Exterior |17,5,-14|0,2,0|
| Approach |10,1.7,1|3,1.5,1|
| Interior |1.9,1.65,-2|-.5,1.3,1|
| Interaction |1.1,1.55,-1.5|0,1.2,0|
| Threat |12,1.7,4|20,1,4|
| Flashlight |1.9,1.65,1.6|-1.5,1.4,-1|

Merkez(-360,0,-140); golden capture1920×1080,FOV65,near.05,far250. Threat kamerası tek başına canlı zombi görünürlük kanıtı değildir; ayrıca sahnede gerçek actor ile kabul gerekir. Flashlight capture kontrollü3-intensity,12m,50° spot fixture'dır; üründe flashlight input/inventory sistemi varmış gibi gösterilmez.

Development build `-s013Performance <yeni-output-dizini>` ile opt-in başlar. Her koşulda mevcut clock Simulation.AdvanceUntil hedefe ulaşır; ardından hava snapshot'ı kurulur. Gündüz/yağmur43200s, gece82800s;1world-second/real-second. Player(-350,.05,-139),yaw270 sabit, kamera gerçek75FOV.5s ısınma,20s ölçüm. Baseline ve S013 aynı yöntem/donanım/resolution ile ayrı yürütülür. Golden render ve screenshot ölçüm dışındadır.10 lifecycle turu ayrıca kayıt üretir.

Frame avg/p50/p95/p99, mevcut ProfilerRecorder CPU/GPU/GC/drawcall/batch/memory sayaçları; desteklenmeyenler UNAVAILABLE yazılır. GPU verisi olmaması sıfır maliyet anlamına gelmez. Texture memory/material count/overdraw için ayrıca inceleme gerekir. Kamera/capture kodunun varlığı ölçüm değildir.

Mevcut kalite adları Mobile,PC; Low/Medium/High yok. `-s013Tier Low` (25m,1cascade,MSAA1) ve `Medium` (40m,2cascade,MSAA2) runtime klon üzerinde öneri karşılaştırmalarıdır; project settings'i değiştirmez. High/varsayılan mevcut PC'dir. `-s013Quick` yalnız day ölçer, lifecycle atlar. Bu adayların kabulü NOT_RUN; kanıtsız kalite profili önerilmez.

Gerçek build henüz modalda beklediği için screenshot/performance sonuçları yok. Editor `preview-r3` yalnız gündüz kompozisyon ve kesişme QA'sıdır. Mac sonuçları alındığında bile Windows retail performansı olarak sunulamaz.
