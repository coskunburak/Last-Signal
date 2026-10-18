# P00 — Minimal owner-run static doğrulama

Durum: NOT_RUN_BY_OWNER. Bu komutlar **Burak tarafından** çalıştırılacak. Agent yalnız kaynak/bağlantı/koruma amaçlı read-only inspection yaptı; aşağıdaki kabul prosedürünü çalıştırıp sonuçlandırmadı. Unity, compilation, EditMode, PlayMode, build, profiler, benchmark ve gameplay: **N/A—NO_RUNTIME_CHANGE**, çünkü yalnız yeni Markdown belgeleri var. Mevcut dirty Unity işleri için bu N/A ayrı bir onay değildir.

## MS-01 — Git görünümü

Repo kökünde:

```sh
cd '/Users/burakcoskun/Last Signal'
git status --short
git diff --check
git diff --stat
```

`git diff` untracked P00 belgelerini göstermez. Beklenen: başlangıçtaki yedi tracked dirty dosya korunur; yeni değişiklik yalnız `Docs/Implementation/BLD/P00/`. Başlangıçtaki iki untracked belge dizini kullanıcı içeriğidir. Varsa eski whitespace uyarısı P00'a mal edilmez; ilgili başlangıç farkıyla karşılaştırılır. Tüm tracked diff, protected dosyalar ve yeni belgeler aşağıdaki komutta ayrıca kontrol edilir.

## MS-02 — Kaynak, kart, link ve preservation kontrolü

Standart Python 3 yeterli; paket kurmaz, dosya yazmaz, Unity başlatmaz. Aşağıdaki komut **bu belgedeki tek Python bloğunu** çalıştırır. İlk çalıştırmada `all`; dar retest için yalnız `sources`, `outputs` veya `preservation` argümanı kullanılır.

```sh
python3 -c 'from pathlib import Path; exec(Path("Docs/Implementation/BLD/P00/MANUAL_VERIFY_COMMANDS.md").read_text().split("```python\n",1)[1].split("\n```",1)[0])' all
```

Exit 0 yalnız statik kontrollerde uyuşmazlık bulunmadığı anlamına gelir; G0 veya runtime PASS değildir. Exit 1 çıktısındaki etiket/dosya ile yalnız başarısız kontrol yeniden ele alınır. Download paketi taşınmışsa ortam değişkeni `P00_PACKAGE_PATH` extracted klasörü gösterebilir; test başka bir kaynağı sessizce otoriter yapmaz, baseline hash'i yine zorunludur.

