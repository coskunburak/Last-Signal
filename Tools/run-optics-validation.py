#!/usr/bin/env python3
"""Açık Unity Editor üzerinden test ister; gerçek NUnit XML sonucunu raporlar."""
import argparse
import datetime
import fcntl
import json
import os
from pathlib import Path
import sys
import time
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("edit", "play", "optics"),
                        help="edit/play: tüm proje test derlemesi; optics: üç optik PlayMode testi")
    parser.add_argument("--timeout", type=int, default=1800,
                        help="Sonuç bekleme sınırı, saniye (varsayılan 1800)")
    parser.add_argument("--filter", action="append", default=[],
                        help="Tam test veya sınıf adı; birden çok sınıf için tekrarlanabilir")
    args = parser.parse_args()
    if args.mode == "optics" and args.filter:
        parser.error("optics zaten sabit üç testi seçer; özel filtre için edit/play kullanın.")
    if args.timeout < 30:
        parser.error("Süre sınırı en az 30 saniye olmalı.")
    temp = ROOT / "Temp"
    if not (temp / "UnityLockfile").exists():
        parser.error("Bu projeyi Unity Editor'da açın; ardından komutu yeniden çalıştırın.")
    with (temp / "LastSignalOpticsValidation.lock").open("w") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            parser.error("Başka bir doğrulama komutu hâlâ çalışıyor.")
        request = temp / "LastSignalZombieValidation.json"
        if request.exists():
            parser.error("Unity henüz önceki test isteğini tüketmedi. Yeni tur gönderilmedi.")
        stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + "-" + uuid.uuid4().hex[:6]
        folder = ROOT / "Docs/Implementation/Combat/OpticsEvidence" / (stamp + "-" + args.mode)
        folder.mkdir(parents=True)
        output = folder / "results.xml"
        report = folder / "RAPOR.txt"
        payload = dict(action="edit" if args.mode == "edit" else "play",
                       filter="LastSignal.Tests.ScopeOpticPlayTests" if args.mode == "optics" else "",
                       filters=args.filter,
                       output=str(output))
        print("Sahnelerinizi kaydedin; Play Mode ve başka test turu kapalı olmalı.", flush=True)
        print("8 saniye sonra başlayacak. Unity'ye geçin ve test boyunca odakta tutun.", flush=True)
        print(f"Rapor: {report}", flush=True)
        time.sleep(8)
        started = time.time()
        staging = temp / ("optics-request-" + uuid.uuid4().hex + ".json")
        staging.write_text(json.dumps(payload), encoding="utf-8")
        try:
            # Tam yazılmış isteği atomik yayımla; mevcut bir isteğin üstüne yazma.
            os.link(staging, request)
        finally:
            staging.unlink()
        error = temp / "LastSignalZombieValidation-error.txt"
        status = temp / "LastSignalZombieValidation-result.json"
        while time.time() - started < args.timeout:
            if output.exists():
                try:
                    root = ET.parse(output).getroot()
                except ET.ParseError:
                    time.sleep(1)
                    continue
                lines = [f"Tur: {args.mode}", f"Filtre: {args.filter or payload['filter'] or 'Tüm derleme'}", f"Sonuç XML: {output}",
                         "Özet: " + json.dumps(root.attrib, ensure_ascii=False)]
                for case in root.iter("test-case"):
                    if case.get("result") != "Passed":
                        lines.append(f"\n{case.get('result')}: {case.get('fullname', case.get('name'))}")
                        for tag in ("failure/message", "failure/stack-trace", "reason/message"):
                            value = case.findtext(tag)
                            if value:
                                lines.append(value)
                text = "\n".join(lines) + "\n"
                report.write_text(text, encoding="utf-8")
                print(text)
                return 0 if root.get("result") == "Passed" and int(root.get("total", "0")) > 0 else 1
            problem = None
            if error.exists() and error.stat().st_mtime >= started:
                problem = error.read_text(encoding="utf-8")
            if status.exists() and status.stat().st_mtime >= started:
                try:
                    if json.loads(status.read_text()).get("compileFailed"):
                        problem = "Unity betik derlemesi başarısız. Console hata metinlerini gönderin."
                except (ValueError, OSError):
                    pass
            if problem:
                report.write_text(problem, encoding="utf-8")
                print(problem)
                return 2
            time.sleep(1)
        message = ("Süre sınırına ulaşıldı; tamamlanmış test sonucu yok. Bu bir başarı değildir.\n"
                   "Unity'deki tur otomatik iptal edilmedi. Test Runner'dan durdurun; "
                   "Console hatalarını ve bu raporu gönderin.\n"
                   f"İstek henüz tüketilmedi: {request.exists()}\n")
        report.write_text(message, encoding="utf-8")
        print(message)
        return 2


if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        print("\nBekleme kesildi. Unity'de başlamış test turu çalışmaya devam edebilir.")
        sys.exit(130)
