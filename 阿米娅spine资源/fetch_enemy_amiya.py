# -*- coding: utf-8 -*-
"""
下载 boss「"阿米娅"，炉芯终曲」(enemy_2092_skzamy) 等阿米娅敌方形态的 Spine 资源。
优先从 prts 官方预览前缀 torappu.prts.wiki/assets/enemy_spine/ 抓取; 失败则回退 Ark-Models(main) 镜像。
"""
import csv, json, os, shutil, time, urllib.parse, urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
PRTS_BASE = 'https://torappu.prts.wiki/assets/enemy_spine/'
GH = 'https://raw.githubusercontent.com/isHarryh/Ark-Models/main/models_enemies/%s/%s'
UA = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0 Safari/537.36',
      'Referer': 'https://prts.wiki/', 'Accept-Language': 'zh-CN,zh;q=0.9'}

KEYS = ['2092_skzamy', '1552_mmamiy', '3008_lramiy']  # 炉芯终曲 / 敌方阿米娅(首领) / 敌方阿米娅(普通)

def get(url, timeout=200):
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.read()

def dl(url, dest):
    for attempt in range(3):
        try:
            req = urllib.request.Request(url, headers=UA)
            with urllib.request.urlopen(req, timeout=240) as r, open(dest, 'wb') as f:
                f.write(r.read())
            if os.path.getsize(dest) < 100:
                os.remove(dest); raise IOError('too small')
            return True
        except Exception as e:
            print('  dl fail(%d) %s -> %s' % (attempt + 1, url[-90:], str(e)[:80]))
            time.sleep(1.5)
    return False

def main():
    os.makedirs(HERE, exist_ok=True)
    # 从索引取条目元数据(真实中文名)
    try:
        data = json.loads(get('https://raw.githubusercontent.com/isHarryh/Ark-Models/main/models_data.json'))
        entries = data['data']
    except Exception:
        entries = {}
    rows, meta = [], {}
    for key in KEYS:
        ent = entries.get(key) or {}
        al = ent.get('assetList') or {'.atlas': 'enemy_%s.atlas' % key, '.png': 'enemy_%s.png' % key, '.skel': 'enemy_%s.skel' % key}
        sub = os.path.join(HERE, 'enemy_' + key)
        os.makedirs(sub, exist_ok=True)
        ok = 0
        used_prts = True
        for ext, fname in al.items():
            dest = os.path.join(sub, fname)
            if os.path.exists(dest) and os.path.getsize(dest) >= 100:
                ok += 1
                continue
            url_prts = PRTS_BASE + urllib.parse.quote(key) + '/' + urllib.parse.quote(fname)
            if dl(url_prts, dest):
                ok += 1
                continue
            url_gh = GH % (urllib.parse.quote(key), urllib.parse.quote(fname))
            if dl(url_gh, dest):
                ok += 1
                used_prts = False
        if ok == 0:
            shutil.rmtree(sub, ignore_errors=True)
            print('skip empty:', key)
            continue
        sz = sum(os.path.getsize(os.path.join(sub, f)) for f in os.listdir(sub) if os.path.isfile(os.path.join(sub, f)))
        rows.append({'条目': 'enemy_' + key, 'type': ent.get('type', 'Enemy'),
                     '官方名': ent.get('name', ''), 'skinGroup': ent.get('skinGroupName', ''),
                     'tags': ','.join(ent.get('sortTags', []) or []),
                     '文件': ','.join(al.values()), '文件数': len(al), '成功数': ok,
                     '总字节': sz, '来源': 'prts直链' if used_prts else 'Ark-Models镜像'})
        meta[key] = {'entry': key, 'name': ent.get('name'), 'skinGroup': ent.get('skinGroupName'),
                     'files': al, 'via': 'prts直链' if used_prts else 'Ark-Models镜像'}
        print('enemy_%-18s ok=%d/%d bytes=%d via=%s name=%s' % (key, ok, len(al), sz, 'prts' if used_prts else 'gh', ent.get('name')))

    with open(os.path.join(HERE, 'manifest_enemy.csv'), 'w', newline='', encoding='utf-8-sig') as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()) if rows else [])
        w.writeheader()
        w.writerows(rows)
    with open(os.path.join(HERE, 'enemy_meta.json'), 'w', encoding='utf-8') as f:
        json.dump(meta, f, ensure_ascii=False, indent=1)
    with open(os.path.join(HERE, '敌方条目说明.txt'), 'w', encoding='utf-8') as f:
        f.write('阿米娅 敌方/特殊形态 Spine 资源\n')
        f.write('prts 页面: https://prts.wiki/w/“阿米娅”，炉芯终曲   (页面内 SPINEDATA 前缀即 torappu.prts.wiki/assets/enemy_spine/enemy_2092_skzamy/)\n')
        f.write('来源: prts(游戏资源镜像) + isHarryh/Ark-Models 敌方目录, 版权属 Hypergryph/鹰角网络, 仅供学习/非商用参考。\n')
        for r in rows:
            f.write('[%s] 官方名=%s 皮肤组=%s tags=%s 来源=%s\n  文件: %s\n' % (
                r['条目'], r['官方名'], r['skinGroup'], r['tags'], r['来源'], r['文件']))
    print('DONE enemy entries:', len(rows))

if __name__ == '__main__':
    main()