```python
from pathlib import Path
import hashlib
import json
import os
import re
import subprocess
import sys
from urllib.parse import unquote

root = Path.cwd()
out = root / "Docs/Implementation/BLD/P00"
kb = root / "Docs/BLD_Production_Knowledge_Base_v2_2026-10-17"
package = Path(os.environ.get("P00_PACKAGE_PATH", "/Users/burakcoskun/Downloads/LastSignal_P00_Production_Execution_2026-10-18"))
mode = sys.argv[1] if len(sys.argv) > 1 else "all"
if mode not in {"all", "sources", "outputs", "preservation"}:
    raise SystemExit("Use all, sources, outputs, or preservation")
manifest = json.loads((out / "SOURCE_PRESERVATION_MANIFEST.md").read_text().split("```json\n", 1)[1].split("\n```", 1)[0])
errors = []
def issue(label, detail):
    errors.append(f"{label}: {detail}")
def sha(path):
    if not path.is_file():
        issue("MISSING", str(path))
        return None
    return hashlib.sha256(path.read_bytes()).hexdigest()
def git(*args):
    return subprocess.check_output(["git", *args], cwd=root)
def check_links(paths):
    for path in paths:
        if not path.is_file():
            issue("MISSING", str(path))
            continue
        body = re.sub(r"```.*?```", "", path.read_text(), flags=re.S)
        for dest in re.findall(r"\]\(([^)]+)\)", body):
            dest = unquote(dest.strip("<>"))
            if re.match(r"[a-zA-Z][a-zA-Z0-9+.-]*:", dest):
                continue
            target, _, anchor = dest.partition("#")
            target_path = path.parent / target if target else path
            if not target_path.exists():
                issue("LINK", f"{path.relative_to(root)} -> {dest}")
            if anchor:
                issue("ANCHOR_REVIEW", f"Review anchor manually: {path.name} -> {dest}")
if mode in {"all", "sources"}:
    rows = manifest["sources"]
    if len(rows) != 30 or len({r["path"] for r in rows}) != 30:
        issue("SOURCE_COUNT", "Expected 30 unique baseline source paths")
    for row in rows:
        local = kb / row["path"]
        original = package / "P00_SOURCE" / row["path"]
        if sha(local) != row["source"]:
            issue("SOURCE_LOCAL_DRIFT", row["path"])
        if sha(original) != row["source"]:
            issue("SOURCE_PACKAGE_DRIFT", row["path"])
    for name, expected in manifest["package"].items():
        if sha(package / name) != expected:
            issue("PACKAGE_DRIFT", name)
    for base in (kb, package / "P00_SOURCE"):
        phase = base / "PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari"
        cards = sorted(phase.glob("BLD-S*/DAYS/BLD-D*.md"))
        ids = []
        for card in cards:
            match = re.match(r"BLD-D(\d{3})_", card.name)
            if not match:
                issue("CARD_NAME", str(card)); continue
            number = int(match[1]); ids.append(number)
            text = card.read_text()
            if not text.startswith(f"# BLD-D{number:03d} —"):
                issue("CARD_HEADING", str(card))
            expected_sprint = "BLD-S001" if number <= 8 else "BLD-S002"
            if not card.parent.parent.name.startswith(expected_sprint):
                issue("CARD_SPRINT", str(card))
            if "**Kaynak plandaki kabul:**" not in text:
                issue("CARD_ACCEPTANCE", str(card))
        if sorted(ids) != list(range(1, 17)):
            issue("CARD_IDS", f"{base}: expected unique 001..016, got {ids}")
    check_links([kb / row["path"] for row in rows])
    # Extracted P00 subset links to full program intentionally resolve in existing local KB.
if mode in {"all", "outputs"}:
    expected = {
        "README.md", "ENTRY_AUDIT.md", "SOURCE_PRESERVATION_MANIFEST.md",
        "BLD_S001_PRODUCT_SCOPE_CHANGE.md", "PLAYER_PROMISE_AND_STORIES.md",
        "BUILD_ZONE_POLICY.md", "SCOPE_MATRIX_AND_BUDGET.md", "SURVIVAL_VALUE_MAP.md",
        "FIRST_PLAYABLE_SCENARIO.md", "BLD_S001_HANDOFF.md", "CURRENT_CODE_BOUNDARIES.md",
        "BLD_S002_DOMAIN_OWNERSHIP.md", "CONTRACT_DATA_MODELS.md",
        "CONSTRUCTION_TRANSACTION_SEQUENCE.md", "PERSISTENCE_CELL_AND_ID_POLICY.md",
        "ASSET_SOURCE_AND_IMPORT_POLICY.md", "QA_AND_FAULT_MATRIX.md",
        "SAFETY_CHECKPOINT_AND_MIGRATION.md", "BLD_S002_HANDOFF.md",
        "G0_GATE_RECEIPT.md", "MANUAL_VERIFY_COMMANDS.md", "PRESERVATION_REPORT.md"
    }
    actual = {p.name for p in out.glob("*.md")}
    if actual != expected:
        issue("OUTPUT_SET", f"missing={sorted(expected-actual)}, extra={sorted(actual-expected)}")
    paths = sorted(out.glob("*.md")); check_links(paths)
    for path in paths:
        for n, line in enumerate(path.read_text().splitlines(), 1):
            if line.rstrip() != line:
                issue("WHITESPACE", f"{path.name}:{n}")
    for first, last, name in ((1,8,"BLD_S001_HANDOFF.md"),(9,16,"BLD_S002_HANDOFF.md")):
        text = (out / name).read_text()
        for i in range(first,last+1):
            rows = [line for line in text.splitlines() if line.startswith(f"| [BLD-D{i:03d}]")]
            if len(rows) != 1 or "IMPLEMENTED" not in rows[0] or "TEST_NOT_RUN" not in rows[0]:
                issue("TRACE_ROW", f"{name}: BLD-D{i:03d}")
    if "**Durum: PENDING_OWNER.**" not in (out / "G0_GATE_RECEIPT.md").read_text():
        issue("GATE_STATUS_REVIEW", "G0 status changed; verify actual owner signature/evidence")
if mode in {"all", "preservation"}:
    for path, expected_hash in manifest["protected"].items():
        if sha(root / path) != expected_hash:
            issue("PROTECTED_DRIFT", path)
    if hashlib.sha256(git("diff", "--binary", "HEAD")).hexdigest() != manifest["tracked_diff_sha256"]:
        issue("TRACKED_DIFF_DRIFT", "Compare with entry snapshot; do not overwrite new user edits")
    if git("branch", "--show-current").decode().strip() != manifest["branch"]:
        issue("BRANCH_DRIFT", "Refresh entry evidence")
    if git("rev-parse", "HEAD").decode().strip() != manifest["head"]:
        issue("HEAD_DRIFT", "Refresh entry evidence")
    before = set(manifest["status"].splitlines())
    now = set(git("status", "--short").decode().splitlines())
    extra = now - before
    unexpected = [s for s in extra if not s.startswith("?? Docs/Implementation/BLD/")]
    if unexpected or before - now:
        issue("STATUS_DRIFT", f"new={unexpected}, removed={sorted(before-now)}")
    non_docs = [str(p.relative_to(root)) for p in out.rglob("*") if p.is_file() and p.suffix != ".md"]
    if non_docs:
        issue("NON_DOC_OUTPUT_REVIEW", str(non_docs))
for error in errors:
    print("ERROR", error)
print(f"P00 STATIC {mode}: {len(errors)} issue(s); owner interpretation/signoff required; no Unity or G0 result.")
raise SystemExit(1 if errors else 0)
```

