#!/usr/bin/env python3
"""Fetch the real text of the curated FCA Handbook provisions.

The Handbook is a JavaScript single-page app, so a plain HTTP fetch only returns navigation
boilerplate. This renders each provision page with a headless browser and extracts the rendered
provision text into artifacts/handbook-text.json (a reference -> text map).

That file is gitignored: the FCA Handbook is Crown/FCA copyright and is published for retrieval,
not wholesale redistribution, so the real text lives in the database and this local cache - never
committed to the repo. The ingestion tool reads it via FCA_TEXT_FILE.

Usage:
    python3 scripts/fetch_handbook.py
Requires Playwright (pip install playwright) and a Chromium/Chrome browser.
"""
import json
import os
import re
import sys
import time

from playwright.sync_api import sync_playwright

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MANIFEST = os.path.join(REPO, "src", "FcaHandbookAssistant.Ingestion", "data", "manifest.json")
OUT_DIR = os.path.join(REPO, "artifacts")
OUT_FILE = os.path.join(OUT_DIR, "handbook-text.json")

# The rendered provision content lives in these containers, best first.
CONTENT_SELECTORS = [".sections-container", ".section-container", ".provisions-container"]
CHROME = os.environ.get("CHROME_PATH", "/usr/bin/google-chrome")


def clean(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()


def fetch_once(page, url: str) -> str:
    page.goto(url, wait_until="networkidle", timeout=60000)
    for selector in CONTENT_SELECTORS:
        try:
            page.wait_for_selector(selector, timeout=30000)
        except Exception:
            continue
        # The container can appear before its text hydrates; poll until it is substantial.
        for _ in range(20):
            element = page.query_selector(selector)
            if element:
                text = clean(element.inner_text())
                if len(text) > 200:
                    return text
            page.wait_for_timeout(500)
    return ""


def fetch(page, url: str, attempts: int = 3) -> str:
    for attempt in range(attempts):
        text = fetch_once(page, url)
        if text:
            return text
        page.wait_for_timeout(1000 * (attempt + 1))
    return ""


def main() -> int:
    with open(MANIFEST, encoding="utf-8") as handle:
        entries = json.load(handle)

    os.makedirs(OUT_DIR, exist_ok=True)
    # Keep provisions already fetched (runs can be flaky) and only retry the gaps.
    result = {}
    if os.path.exists(OUT_FILE):
        with open(OUT_FILE, encoding="utf-8") as handle:
            result = json.load(handle)

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True, executable_path=CHROME, args=["--no-sandbox"])
        page = browser.new_page(user_agent="fca-handbook-assistant/0.1 (+https://github.com/dbhq-uk/fca-handbook-assistant)")
        for entry in entries:
            reference, url = entry["reference"], entry["url"]
            if len(result.get(reference, "")) > 200:
                print(f"  keep {reference}: {len(result[reference])} chars")
                continue
            try:
                text = fetch(page, url)
            except Exception as error:  # noqa: BLE001
                text = ""
                print(f"  ! {reference}: {error}", file=sys.stderr)
            if text:
                result[reference] = text
                print(f"  ok {reference}: {len(text)} chars")
            else:
                print(f"  MISS {reference}: no content extracted", file=sys.stderr)
            time.sleep(1.5)  # polite pacing
        browser.close()

    with open(OUT_FILE, "w", encoding="utf-8") as handle:
        json.dump(result, handle, ensure_ascii=False, indent=2)
    print(f"Wrote {len(result)}/{len(entries)} provisions to {OUT_FILE}")
    return 0 if len(result) == len(entries) else 1


if __name__ == "__main__":
    raise SystemExit(main())
