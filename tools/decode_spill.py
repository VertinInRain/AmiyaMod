#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Decode a web_fetch spill file containing a GitHub contents API JSON (base64 content)."""
import base64
import json
import re
import sys

spill, out = sys.argv[1], sys.argv[2]
raw = open(spill, "r", encoding="utf-8", errors="replace").read()
m = re.search(r'"content"\s*:\s*"([A-Za-z0-9+/=\n]+)"', raw)
if not m:
    print("no content field found")
    sys.exit(1)
data = base64.b64decode(m.group(1))
open(out, "wb").write(data)
print("decoded bytes:", len(data))