Bu kontrol dosya hedefleri için broken-link, kaynak/heading/sprint ID ve D001–016 duplicate kontrolü yapar; Markdown semantik doğruluğunu kanıtlamaz. Source P00 alt paketin dış program linkleri yerel tam KB'de çözülür. Koruma snapshot'ından sonra owner yeni edit/stage/commit yaptıysa preservation drift meşru olabilir; raporu iletin, geri alma yapmayın. Evidence dosyaları eklenmişse NON_DOC_OUTPUT_REVIEW beklenebilir ve ayrı review edilir.

## MS-03 — Kısa içerik spot-check

1. ADR önce/sonra tablosunu LS-DOC-14:18/26, LS-DOC-01:50 ve LS-PLAN-M02:42 ile karşılaştırın; kaynak paragraf silinmemiş olmalı.
2. S001/S002 handoff'ta 16 satırı açın: özgün kabul → çıktı paragrafı → karar → owner evidence eksiği doğru mu? D003 koordinat OPEN, D007 tahmin düşük güven olmalı.
3. D009 kod haritasında üç socket, storage-only üretim, Exchange callback sınırı ve SaveSession.Load adını spot-check edin.
4. D012 için kâğıt üzerinde prepublish fail ve postpublish lost response'u izleyin: biri rollback, diğeri aynı durable receipt. D013'te single owner, tombstone ve unloaded delta korunmalı.
5. US-12/SC-13 ile zorunlu crafting softlock'u ve voxel/co-op kapsam sızıntısı olmadığını inceleyin. G0 karar formunu doldurun; belgenin varlığı PASS değildir.

MS-04: G0 receipt'e owner/tarih, incelenen HEAD, statik log ve OD-01–05 kararlarını yazın. Yeni execution için ayrı talimat gerekir. Bir kontrol başarısızsa yalnız ilgili çıktı/dosya satırını iletin; exact focused retest bu belgedeki komutta `all` yerine ilgili mode kullanılarak verilecektir.
